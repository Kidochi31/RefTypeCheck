

internal class CheckByteCode(Z3.Context context)
{
    Z3.Context Context = context;
    public Z3Management Management = new Z3Management(context);

    public (bool Valid, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckBlock(BBlock block, Z3.BoolExpr context)
    {
        switch (block)
        {
            case BBlock.Basic basicBlock:
                return CheckBasicBlock(basicBlock, context);
            default:
                throw new Exception("Invalid block");
        }
    }

    public (bool Valid, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckBasicBlock(BBlock.Basic basicBlock, Z3.BoolExpr context)
    {
        // go through all the statements and check them
        foreach(BStmt statement in basicBlock.Body)
        {
            (bool valid, context, Z3.Model? counterexample) = CheckStatement(statement, context);
            if (!valid)
            {
                return (false, Context.MkBool(false), counterexample);
            }
        }
        return (true, context, null);
    }
    
    public (bool Valid, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckStatement(BStmt statement, Z3.BoolExpr context)
    {
        switch (statement)
        {
            case BStmt.Assignment assignment:
                return CheckAssignmentStatement(assignment, context);
            case BStmt.Assertion assertion:
                return CheckAssertion(assertion, context);
            case BStmt.Assumption assumption:
                return CheckAssumption(assumption, context);
            case BStmt.Z3Assumption z3assumption:
                return CheckZ3Assumption(z3assumption, context);
            default:
                throw new Exception("Invalid statement");
        }
    }

    public (bool Valid, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckZ3Assumption (BStmt.Z3Assumption statement, Z3.BoolExpr context)
    {
        // an assumption adds the statement to the context
        context = Context.MkAnd(context, statement.AssumptionFunction(Management, statement.Arguments));
        return (true, context, null);
    }

    public (bool Valid, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckAssertion (BStmt.Assertion statement, Z3.BoolExpr context)
    {
        // an assertion checks that the given variable will always be true
        // check implication of context -> variable
        // then add the variable to the context
        Z3ImplicationResult result = Z3Check.CheckImplication(context, Management.GetZ3BoolVariable(statement.Variable), Context);
        context = Context.MkAnd(context, Management.GetZ3BoolVariable(statement.Variable));
        return (result.Status == Z3ImplicationStatus.Proven, context, result.Counterexample);
    }

    public (bool Valid, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckAssumption (BStmt.Assumption statement, Z3.BoolExpr context)
    {
        // an assumption adds the statement to the context
        context = Context.MkAnd(context, Management.GetZ3BoolVariable(statement.Variable));
        return (true, context, null);
    }

    public (bool Valid, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckAssignmentStatement(BStmt.Assignment statement, Z3.BoolExpr context)
    {
        // assignment statement has two parts: variable, and expression
        // the type of the expression must match the type of the variable
        Variable variable = statement.Variable;
        switch (statement.Value)
        {
            case BExpr.BoolConstant boolConstant:
                {
                    // add var = constant to the context
                    
                    Z3.BoolExpr assignmentRefinement = Context.MkEq(Management.GetZ3BoolVariable(variable), Context.MkBool(boolConstant.Value));
                    context = Context.MkAnd(context, assignmentRefinement);
                    break;
                }
            case BExpr.FunctionCall functionCall:
                {
                    // add var = function(args) to the context
                    Z3.Expr functionResult = Management.ApplyFunc(functionCall.Function, functionCall.Arguments);
                    Z3.BoolExpr assignmentRefinement = Context.MkEq(Management.GetZ3Variable(variable), functionResult);
                    context = Context.MkAnd(context, assignmentRefinement);
                    break;
                }
            case BExpr.IntConstant intConstant:
                {
                    // add var = constant to the context
                    Z3.BoolExpr assignmentRefinement = Context.MkEq(Management.GetZ3IntVariable(variable), Context.MkInt(intConstant.Value));
                    context = Context.MkAnd(context, assignmentRefinement);
                    break;
                }
            case BExpr.VariableRead variableRead:
                {
                    // add var = variable to the context
                    Z3.BoolExpr assignmentRefinement = Context.MkEq(Management.GetZ3Variable(variable), Management.GetZ3Variable(variableRead.Variable));
                    context = Context.MkAnd(context, assignmentRefinement);
                    break;
                }
            default:
                throw new Exception("Invalid expression");
        }
        // assignment does no checking -> it merely adds to context -> return true
        // NOTE: maybe add checking to check that assignment is not impossible at all (i.e. checking that context does not evaluate to false)
        return (true, context, null);
    }
}