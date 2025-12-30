

internal class CheckBC(Z3.Context context)
{
    Z3.Context Context = context;
    // base types are already checked
    // need to check refinements with z3

    public (bool Valid, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckBlock(UBlock block, Z3.BoolExpr context)
    {
        switch (block)
        {
            case UBlock.Basic basicBlock:
                return CheckBasicBlock(basicBlock, context);
            case UBlock.If ifBlock:
                return CheckIfBlock(ifBlock, context);
            default:
                throw new Exception("Invalid block");
        }
    }

    public (bool Valid, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckBasicBlock(UBlock.Basic basicBlock, Z3.BoolExpr context)
    {
        // go through all the statements and check them
        foreach(UStmt statement in basicBlock.Body)
        {
            (bool valid, context, Z3.Model? counterexample) = CheckStatement(statement, context);
            if (!valid)
            {
                return (false, Context.MkBool(false), counterexample);
            }
        }
        return (true, context, null);
    }

    public (bool Valid, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckIfBlock(UBlock.If ifBlock, Z3.BoolExpr context)
    {
        return (false, Context.MkBool(false), null);
    }

    public (bool Valid, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckStatement(UStmt statement, Z3.BoolExpr context)
    {
        switch (statement)
        {
            case UStmt.Assignment assignment:
                return CheckAssignmentStatement(assignment, context);
            default:
                throw new Exception("Invalid statement");
        }
    }

    public (bool Valid, Z3.BoolExpr NewContext, Z3.Model? Counterexample) CheckAssignmentStatement(UStmt.Assignment statement, Z3.BoolExpr context)
    {
        // assignment statement has two parts: variable, and expression
        // the type of the expression must match the type of the variable
        UVar variable = statement.Variable;
        switch (statement.Value)
        {
            case UExpr.BoolConstant boolConstant:
                {
                    // add var = constant to the context
                    
                    Z3.BoolExpr assignmentRefinement = Context.MkEq(variable.GetZ3Variable(Context), Context.MkBool(boolConstant.Value));
                    context = Context.MkAnd(context, assignmentRefinement);
                    break;
                }
            case UExpr.FunctionCall functionCall:
                {
                    // the function call requires all the arguments to imply the parameter types
                    Z3.BoolExpr functionParameterImplication = Context.MkAnd(from (UVar arg, UVar par) x in functionCall.Arguments.Zip(functionCall.Function.Parameters) select x.par.Type.GetSubstitutedZ3Expr(Context, x.arg.GetZ3Variable(Context), functionCall.Function.Parameters, from arg in functionCall.Arguments select arg.GetZ3Variable(Context)));
                    Z3ImplicationResult functionParameterResult = Z3Check.CheckImplication(context, functionParameterImplication, Context);
                    

                    // add var = function(args) to the context
                    Z3.BoolExpr assignmentRefinement = Context.MkEq(variable.GetZ3Variable(Context), Context.MkApp(functionCall.Function.Z3Function, from arg in functionCall.Arguments select arg.GetZ3Variable(Context)));
                    context = Context.MkAnd(context, assignmentRefinement);

                    // get the function output type, and apply it to the target variable
                    Z3.BoolExpr outputRefinement = functionCall.Function.ReturnType.GetSubstitutedZ3Expr(Context, variable.GetZ3Variable(Context), functionCall.Function.Parameters, from arg in functionCall.Arguments select arg.GetZ3Variable(Context));
                    context = Context.MkAnd(context, outputRefinement);

                    // don't bother checking assignment if function parameters are wrong
                    if(functionParameterResult.Status != Z3ImplicationStatus.Proven)
                    {
                        return (false, Context.MkBool(false), functionParameterResult.Counterexample);
                    }

                    break;
                }
            case UExpr.IntConstant intConstant:
                {
                    // add var = constant to the context
                    Z3.BoolExpr assignmentRefinement = Context.MkEq(variable.GetZ3Variable(Context), Context.MkInt(intConstant.Value));
                    context = Context.MkAnd(context, assignmentRefinement);
                    break;
                }
            case UExpr.VariableRead variableRead:
                {
                    // add var = variable to the context
                    Z3.BoolExpr assignmentRefinement = Context.MkEq(variable.GetZ3Variable(Context), variableRead.Variable.GetZ3Variable(Context));
                    context = Context.MkAnd(context, assignmentRefinement);
                    break;
                }
            default:
                throw new Exception("Invalid expression");
        }
        // the assignment is true if the context is a premise that implies the conclusion of the variable's type
        Z3.BoolExpr varTypeRefinement = variable.Type.GetSubstitutedZ3Expr(Context, variable.GetZ3Variable(Context));
        Z3ImplicationResult result = Z3Check.CheckImplication(context, varTypeRefinement, Context);
        Console.WriteLine($"Context: {context}");
        Console.WriteLine($"Goal: {varTypeRefinement}");
        return (result.Status == Z3ImplicationStatus.Proven, context, result.Counterexample);
    }
}