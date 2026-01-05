

internal class CheckTypedByteCode(Type intType, Type boolType, List<Generic> functionTypes, CheckByteCode byteCodeChecker)
{
    Type IntType = intType;
    Type BoolType = boolType;
    List<Generic> FunctionTypes = functionTypes;
    Dictionary<Variable, RefinementAndType> Environment = new();
    CheckByteCode ByteCodeChecker = byteCodeChecker;

    // This will produce the untyped refinement checking byte code
    // And it will also check the base types of the code

    public (bool Valid, BBlock? Block, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckBlock(TBlock block, Z3.BoolExpr context)
    {
        switch (block)
        {
            case TBlock.Basic basicBlock:
                return CheckBasicBlock(basicBlock, context);
            default:
                throw new Exception("Invalid block");
        }
    }

    public (bool Valid, BBlock? Block, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckBasicBlock(TBlock.Basic basicBlock, Z3.BoolExpr context)
    {
        List<BStmt> statements = new();
        // go through all the statements and check them
        foreach (TStmt statement in basicBlock.Body)
        {
            (bool valid, var stmts, context, var counterexample) = CheckStatement(statement, context);
            statements.AddRange(stmts);
            if (!valid)
            {
                return (false, new BBlock.Basic(statements), context, counterexample);
            }
        }
        return (true, new BBlock.Basic(statements), context, null);
    }

    public (bool Valid, List<BStmt> Statement, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckStatement(TStmt statement, Z3.BoolExpr context)
    {
        switch (statement)
        {
            case TStmt.Assignment assignment:
                return CheckAssignmentStatement(assignment, context);
            case TStmt.AssertType assertion:
                return CheckAssertion(assertion, context);
            case TStmt.AssumeType assumption:
                return CheckAssumption(assumption, context);
            case TStmt.Z3Assumption z3assumptions:
                return CheckZ3Assumption(z3assumptions, context);
            default:
                throw new Exception("Invalid statement");
        }
    }


    public (bool Valid, List<BStmt> Statement, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckAssertion(TStmt.AssertType statement, Z3.BoolExpr context)
    {
        // assert the variable is part of the type
        return CheckVariableInRefinedType(statement.Variable, Environment[statement.Variable].Type, statement.Type, [], context);
    }

    public (bool Valid, List<BStmt> Statement, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckAssumption(TStmt.AssumeType statement, Z3.BoolExpr context)
    {
        // assume the variable is part of the type
        List<BStmt> statements = new();

        // the given base type must be more specific than the current type of the variable (if it has one)
        if (Environment.ContainsKey(statement.Variable))
        {
            (bool valid, var stmts, context, var counterexample) = CheckTypeInType(statement.Type.Type, Environment[statement.Variable].Type, [], context);
            statements.AddRange(stmts);
            if (!valid)
            {
                return (false, statements, context, counterexample);
            }
        }
        // now give it that type
        Environment[statement.Variable] = statement.Type;

        // now assert the refinement
        if (statement.Type.Refinement is not null)
        {
            (bool refinementResult, var refinementStatements, context, var counterexample) = CheckRefinementsOnVariable(statement.Variable, statement.Type.Refinement, [], true, context);
            statements.AddRange(refinementStatements);
            if (!refinementResult)
            {
                return (false, statements, context, counterexample);
            }
        }

        return (true, statements, context, null);
    }

    public (bool Valid, List<BStmt> Statement, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckZ3Assumption(TStmt.Z3Assumption statement, Z3.BoolExpr context)
    {
        List<BStmt> statements = new();

        // go through each statement and check its type
        foreach((RefinementAndType type, Variable variable) in statement.Arguments)
        {
            (bool refinementResult, var refinementStatements, context, var counterexample) = CheckVariableInRefinedType(variable, Environment[variable].Type, type, [], context);
            statements.AddRange(refinementStatements);
            if (!refinementResult)
            {
                return (false, statements, context, counterexample);
            }
        }

        // the checkvar is a boolean
        Environment[statement.CheckVar] = new(new TypeOrTypeVariable.Type(BoolType), null);

        BStmt newStatement = new BStmt.Z3Assumption(statement.AssumptionFunction, statement.CheckVar, [.. from arg in statement.Arguments select arg.Item2]);
        statements.Add(newStatement);
        (bool assignmentValid, context, var assignmentCounterexample) = ByteCodeChecker.CheckStatement(newStatement, context);
        if (!assignmentValid)
        {
            return (false, statements, context, assignmentCounterexample);
        }
        return (true, statements, context, null);
    }

    public (bool Valid, List<BStmt> Statements, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckAssignmentStatement(TStmt.Assignment statement, Z3.BoolExpr context)
    {
        // assignment statement has two parts: variable, and expression
        // the type of the expression must match the type of the variable
        List<BStmt> statements = new List<BStmt>();
        Variable variable = statement.Variable;
        Environment[variable] = statement.Type;
        TypeOrTypeVariable expressionBaseType;
        Dictionary<Variable, Variable> substitutions;
        switch (statement.Value)
        {
            case TExpr.BoolConstant boolConstant:
                {
                    // add var = constant to the statement
                    BStmt newStatement = new BStmt.Assignment(variable, new BExpr.BoolConstant(boolConstant.Value));
                    statements.Add(newStatement);
                    (bool assignmentValid, context, var assignmentCounterexample) = ByteCodeChecker.CheckStatement(newStatement, context);
                    if (!assignmentValid)
                    {
                        return (false, statements, context, assignmentCounterexample);
                    }
                    expressionBaseType = new TypeOrTypeVariable.Type(BoolType);
                    substitutions = [];
                    break;
                }
            case TExpr.FunctionCall functionCall:
                {
                    // get the function
                    Variable function = functionCall.Function;
                    RefinementAndType functionType = Environment[function];
                    Type functionBase = functionType.Type is TypeOrTypeVariable.Type ? ((TypeOrTypeVariable.Type)functionType.Type).T : throw new Exception("function type cannot be a type variable yet!");
                    // check that the type of the function is a function type
                    if (!FunctionTypes.Contains(functionBase.Base))
                    {
                        Console.WriteLine("function given is not of a function type!");
                        return (false, statements, context, null);
                    }
                    // check that arguments will fit into function
                    List<RefinementAndType> parameterTypes = functionBase.Arguments[0..^1];
                    RefinementAndType resultType = functionBase.Arguments[^1];
                    if (functionCall.Arguments.Count != parameterTypes.Count)
                    {
                        Console.WriteLine("function arguments do not match parameter length");
                        return (false, statements, context, null);
                    }

                    substitutions = [];
                    (bool valid, List<BStmt> stmts, context, var funcCounterexample) = CheckFunctionApplicationType(function, functionBase, [..functionCall.Arguments, variable], substitutions, context);
                    statements.AddRange(stmts);
                    if (!valid)
                    {
                        return (false, statements, context, funcCounterexample);
                    }
                    expressionBaseType = resultType.Type;
                    break;
                }
            case TExpr.IntConstant intConstant:
                {
                    // add var = constant to the statement
                    BStmt newStatement = new BStmt.Assignment(variable, new BExpr.IntConstant(intConstant.Value));
                    statements.Add(newStatement);
                    (bool assignmentValid, context, var assignmentCounterexample) = ByteCodeChecker.CheckStatement(newStatement, context);
                    if (!assignmentValid)
                    {
                        return (false, statements, context, assignmentCounterexample);
                    }
                    expressionBaseType = new TypeOrTypeVariable.Type(IntType);
                    substitutions = [];
                    break;
                }
            case TExpr.VariableRead variableRead:
                {
                    // add var = variable to the statement
                    BStmt newStatement = new BStmt.Assignment(variable, new BExpr.VariableRead(variableRead.Variable));
                    statements.Add(newStatement);
                    (bool assignmentValid, context, var assignmentCounterexample) = ByteCodeChecker.CheckStatement(newStatement, context);
                    if (!assignmentValid)
                    {
                        return (false, statements, context, assignmentCounterexample);
                    }
                    expressionBaseType = Environment[variableRead.Variable].Type;
                    substitutions = [];
                    break;
                }
            default:
                throw new Exception("Invalid expression");
        }
        // now check that that the variable will be able to fit into its type
        (bool typeCheck, List<BStmt> moreStatements, context, var counterexample) = CheckVariableInRefinedType(variable, expressionBaseType, Environment[variable], substitutions, context);
        statements.AddRange(moreStatements);
        return (typeCheck, statements, context, counterexample);
    }

    public (bool Valid, List<BStmt> Statements, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckFunctionApplicationType(Variable functionVariable, Type functionType, List<Variable> arguments, Dictionary<Variable, Variable> substitutions, Z3.BoolExpr context)
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
                        BStmt assignmentStatement = new BStmt.Assignment(arguments[i], new BExpr.FunctionCall(functionVariable, inputs, i));
                        statements.Add(assignmentStatement);
                        // check the assignment
                        (bool assignmentValid, context, var assignmentCounterexample) = ByteCodeChecker.CheckStatement(assignmentStatement, context);
                        if (!assignmentValid)
                        {
                            return (false, statements, context, assignmentCounterexample);
                        }
                        // make assumptions about the output variable
                        if (typeArguments[i].Refinement is not null)
                        {
                            (bool targetValid, List<BStmt> targetStmts, context, var counterexample) = CheckRefinementsOnVariable(arguments[i], typeArguments[i].Refinement, new Dictionary<Variable, Variable>(substitutions), true, context);
                            statements.AddRange(targetStmts);
                            if (!targetValid)
                            {
                                return (false, statements, context, counterexample);
                            }
                        }
                        break;
                    }
                // if it is contravariant -> it is an input
                case Variance.Contravariant:
                    {
                        // create a substitution from the parameter variable to the argument
                        (bool argValid, List<BStmt> argStmts, context, var counterexample) = CheckVariableInRefinedType(arguments[i], Environment[arguments[i]].Type, typeArguments[i], new Dictionary<Variable, Variable>(substitutions), context);
                        statements.AddRange(argStmts);
                        if (!argValid)
                        {
                            return (false, statements, context, counterexample);
                        }
                        break;
                    }
                // if it is invariant -> raise an error
                case Variance.None:
                default:
                    {
                        Console.WriteLine("a function cannot have an invariant type argument!");
                        return (false, statements, context, null);
                    }
            }
        }
        return (true, statements, context, null);
    }


    public (bool Valid, List<BStmt> Statements, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckVariableInRefinedType(Variable variable, TypeOrTypeVariable variableBaseType, RefinementAndType targetType, Dictionary<Variable, Variable> substitutions, Z3.BoolExpr context)
    {
        // check that the variableBaseType goes into the target base type
        (bool baseResult, var baseStatements, context, var counterexample) = CheckTypeInType(variableBaseType, targetType.Type, new Dictionary<Variable, Variable>(substitutions), context);
        List<BStmt> statements = [.. baseStatements];
        if (!baseResult)
        {
            return (false, statements, context, counterexample);
        }

        // now assert the refinement
        if (targetType.Refinement is not null)
        {
            (bool refinementResult, var refinementStatements, context, var refinementCounterexample) = CheckRefinementsOnVariable(variable, targetType.Refinement, new Dictionary<Variable, Variable>(substitutions), false, context);
            statements.AddRange(refinementStatements);
            if (!refinementResult)
            {
                return (false, statements, context, refinementCounterexample);
            }
        }

        return (true, statements, context, null);
    }

    public (bool Valid, List<BStmt> Statements, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckRefinementTypeInRefinementType(RefinementAndType startType, RefinementAndType targetType, Dictionary<Variable, Variable> substitutions, Z3.BoolExpr context)
    {
        // check that the base type goes into the target base type
        // this will also add the required parameters to substitutions
        (bool baseResult, var baseStatements, context, var counterexample) = CheckTypeInType(startType.Type, targetType.Type, substitutions, context);
        List<BStmt> statements = [.. baseStatements];
        if (!baseResult)
        {
            return (false, statements, context, counterexample);
        }

        // now assert the refinement - only create the variable if there is a required refinement
        if (targetType.Refinement is not null)
        {
            Variable dummyVar = new Variable("dummy");
            Environment[dummyVar] = startType;
            if (startType.Refinement is not null)
            {
                (bool assumptionResults, var assumptionStatements, context, var assumptionCounterexample) = CheckRefinementsOnVariable(dummyVar, startType.Refinement, substitutions, true, context);
                statements.AddRange(assumptionStatements);
                if (!assumptionResults)
                {
                    return (false, statements, context, assumptionCounterexample);
                }
            }

            (bool refinementResult, var refinementStatements, context, var refinementCounterexample) = CheckRefinementsOnVariable(dummyVar, targetType.Refinement, substitutions, false, context);
            statements.AddRange(refinementStatements);
            if (!refinementResult)
            {
                return (false, statements, context, refinementCounterexample);
            }
        }

        return (true, statements, context, null);
    }

    public (bool Valid, List<BStmt> Statements, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckTypeInType(TypeOrTypeVariable startType, TypeOrTypeVariable targetType, Dictionary<Variable, Variable> substitutions, Z3.BoolExpr context)
    {
        if (startType is TypeOrTypeVariable.TypeVariable startTypeVar)
        {
            if(targetType is TypeOrTypeVariable.TypeVariable targetTypeVar)
            {
                // type vars can currently only be compared by equality
                return (startTypeVar == targetTypeVar, [], context, null);
            }
            else
            {
                Console.WriteLine("Currently, can only compare type variables by equality");
            return (false, [], context, null);
            }
        }

        if(targetType is TypeOrTypeVariable.TypeVariable)
        {
            Console.WriteLine("Currently, can only compare type variables by equality");
            return (false, [], context, null);
        }

        // the type must be a type
        Type startTypeType = ((TypeOrTypeVariable.Type)startType).T;
        Type targetTypeType = ((TypeOrTypeVariable.Type)targetType).T;

        // the base generic type must be the same
        if (!CheckGenericTypeInGenericType(startTypeType.Base, targetTypeType.Base))
        {
            return (false, [], context, null);
        }



        // if there are any type arguments, go through them and check variance
        if (startTypeType.Base == targetTypeType.Base)
        {
            Generic baseType = startTypeType.Base;

            List<Variance> variances = baseType.Parameters;
            if (startTypeType.Arguments.Count != variances.Count)
            {
                Console.WriteLine("type has wrong number of parameters");
                return (false, [], context, null);
            }
            if (targetTypeType.Arguments.Count != variances.Count)
            {
                Console.WriteLine("type has wrong number of parameters");
                return (false, [], context, null);
            }

            if (baseType.FunctionType)
            {
                // there are variable parameters to create here
                if (startTypeType.FunctionVariables.Count != variances.Count)
                {
                    Console.WriteLine("type has wrong number of variable parameters!");
                    return (false, [], context, null);
                }
                if (targetTypeType.FunctionVariables.Count != variances.Count)
                {
                    Console.WriteLine("type has wrong number of variable parameters!");
                    return(false, [], context, null);
                }

                Console.WriteLine("Not yet implemented assigning function types to function types.");
            }


            return CheckNonFunctionTypeArguments(variances, startTypeType.Arguments, targetTypeType.Arguments, substitutions, context);
        }
        else
        {
            return (false, [], context, null);
        }
    }

    public (bool Valid, List<BStmt> Statements, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckNonFunctionTypeArguments(List<Variance> variances, List<RefinementAndType> startArguments, List<RefinementAndType> targetArguments, Dictionary<Variable, Variable> substitutions, Z3.BoolExpr context)
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
                        (bool valid, List<BStmt> stmts, context, var counterexample) = CheckRefinementTypeInRefinementType(start, target, substitutions, context);
                        statements.AddRange(stmts);
                        if (!valid)
                        {
                            return (false, statements, context, counterexample);
                        }
                        break;
                    }
                case Variance.Contravariant:
                    {
                        // target -> start
                        (bool valid, List<BStmt> stmts, context, var counterexample) = CheckRefinementTypeInRefinementType(target, start, substitutions, context);
                        statements.AddRange(stmts);
                        if (!valid)
                        {
                            return (false, statements, context, counterexample);
                        }
                        break;
                    }
                case Variance.None:
                default:
                    {
                        // start -> target
                        (bool valid, List<BStmt> stmts, context, var counterexample) = CheckRefinementTypeInRefinementType(start, target, substitutions, context);
                        statements.AddRange(stmts);
                        if (!valid)
                        {
                            return (false, statements, context, counterexample);
                        }
                        // target -> start
                        (valid, stmts, context, counterexample) = CheckRefinementTypeInRefinementType(target, start, substitutions, context);
                        statements.AddRange(stmts);
                        if (!valid)
                        {
                            return (false, statements, context, counterexample);
                        }
                        break;
                    }

            }
        }
        return (true, statements, context, null);
    }

    public bool CheckGenericTypeInGenericType(Generic startGeneric, Generic targetGeneric)
    {
        if (startGeneric == targetGeneric)
        {
            return true;
        }
        return false;
    }

    public (bool Valid, List<BStmt> Statements, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckRefinementsOnVariable(Variable variable, Refinement refinement, Dictionary<Variable, Variable> aboveSubstitutions, bool assumption, Z3.BoolExpr context)
    {
        Dictionary<Variable, Variable> substitutions = new([.. aboveSubstitutions, new KeyValuePair<Variable, Variable>(refinement.Value, variable)]);
        (var tStatements, Variable confirmVar) = SubstituteRefinementsOnVariable(refinement, variable, substitutions);
        List<BStmt> statements = [];
        foreach (TStmt stmt in tStatements)
        {
            (bool valid, List<BStmt> stmts, context, var counterexample) = CheckStatement(stmt, context);
            statements.AddRange(stmts);
            if (!valid)
            {
                return (false, statements, context, counterexample);
            }
        }
        // assert or assume the confirmVar
        if (assumption)
        {
            BStmt newStmt = new BStmt.Assumption(confirmVar);
            statements.Add(newStmt);
            (bool valid, context, var counterexample) = ByteCodeChecker.CheckStatement(newStmt, context);
            return (valid, statements, context, counterexample);
        }
        else
        {
            BStmt newStmt = new BStmt.Assertion(confirmVar);
            statements.Add(newStmt);
            (bool valid, context, var counterexample) = ByteCodeChecker.CheckStatement(newStmt, context);
            return (valid, statements, context, counterexample);
        }
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
        if(type.Type is TypeOrTypeVariable.Type typeType)
        {
            Generic generic = typeType.T.Base;
            List<RefinementAndType> arguments = [.. from arg in typeType.T.Arguments select SubstituteVariablesInRefinedType(arg, substitutions)];
            type = new RefinementAndType(new TypeOrTypeVariable.Type(new Type(generic, arguments, typeType.T.FunctionVariables)), type.Refinement);
        }
        
        if (type.Refinement is not null)
        {
            List<TStmt> substitutedStatements = new();
            foreach (TStmt statement in type.Refinement.TypingStatements)
            {
                substitutedStatements.Add(SubstituteVariablesInStatement(statement, substitutions));
            }
            Refinement refinement = type.Refinement with { TypingStatements = substitutedStatements };
            type = type with { Refinement = refinement };
        }
        return type;
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