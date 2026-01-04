

using System.ComponentModel.Design;

internal class CheckTypedByteCode(Type intType, Type boolType, List<Generic> functionTypes)
{
    Type IntType = intType;
    Type BoolType = boolType;
    List<Generic> FunctionTypes = functionTypes;
    Dictionary<Variable, RefinementAndType> Environment = new();

    // This will produce the untyped refinement checking byte code
    // And it will also check the base types of the code

    public (bool Valid, BBlock? Block) CheckBlock(TBlock block)
    {
        switch (block)
        {
            case TBlock.Basic basicBlock:
                return CheckBasicBlock(basicBlock);
            default:
                throw new Exception("Invalid block");
        }
    }

    public (bool Valid, BBlock? Block) CheckBasicBlock(TBlock.Basic basicBlock)
    {
        List<BStmt> statements = new();
        // go through all the statements and check them
        foreach (TStmt statement in basicBlock.Body)
        {
            (bool valid, var stmts) = CheckStatement(statement);
            statements.AddRange(stmts);
            if (!valid)
            {
                return (false, null);
            }
        }
        return (true, new BBlock.Basic(statements));
    }

    public (bool Valid, List<BStmt> Statement) CheckStatement(TStmt statement)
    {
        switch (statement)
        {
            case TStmt.Assignment assignment:
                return CheckAssignmentStatement(assignment);
            case TStmt.AssertType assertion:
                return CheckAssertion(assertion);
            case TStmt.AssumeType assumption:
                return CheckAssumption(assumption);
            case TStmt.Z3Assumption z3assumptions:
                return CheckZ3Assumption(z3assumptions);
            default:
                throw new Exception("Invalid statement");
        }
    }


    public (bool Valid, List<BStmt> Statement) CheckAssertion(TStmt.AssertType statement)
    {
        // assert the variable is part of the type
        return CheckVariableInRefinedType(statement.Variable, Environment[statement.Variable].Type, statement.Type, []);
    }

    public (bool Valid, List<BStmt> Statement) CheckAssumption(TStmt.AssumeType statement)
    {
        // assume the variable is part of the type
        List<BStmt> statements = new();

        // if the given type is more specific than the current environment type, change it

        // currently, just give it that type
        Environment[statement.Variable] = statement.Type;

        // now assert the refinement
        if (statement.Type.Refinement is not null)
        {
            (bool refinementResult, var refinementStatements) = CheckRefinementsOnVariable(statement.Variable, statement.Type.Refinement, [], true);
            statements.AddRange(refinementStatements);
            if (!refinementResult)
            {
                return (false, statements);
            }
        }

        return (true, statements);
    }

    public (bool Valid, List<BStmt> Statement) CheckZ3Assumption(TStmt.Z3Assumption statement)
    {
        List<BStmt> statements = new();

        // go through each statement and check its type
        foreach((RefinementAndType type, Variable variable) in statement.Arguments)
        {
            (bool refinementResult, var refinementStatements) = CheckVariableInRefinedType(variable, Environment[variable].Type, type, []);
            statements.AddRange(refinementStatements);
            if (!refinementResult)
            {
                return (false, statements);
            }
        }

        // the checkvar is a boolean
        Environment[statement.CheckVar] = new(BoolType, null);

        statements.Add(new BStmt.Z3Assumption(statement.AssumptionFunction, statement.CheckVar, [.. from arg in statement.Arguments select arg.Item2]));
        return (true, statements);
    }

    public (bool Valid, List<BStmt> Statements) CheckAssignmentStatement(TStmt.Assignment statement)
    {
        // assignment statement has two parts: variable, and expression
        // the type of the expression must match the type of the variable
        List<BStmt> statements = new List<BStmt>();
        Variable variable = statement.Variable;
        Environment[variable] = statement.Type;
        Type expressionBaseType;
        Dictionary<Variable, Variable> substitutions;
        switch (statement.Value)
        {
            case TExpr.BoolConstant boolConstant:
                {
                    // add var = constant to the statement
                    statements.Add(new BStmt.Assignment(variable, new BExpr.BoolConstant(boolConstant.Value)));
                    expressionBaseType = BoolType;
                    substitutions = [];
                    break;
                }
            case TExpr.FunctionCall functionCall:
                {
                    // get the function
                    Variable function = functionCall.Function;
                    RefinementAndType functionType = Environment[function];
                    // check that the type of the function is a function type
                    if (!FunctionTypes.Contains(functionType.Type.Base))
                    {
                        Console.WriteLine("function given is not of a function type!");
                        return (false, statements);
                    }
                    // check that arguments will fit into function
                    List<RefinementAndType> parameterTypes = functionType.Type.Arguments[0..^1];
                    RefinementAndType resultType = functionType.Type.Arguments[^1];
                    if (functionCall.Arguments.Count != parameterTypes.Count)
                    {
                        Console.WriteLine("function arguments do not match parameter length");
                        return (false, statements);
                    }

                    substitutions = [];
                    (bool valid, List<BStmt> stmts) = CheckFunctionApplicationType(function, functionType.Type, [..functionCall.Arguments, variable], substitutions);
                    statements.AddRange(stmts);
                    if (!valid)
                    {
                        return (false, statements);
                    }
                    expressionBaseType = resultType.Type;
                    break;
                }
            case TExpr.IntConstant intConstant:
                {
                    // add var = constant to the statement
                    statements.Add(new BStmt.Assignment(variable, new BExpr.IntConstant(intConstant.Value)));
                    expressionBaseType = IntType;
                    substitutions = [];
                    break;
                }
            case TExpr.VariableRead variableRead:
                {
                    // add var = variable to the statement
                    statements.Add(new BStmt.Assignment(variable, new BExpr.VariableRead(variableRead.Variable)));
                    expressionBaseType = Environment[variableRead.Variable].Type;
                    substitutions = [];
                    break;
                }
            default:
                throw new Exception("Invalid expression");
        }
        // now check that that the variable will be able to fit into its type
        (bool typeCheck, List<BStmt> moreStatements) = CheckVariableInRefinedType(variable, expressionBaseType, Environment[variable], substitutions);
        statements.AddRange(moreStatements);
        return (typeCheck, statements);
    }

    public (bool Valid, List<BStmt> Statements) CheckFunctionApplicationType(Variable functionVariable, Type functionType, List<Variable> arguments, Dictionary<Variable, Variable> substitutions)
    {
        List<BStmt> statements = [];
        // check that the arguments match the number of type arguments
        List<Variable> parameters = functionType.FunctionVariables;
        List<RefinementAndType> typeArguments = functionType.Arguments;
        List<Variance> variances = functionType.Base.Parameters;
        if (arguments.Count != parameters.Count || parameters.Count != typeArguments.Count || typeArguments.Count != variances.Count)
        {
            Console.WriteLine("number of function arguments must be the same as the parameters which must be the same as the type arguments which must be the same as the variances!");
        }

        List<Variable> inputs = [.. from i in Enumerable.Range(0, variances.Count) where variances[i] == Variance.Contravariant select arguments[i]];

        // now check parameters
        for (int i = 0; i < variances.Count; i++)
        {
            // go through each parameter -> action depends on variance
            Variance variance = variances[i];
            substitutions[parameters[i]] = arguments[i];
            switch (variance)
            {
                // if it is covariant -> it is an output
                case Variance.Covariant:
                    {
                        // add a function application
                        statements.Add(new BStmt.Assignment(arguments[i], new BExpr.FunctionCall(functionVariable, inputs, i)));
                        // make assumptions about the output variable
                        if (typeArguments[i].Refinement is not null)
                        {
                            (bool targetValid, List<BStmt> targetStmts) = CheckRefinementsOnVariable(arguments[i], typeArguments[i].Refinement, new Dictionary<Variable, Variable>(substitutions), true);
                            statements.AddRange(targetStmts);
                            if (!targetValid)
                            {
                                return (false, statements);
                            }
                        }
                        break;
                    }
                // if it is contravariant -> it is an input
                case Variance.Contravariant:
                    {
                        // create a substitution from the parameter variable to the argument
                        (bool argValid, List<BStmt> argStmts) = CheckVariableInRefinedType(arguments[i], Environment[arguments[i]].Type, typeArguments[i], new Dictionary<Variable, Variable>(substitutions));
                        statements.AddRange(argStmts);
                        if (!argValid)
                        {
                            return (false, statements);
                        }
                        break;
                    }
                // if it is invariant -> raise an error
                case Variance.None:
                default:
                    {
                        Console.WriteLine("a function cannot have an invariant type argument!");
                        return (false, []);
                    }
            }
        }
        return (true, statements);
    }


    public (bool Valid, List<BStmt> Statements) CheckVariableInRefinedType(Variable variable, Type variableBaseType, RefinementAndType targetType, Dictionary<Variable, Variable> substitutions)
    {
        // check that the variableBaseType goes into the target base type
        (bool baseResult, var baseStatements) = CheckTypeInType(variableBaseType, targetType.Type, new Dictionary<Variable, Variable>(substitutions));
        List<BStmt> statements = [.. baseStatements];
        if (!baseResult)
        {
            return (false, statements);
        }

        // now assert the refinement
        if (targetType.Refinement is not null)
        {
            (bool refinementResult, var refinementStatements) = CheckRefinementsOnVariable(variable, targetType.Refinement, new Dictionary<Variable, Variable>(substitutions), false);
            statements.AddRange(refinementStatements);
            if (!refinementResult)
            {
                return (false, statements);
            }
        }

        return (true, statements);
    }

    public (bool Valid, List<BStmt> Statements) CheckRefinementTypeInRefinementType(RefinementAndType startType, RefinementAndType targetType, Dictionary<Variable, Variable> substitutions)
    {
        // check that the base type goes into the target base type
        // this will also add the required parameters to substitutions
        (bool baseResult, var baseStatements) = CheckTypeInType(startType.Type, targetType.Type, substitutions);
        List<BStmt> statements = [.. baseStatements];
        if (!baseResult)
        {
            return (false, baseStatements);
        }

        // now assert the refinement - only create the variable if there is a required refinement
        if (targetType.Refinement is not null)
        {
            Variable dummyVar = new Variable("dummy");
            Environment[dummyVar] = startType;
            if (startType.Refinement is not null)
            {
                (bool assumptionResults, var assumptionStatements) = CheckRefinementsOnVariable(dummyVar, startType.Refinement, substitutions, true);
                statements.AddRange(assumptionStatements);
                if (!assumptionResults)
                {
                    return (false, statements);
                }
            }

            (bool refinementResult, var refinementStatements) = CheckRefinementsOnVariable(dummyVar, targetType.Refinement, substitutions, false);
            statements.AddRange(refinementStatements);
            if (!refinementResult)
            {
                return (false, statements);
            }
        }

        return (true, statements);
    }

    public (bool Valid, List<BStmt> Statements) CheckTypeInType(Type startType, Type targetType, Dictionary<Variable, Variable> substitutions)
    {
        // the base generic type must be the same
        if (!CheckGenericTypeInGenericType(startType.Base, targetType.Base))
        {
            return (false, []);
        }



        // if there are any type arguments, go through them and check variance
        if (startType.Base == targetType.Base)
        {
            Generic baseType = startType.Base;

            List<Variance> variances = baseType.Parameters;
            if (startType.Arguments.Count != variances.Count)
            {
                Console.WriteLine("type has wrong number of parameters");
                return (false, []);
            }
            if (targetType.Arguments.Count != variances.Count)
            {
                Console.WriteLine("type has wrong number of parameters");
                return (false, []);
            }

            if (baseType.FunctionType)
            {
                // there are variable parameters to create here
                if (startType.FunctionVariables.Count != variances.Count)
                {
                    Console.WriteLine("type has wrong number of variable parameters!");
                    return (false, []);
                }
                if (targetType.FunctionVariables.Count != variances.Count)
                {
                    Console.WriteLine("type has wrong number of variable parameters!");
                    return (false, []);
                }

                Console.WriteLine("Not yet implemented assigning function types to function types.");
            }


            return CheckNonFunctionTypeArguments(variances, startType.Arguments, targetType.Arguments, substitutions);
        }
        else
        {
            return (false, []);
        }
    }

    public (bool Valid, List<BStmt> Statements) CheckNonFunctionTypeArguments(List<Variance> variances, List<RefinementAndType> startArguments, List<RefinementAndType> targetArguments, Dictionary<Variable, Variable> substitutions)
    {
        List<BStmt> statements = [];

        for (int i = 0; i < variances.Count; i++)
        {
            Variance variance = variances[i];
            RefinementAndType start = startArguments[i];
            RefinementAndType target = targetArguments[i];

            switch (variance)
            {
                case Variance.Covariant:
                    {
                        // start -> target
                        (bool valid, List<BStmt> stmts) = CheckRefinementTypeInRefinementType(start, target, substitutions);
                        statements.AddRange(stmts);
                        if (!valid)
                        {
                            return (false, []);
                        }
                        break;
                    }
                case Variance.Contravariant:
                    {
                        // target -> start
                        (bool valid, List<BStmt> stmts) = CheckRefinementTypeInRefinementType(target, start, substitutions);
                        statements.AddRange(stmts);
                        if (!valid)
                        {
                            return (false, []);
                        }
                        break;
                    }
                case Variance.None:
                default:
                    {
                        // start -> target
                        (bool valid, List<BStmt> stmts) = CheckRefinementTypeInRefinementType(start, target, substitutions);
                        statements.AddRange(stmts);
                        if (!valid)
                        {
                            return (false, []);
                        }
                        // target -> start
                        (valid, stmts) = CheckRefinementTypeInRefinementType(target, start, substitutions);
                        statements.AddRange(stmts);
                        if (!valid)
                        {
                            return (false, []);
                        }
                        break;
                    }

            }
        }
        return (true, statements);
    }

    public bool CheckGenericTypeInGenericType(Generic startGeneric, Generic targetGeneric)
    {
        if (startGeneric == targetGeneric)
        {
            return true;
        }
        return false;
    }

    public (bool Valid, List<BStmt> Statements) CheckRefinementsOnVariable(Variable variable, Refinement refinement, Dictionary<Variable, Variable> aboveSubstitutions, bool assumption)
    {
        Dictionary<Variable, Variable> substitutions = new([.. aboveSubstitutions, new KeyValuePair<Variable, Variable>(refinement.Value, variable)]);
        (var tStatements, Variable confirmVar) = SubstituteRefinementsOnVariable(refinement, variable, substitutions);
        List<BStmt> statements = [];
        foreach (TStmt stmt in tStatements)
        {
            (bool valid, List<BStmt> stmts) = CheckStatement(stmt);
            if (!valid)
            {
                return (false, []);
            }
            statements.AddRange(stmts);
        }
        // assert or assume the confirmVar
        if (assumption)
        {
            statements.Add(new BStmt.Assumption(confirmVar));
        }
        else
        {
            statements.Add(new BStmt.Assertion(confirmVar));
        }
        return (true, statements);
    }

    public (List<TStmt> Statements, Variable ConfirmVariable) SubstituteRefinementsOnVariable(Refinement refinement, Variable target, Dictionary<Variable, Variable> otherSubstitutions)
    {
        Variable confirm = new Variable("Confirm");
        List<TStmt> statements = new();
        foreach (TStmt statement in refinement.TypingStatements)
        {
            statements.Add(SubstituteStatement(refinement, statement, target, confirm, otherSubstitutions));
        }
        return (statements, confirm);
    }

    public TStmt SubstituteStatement(Refinement refinement, TStmt statement, Variable target, Variable confirm, Dictionary<Variable, Variable> otherSubstitutions)
    {
        switch (statement)
        {
            case TStmt.Assignment assignment:
                return SubstituteAssignmentStatement(refinement, assignment, target, confirm, otherSubstitutions);
            case TStmt.AssertType assertion:
                return SubstituteAssertion(refinement, assertion, target, confirm, otherSubstitutions);
            case TStmt.AssumeType assumption:
                return SubstituteAssumption(refinement, assumption, target, confirm, otherSubstitutions);
            case TStmt.Z3Assumption z3assumption:
                return SubstituteZ3Assumption(refinement, z3assumption, target, confirm, otherSubstitutions);
            default:
                throw new Exception("Invalid statement");
        }
    }


    public TStmt SubstituteAssertion(Refinement refinement, TStmt.AssertType statement, Variable target, Variable confirm, Dictionary<Variable, Variable> otherSubstitutions)
    {
        RefinementAndType newType = SubstituteVariablesInRefinedType(statement.Type, new Dictionary<Variable, Variable>(otherSubstitutions));
        return new TStmt.AssertType(newType, otherSubstitutions.ContainsKey(statement.Variable) ? otherSubstitutions[statement.Variable] : statement.Variable);
    }

    public TStmt SubstituteAssumption(Refinement refinement, TStmt.AssumeType statement, Variable target, Variable confirm, Dictionary<Variable, Variable> otherSubstitutions)
    {
        RefinementAndType newType = SubstituteVariablesInRefinedType(statement.Type, new Dictionary<Variable, Variable>(otherSubstitutions));
        return new TStmt.AssumeType(newType, otherSubstitutions.ContainsKey(statement.Variable) ? otherSubstitutions[statement.Variable] : statement.Variable);
    }

    public TStmt SubstituteZ3Assumption(Refinement refinement, TStmt.Z3Assumption statement, Variable target, Variable confirm, Dictionary<Variable, Variable> otherSubstitutions)
    {
        List<(RefinementAndType, Variable)> newArguments = [..from arg in statement.Arguments select (SubstituteVariablesInRefinedType(arg.Item1, otherSubstitutions), otherSubstitutions.ContainsKey(arg.Item2) ? otherSubstitutions[arg.Item2] : arg.Item2)];
        Z3AssumptionFunction newFunction = new(statement.AssumptionFunction.Name, statement.AssumptionFunction.Function);
        Variable variable = statement.CheckVar;
        Variable replacementVariable;
        if (variable == refinement.Value)
        {
            throw new Exception("cannot substitute assignemnt of the target variable!");
        }
        else if (variable == refinement.ConfirmVariable)
        {
            replacementVariable = confirm;
        }
        else if (Environment.ContainsKey(variable))
        {
            replacementVariable = variable;
        }
        else
        {
            replacementVariable = new Variable(variable.Name);
        }
        otherSubstitutions[variable] = replacementVariable;
        return new TStmt.Z3Assumption(statement.AssumptionFunction, replacementVariable, newArguments);
    }

    public TStmt SubstituteAssignmentStatement(Refinement refinement, TStmt.Assignment statement, Variable target, Variable confirm, Dictionary<Variable, Variable> otherSubstitutions)
    {
        Variable variable = statement.Variable;
        Variable replacementVariable;
        if (variable == refinement.Value)
        {
            throw new Exception("cannot substitute assignemnt of the target variable!");
        }
        else if (variable == refinement.ConfirmVariable)
        {
            replacementVariable = confirm;
        }
        else if (Environment.ContainsKey(variable))
        {
            replacementVariable = variable;
        }
        else
        {
            replacementVariable = new Variable(variable.Name);
        }
        otherSubstitutions[variable] = replacementVariable;
        RefinementAndType newType = SubstituteVariablesInRefinedType(statement.Type, new Dictionary<Variable, Variable>(otherSubstitutions));

        switch (statement.Value)
        {
            case TExpr.BoolConstant boolConstant:
                {
                    return new TStmt.Assignment(newType, replacementVariable, new TExpr.BoolConstant(boolConstant.Value));
                }
            case TExpr.FunctionCall functionCall:
                {
                    // substitute the function and its arguments
                    Variable function = otherSubstitutions.ContainsKey(functionCall.Function) ? otherSubstitutions[functionCall.Function] : functionCall.Function;
                    List<Variable> arguments = [.. from arg in functionCall.Arguments select otherSubstitutions.ContainsKey(arg) ? otherSubstitutions[arg] : arg];
                    return new TStmt.Assignment(newType, replacementVariable, new TExpr.FunctionCall(function, arguments));
                }
            case TExpr.IntConstant intConstant:
                {
                    return new TStmt.Assignment(newType, replacementVariable, new TExpr.IntConstant(intConstant.Value));
                }
            case TExpr.VariableRead variableRead:
                {
                    // substitute the variable being read
                    return new TStmt.Assignment(newType, replacementVariable, new TExpr.VariableRead(otherSubstitutions.ContainsKey(variableRead.Variable) ? otherSubstitutions[variableRead.Variable] : variableRead.Variable));
                }
            default:
                throw new Exception("Invalid expression");
        }
    }

    public RefinementAndType SubstituteVariablesInRefinedType(RefinementAndType type, Dictionary<Variable, Variable> substitutions)
    {
        Generic generic = type.Type.Base;
        List<RefinementAndType> arguments = [.. from arg in type.Type.Arguments select SubstituteVariablesInRefinedType(arg, substitutions)];
        RefinementAndType substitutedType = new RefinementAndType(new Type(generic, arguments, type.Type.FunctionVariables), null);
        if (type.Refinement is not null)
        {
            List<TStmt> substitutedStatements = new();
            foreach (TStmt statement in type.Refinement.TypingStatements)
            {
                substitutedStatements.Add(SubstituteVariablesInStatement(statement, substitutions));
            }
            Refinement refinement = type.Refinement with { TypingStatements = substitutedStatements };
            substitutedType = substitutedType with { Refinement = refinement };
        }
        return substitutedType;
    }

    public TStmt SubstituteVariablesInStatement(TStmt statement, Dictionary<Variable, Variable> substitutions)
    {
        switch (statement)
        {
            case TStmt.Assignment assignment:
                return SubstituteVariablesInAssignmentStatement(assignment, substitutions);
            case TStmt.AssertType assertion:
                return SubstituteVariablesInAssertion(assertion, substitutions);
            case TStmt.AssumeType assumption:
                return SubstituteVariablesInAssumption(assumption, substitutions);
            case TStmt.Z3Assumption z3Assumption:
                return SubstituteVariablesInZ3Assumption(z3Assumption, substitutions);
            default:
                throw new Exception("Invalid statement");
        }
    }


    public TStmt SubstituteVariablesInAssertion(TStmt.AssertType statement, Dictionary<Variable, Variable> substitutions)
    {
        RefinementAndType newType = SubstituteVariablesInRefinedType(statement.Type, new Dictionary<Variable, Variable>(substitutions));
        return new TStmt.AssumeType(newType, substitutions.ContainsKey(statement.Variable) ? substitutions[statement.Variable] : statement.Variable);
    }

    public TStmt SubstituteVariablesInAssumption(TStmt.AssumeType statement, Dictionary<Variable, Variable> substitutions)
    {
        RefinementAndType newType = SubstituteVariablesInRefinedType(statement.Type, new Dictionary<Variable, Variable>(substitutions));
        return new TStmt.AssumeType(newType, substitutions.ContainsKey(statement.Variable) ? substitutions[statement.Variable] : statement.Variable);
    }

    public TStmt SubstituteVariablesInZ3Assumption(TStmt.Z3Assumption statement, Dictionary<Variable, Variable> substitutions)
    {
        List<(RefinementAndType, Variable)> newArguments = [..from arg in statement.Arguments select (SubstituteVariablesInRefinedType(arg.Item1, substitutions), substitutions.ContainsKey(arg.Item2) ? substitutions[arg.Item2] : arg.Item2)];
        if (substitutions.ContainsKey(statement.CheckVar))
        {
            throw new Exception($"Substituting variable in z3 assignment statement, but assignment was made to a variable that was to be substituted. Substitution removed.");

        }
        return new TStmt.Z3Assumption(statement.AssumptionFunction, statement.CheckVar, newArguments);
    }

    public TStmt SubstituteVariablesInAssignmentStatement(TStmt.Assignment statement, Dictionary<Variable, Variable> substitutions)
    {
        Variable variable = statement.Variable;
        if (substitutions.ContainsKey(variable))
        {
            substitutions.Remove(variable);
            Console.WriteLine($"Substituting variable in assignment statement, but assignment was made to a variable that was to be substituted. Substitution removed.");
        }
        switch (statement.Value)
        {
            case TExpr.BoolConstant boolConstant:
                {
                    RefinementAndType newType = SubstituteVariablesInRefinedType(statement.Type, new Dictionary<Variable, Variable>(substitutions));
                    return new TStmt.Assignment(newType, variable, new TExpr.BoolConstant(boolConstant.Value));
                }
            case TExpr.FunctionCall functionCall:
                {
                    RefinementAndType newType = SubstituteVariablesInRefinedType(statement.Type, new Dictionary<Variable, Variable>(substitutions));
                    return new TStmt.Assignment(newType, variable, new TExpr.FunctionCall(substitutions.ContainsKey(functionCall.Function) ? substitutions[functionCall.Function] : functionCall.Function, [..from arg in functionCall.Arguments select substitutions.ContainsKey(arg) ? substitutions[arg] : arg]));
                }
            case TExpr.IntConstant intConstant:
                {
                    RefinementAndType newType = SubstituteVariablesInRefinedType(statement.Type, new Dictionary<Variable, Variable>(substitutions));
                    return new TStmt.Assignment(newType, variable, new TExpr.IntConstant(intConstant.Value));
                }
            case TExpr.VariableRead variableRead:
                {
                    RefinementAndType newType = SubstituteVariablesInRefinedType(statement.Type, new Dictionary<Variable, Variable>(substitutions));
                    return new TStmt.Assignment(newType, variable, new TExpr.VariableRead(substitutions.ContainsKey(variableRead.Variable) ? substitutions[variableRead.Variable] : variableRead.Variable));
                }
            default:
                throw new Exception("Invalid expression");
        }
    }
}