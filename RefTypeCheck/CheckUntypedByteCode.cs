

using System.Diagnostics.Metrics;
using System.Runtime.Serialization;

internal class CheckUntypedByteCode(Z3.Context context)
{
    Z3.Context Context = context;
    Z3SortFinder SortFinder = new Z3SortFinder(context);
    Dictionary<Variable, TypedVariable> Variables = new Dictionary<Variable, TypedVariable>();

    public (UBBlock<TypedVariable>? Block, Z3.BoolExpr NewContext, Z3.Model? Counterexample) TypeBlock(UBBlock<Variable> block, Z3.BoolExpr context)
    {
        switch (block)
        {
            case UBBlock<Variable>.Basic basicBlock:
                return TypeBasicBlock(basicBlock, context);
            default:
                throw new Exception("Invalid block");
        }
    }

    public (UBBlock<TypedVariable>? Block, Z3.BoolExpr NewContext, Z3.Model? Counterexample) TypeBasicBlock(UBBlock<Variable>.Basic basicBlock, Z3.BoolExpr context)
    {
        // go through all the statements and check them
        List<UBStmt<TypedVariable>> typedStatements = new();
        foreach(UBStmt<Variable> statement in basicBlock.Body)
        {
            (UBStmt<TypedVariable>? typedStatement, context, Z3.Model? counterexample) = TypeStatement(statement, context);
            if (typedStatement is null)
            {
                return (null, Context.MkBool(false), counterexample);
            }
            typedStatements.Add(typedStatement);
        }
        UBBlock<TypedVariable> typedBlock = new UBBlock<TypedVariable>.Basic(typedStatements);
        return (typedBlock, context, null);
    }

    public (UBStmt<TypedVariable>? typedStatement, Z3.BoolExpr NewContext, Z3.Model? Counterexample) TypeStatement(UBStmt<Variable> statement, Z3.BoolExpr context)
    {
        switch (statement)
        {
            case UBStmt<Variable>.Assignment assignment:
                return TypeAssignmentStatement(assignment, context);
            case UBStmt<Variable>.AssignZ3Expr assignZ3Expr:
                return TypeAssignZ3ExprStatement(assignZ3Expr, context);
            default:
                throw new Exception("Invalid statement");
        }
    }

    public (UBStmt<TypedVariable>? typedStatement, Z3.BoolExpr NewContext, Z3.Model? Counterexample) TypeAssignZ3ExprStatement(UBStmt<Variable>.AssignZ3Expr statement, Z3.BoolExpr context)
    {
        Variable target = statement.Variable;
        TypedVariable typedVar = new TypedVariable(target, statement.ExpectedType.BaseType);
        Variables[target] = typedVar;
        Z3.BoolExpr assignmentRefinement  = Context.MkEq(statement.Function((var) => {Console.WriteLine(var); return Variables[var].GetZ3Variable(SortFinder);}), typedVar.GetZ3Variable(SortFinder));
        context = Context.MkAnd(context, assignmentRefinement);
        (bool valid, context, var counterexample) = CheckVariableType(typedVar, statement.ExpectedType, [], context);
        if (!valid)
        {
            return (null, context, counterexample);
        }
        return (new UBStmt<TypedVariable>.AssignZ3Expr(typedVar, statement.ExpectedType, (func) => statement.Function((var) => Variables[var].GetZ3Variable(SortFinder))), context, counterexample);
}

    public (UBStmt<TypedVariable>? typedStatement, Z3.BoolExpr NewContext, Z3.Model? Counterexample) TypeAssignmentStatement(UBStmt<Variable>.Assignment statement, Z3.BoolExpr context)
    {
        // assignment statement has two parts: variable, and expression
        // the type of the expression must match the type of the variable
        Variable target = statement.Variable;
        TypedVariable typedVar;
        UBStmt<TypedVariable> typedStmt;

        switch (statement.Value)
        {
            case UBExpr<Variable>.BoolConstant boolConstant:
                {
                    // type the target as a bool
                    typedVar = new TypedVariable(target, BaseType.BoolType);
                    typedStmt = new UBStmt<TypedVariable>.Assignment(typedVar, statement.ExpectedType, new UBExpr<TypedVariable>.BoolConstant(boolConstant.Value));

                    // add var = constant to the context
                    Z3.BoolExpr assignmentRefinement = Context.MkEq(typedVar.GetZ3Variable(SortFinder), Context.MkBool(boolConstant.Value));
                    context = Context.MkAnd(context, assignmentRefinement);
                    break;
                }
            case UBExpr<Variable>.FunctionCall functionCall:
                {
                    TypedVariable function = Variables[functionCall.Function];
                    
                    // must ensure that the function has an invoke type
                    if(function.Type.InvokeType is null)
                    {
                        Console.WriteLine("Function has no invoke type!");
                        return (null, context, null);
                    }

                    // type the target as the base type of the return type
                    ValueType ReturnType = function.Type.InvokeType.ResultType;
                    typedVar = new TypedVariable(target, ReturnType.BaseType);

                    // get all the argument z3 sorts
                    List<ValueType> paramTypes = [.. from arg in function.Type.InvokeType.ParameterTypes select arg];
                    List<Z3Sort> argumentSorts = [.. from arg in paramTypes select arg.BaseType.GenericType.Sort];

                    // now can get the function
                    Z3.FuncDecl z3Function = SortFinder.GetFuncDecl(argumentSorts, ReturnType.BaseType.GenericType.Sort);

                    // get all parameter variables
                    List<TypedVariable> arguments = [.. from arg in functionCall.Arguments select Variables[arg]];

                    // ensure that paramTypes and arguments are the same length
                    if (paramTypes.Count != arguments.Count)
                    {
                        Console.WriteLine("Function arguments does not match parameter length");
                        return (null, context, null);
                    }
                    
                    // need to check that arguments match the types in the invoke type
                    Console.WriteLine(string.Join(',',function.Type.InvokeType.Parameters));
                    (bool argsValid, context, var argsCounterexample) = CheckVariableTypes([..arguments.Zip(paramTypes)], [..function.Type.InvokeType.Parameters.Zip(functionCall.Arguments)], context);
                    if (!argsValid)
                    {
                        return (null, context, argsCounterexample);
                    }

                    // add var = function(args) to the context
                    Z3.BoolExpr assignmentRefinement = Context.MkEq(typedVar.GetZ3Variable(SortFinder), Context.MkApp(z3Function, [function.GetZ3Variable(SortFinder), ..from arg in arguments select arg.GetZ3Variable(SortFinder)]));
                    context = Context.MkAnd(context, assignmentRefinement);

                    // additionally, if the function has any refinements to apply to the result, do so here
                    (bool returnValid, context, var returnCounterexample) = CheckVariableType(typedVar, ReturnType, [..function.Type.InvokeType.Parameters.Zip(functionCall.Arguments)], context, assumption:true);
                    if (!returnValid)
                    {
                        return (null, context, returnCounterexample);
                    }

                    typedStmt = new UBStmt<TypedVariable>.Assignment(typedVar, statement.ExpectedType, new UBExpr<TypedVariable>.FunctionCall(function, arguments));
                    break;
                }
            case UBExpr<Variable>.IntConstant intConstant:
                {
                    // type the target as a bool
                    typedVar = new TypedVariable(target, BaseType.IntType);
                    typedStmt = new UBStmt<TypedVariable>.Assignment(typedVar, statement.ExpectedType, new UBExpr<TypedVariable>.IntConstant(intConstant.Value));

                    // add var = constant to the context
                    Z3.BoolExpr assignmentRefinement = Context.MkEq(typedVar.GetZ3Variable(SortFinder), Context.MkInt(intConstant.Value));
                    context = Context.MkAnd(context, assignmentRefinement);
                    break;
                }
            case UBExpr<Variable>.VariableRead variableRead:
                {
                    // type the target as same as read variable
                    typedVar = new TypedVariable(target, Variables[variableRead.Variable].Type);
                    typedStmt = new UBStmt<TypedVariable>.Assignment(typedVar, statement.ExpectedType, new UBExpr<TypedVariable>.VariableRead(Variables[variableRead.Variable]));

                    // add var = variable to the context
                    Z3.BoolExpr assignmentRefinement = Context.MkEq(typedVar.GetZ3Variable(SortFinder), Variables[variableRead.Variable].GetZ3Variable(SortFinder));
                    context = Context.MkAnd(context, assignmentRefinement);
                    break;
                }
            default:
                throw new Exception("Invalid expression");
        }
        // the variable has been assigned -> must check it hasn't been assigned before
        if (Variables.ContainsKey(target))
        {
            throw new Exception("Variable has been assigned twice!");
        }
        Variables[target] = typedVar;

        // if there is a type given for the assignment, check that it is met
        if(statement.ExpectedType is not null)
        {
            (bool valid, context, var counterexample) = CheckVariableType(typedVar, statement.ExpectedType, [], context);
            if (!valid)
            {
                return (null, context, counterexample);
            }
        }

        return (typedStmt, context, null);
    }

    public (TypedVariable Variable, bool Valid, Z3.BoolExpr NewContext, Z3.Model? CounterExample) DeclareAssumedVariable(Variable variable, ValueType type, Z3.BoolExpr context)
    {
        TypedVariable typedVar = new TypedVariable(variable, type.BaseType);
        Variables[variable] = typedVar;
        (var valid, context, var counterexample) = CheckVariableType(typedVar, type, [], context, assumption:true);
        return (typedVar, valid, context, counterexample);
    }

    public (bool Valid, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckVariableType(TypedVariable variable, ValueType type, List<(Variable, Variable)> substitutions, Z3.BoolExpr context, bool assumption = false)
    {
        // the base type of the variable given here must be equal to the base type of the expected type
        if(type.BaseType != variable.Type)
        {
            Console.WriteLine("Variable base types do not match!");
            return (false, context, null);
        }
        // get the substituted type statements and type them
        Dictionary<Variable, Variable> substitutionDictionary = new Dictionary<Variable, Variable>(from s in substitutions select new KeyValuePair<Variable, Variable>(s.Item1, s.Item2));
        List<UBStmt<Variable>> substitutedRefinements = SubstituteTypeRefinement(type.Refinements, variable.Variable, substitutionDictionary);
        foreach((Variable from, Variable to) in substitutionDictionary)
        {
            Console.WriteLine($"from: {from} to {to}");
            Variables[from] = Variables[to];
        }
        foreach(var refinementStatement in substitutedRefinements)
        {
            (var statement, context, var model) = TypeStatement(refinementStatement, context);
            if(statement is null)
            {
                return (false, context, model);
            }
        }

        // assert the type's confirm variable
        if(type.Refinements.ConfirmVariable is not null)
        {
            TypedVariable assertVar = Variables[type.Refinements.ConfirmVariable];
            // it must be a boolean
            if (assertVar.Type != BaseType.BoolType)
            {
                Console.WriteLine("Assert variable must be a boolean!");
                return (false, context, null);
            }
            Z3.BoolExpr assertion = (Z3.BoolExpr)assertVar.GetZ3Variable(SortFinder);
            if(!assumption){
                var result = Z3Check.CheckImplication(context, assertion, Context);
                context = Context.MkAnd(context, assertion);
                return (result.Status == Z3ImplicationStatus.Proven, context, result.Counterexample);
            }
            else
            {
                context = Context.MkAnd(context, assertion);
            }
        }
        return (true, context, null);
    }

    public (bool Valid, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckVariableTypes(List<(TypedVariable, ValueType)> variables, List<(Variable, Variable)> substitutions, Z3.BoolExpr context, bool assumption=false)
    {
        foreach((var variable, var type) in variables){
            // the base type of the variable given here must be equal to the base type of the expected type
            if(type.BaseType != variable.Type)
            {
                Console.WriteLine("Variable base types do not match!");
                return (false, context, null);
            }
        }
        
        // get the substituted type statements and type them
        Dictionary<Variable, Variable> substitutionDictionary = new Dictionary<Variable, Variable>(from s in substitutions select new KeyValuePair<Variable, Variable>(s.Item1, s.Item2));
        foreach((var variable, var type) in variables)
        {
            List<UBStmt<Variable>> substitutedRefinements = SubstituteTypeRefinement(type.Refinements, variable.Variable, substitutionDictionary);
            foreach((Variable from, Variable to) in substitutionDictionary)
            {
                Variables[from] = Variables[to];
            }
            foreach(var refinementStatement in substitutedRefinements)
            {
                (var statement, context, var model) = TypeStatement(refinementStatement, context);
                if(statement is null)
                {
                    return (false, context, model);
                }
            }
        }
        foreach((Variable from, Variable to) in substitutionDictionary)
        {
            Variables[from] = Variables[to];
        }
        // assert the type confirm variable
        foreach((var variable, var type) in variables)
        {
            if(type.Refinements.ConfirmVariable is not null)
        {
            TypedVariable assertVar = Variables[type.Refinements.ConfirmVariable];
            // it must be a boolean
            if (assertVar.Type != BaseType.BoolType)
            {
                Console.WriteLine("Assert variable must be a boolean!");
                return (false, context, null);
            }
            Z3.BoolExpr assertion = (Z3.BoolExpr)assertVar.GetZ3Variable(SortFinder);
            if(!assumption){
                var result = Z3Check.CheckImplication(context, assertion, Context);
                context = Context.MkAnd(context, assertion);
                return (result.Status == Z3ImplicationStatus.Proven, context, result.Counterexample);
            }
            else
            {
                context = Context.MkAnd(context, assertion);
            }
        }
        }
        
        return (true, context, null);
    }

    List<UBStmt<Variable>> SubstituteTypeRefinement(TypeRefinement refinement, Variable typeVariable, Dictionary<Variable, Variable> otherSubstitutions)
    {
        otherSubstitutions[refinement.Value] = typeVariable;
        List<UBStmt<Variable>> substitutedStatements = new();
        foreach(UBStmt<Variable> statement in refinement.TypingStatements)
        {
            substitutedStatements.Add(SubstituteStatement(statement, otherSubstitutions, refinement.ConfirmVariable));
        }
        return substitutedStatements;
    }

    UBStmt<Variable> SubstituteStatement(UBStmt<Variable> statement, Dictionary<Variable, Variable> substitutions, Variable? checkVar)
    {
        switch (statement)
        {
            case UBStmt<Variable>.Assignment assignment:
                return SubstituteAssignment(assignment, substitutions, checkVar);
            case UBStmt<Variable>.AssignZ3Expr z3assignment:
                return SubstituteZ3Assignment(z3assignment, substitutions, checkVar);
            default:
                throw new Exception("Invalid statement");
        }
    }

    public UBStmt<Variable> SubstituteZ3Assignment(UBStmt<Variable>.AssignZ3Expr statement, Dictionary<Variable, Variable> substitutions, Variable? checkVar)
    {
        Variable target = statement.Variable;
        Variable newTarget = target == checkVar ? target : new Variable(target.Name);
        substitutions[target] = newTarget;
        return new UBStmt<Variable>.AssignZ3Expr(newTarget, statement.ExpectedType, statement.Function);
    }

    public UBStmt<Variable> SubstituteAssignment(UBStmt<Variable>.Assignment statement, Dictionary<Variable, Variable> substitutions, Variable? checkVar)
    {
        Variable target = statement.Variable;
        Variable newTarget = target == checkVar ? target : new Variable(target.Name);
        substitutions[target] = newTarget;

        switch (statement.Value)
        {
            case UBExpr<Variable>.BoolConstant boolConstant:
                {
                    return new UBStmt<Variable>.Assignment(newTarget, null, new UBExpr<Variable>.BoolConstant(boolConstant.Value));
                }
            case UBExpr<Variable>.FunctionCall functionCall:
                {
                    return new UBStmt<Variable>.Assignment(newTarget, null, new UBExpr<Variable>.FunctionCall(substitutions.ContainsKey(functionCall.Function) ? substitutions[functionCall.Function] : functionCall.Function,
                    [..from arg in functionCall.Arguments select substitutions.ContainsKey(arg) ? substitutions[arg] : arg]));
                }
            case UBExpr<Variable>.IntConstant intConstant:
                {
                    return new UBStmt<Variable>.Assignment(newTarget, null, new UBExpr<Variable>.IntConstant(intConstant.Value));
                }
            case UBExpr<Variable>.VariableRead variableRead:
                {
                    return new UBStmt<Variable>.Assignment(newTarget, null, new UBExpr<Variable>.VariableRead(substitutions.ContainsKey(variableRead.Variable) ? substitutions[variableRead.Variable] : variableRead.Variable));
                }
            default:
                throw new Exception("Invalid expression");
        }
    }
}