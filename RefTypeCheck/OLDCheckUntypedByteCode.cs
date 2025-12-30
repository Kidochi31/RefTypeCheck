

internal class OLDCheckUntypedByteCode()
{
    // public bool CheckBlock(UBBlock block, Dictionary<UBVar, RefinementType> variableTypes)
    // {
    //     switch (block)
    //     {
    //         case UBBlock.Basic basicBlock:
    //             return CheckBasicBlock(basicBlock, variableTypes);
    //         default:
    //             throw new Exception("Invalid block");
    //     }
    // }

    // public bool CheckBasicBlock(UBBlock.Basic basicBlock, Dictionary<UBVar, RefinementType> variableTypes)
    // {
    //     // go through all the statements and type them
    //     bool valid = true;
    //     foreach(UBStmt statement in basicBlock.Body)
    //     {
    //         if(!CheckStatement(statement, variableTypes))
    //         {
    //             valid = false;
    //         }
    //     }
    //     return valid;
    // }

    // public bool CheckStatement(UBStmt statement, Dictionary<UBVar, RefinementType> variableTypes)
    // {
    //     switch (statement)
    //     {
    //         case UBStmt.Assignment assignment:
    //             return CheckAssignmentStatement(assignment, variableTypes);
    //         default:
    //             throw new Exception("Invalid statement");
    //     }
    // }

    // public bool CheckAssignmentStatement(UBStmt.Assignment statement, Dictionary<UBVar, RefinementType> variableTypes)
    // {
    //     // assignment statement has two parts: variable, and expression
    //     // the type of the expression must match the type of the variable
    //     UBVar variable = statement.Variable;
    //     switch (statement.Value)
    //     {
    //         case UBExpr.BoolConstant boolConstant:
    //             {
    //                 // variable will be typed as a bool
    //                 variableTypes[variable] = UBType.BoolType;
    //                 return true;
    //             }
    //         case UBExpr.FunctionCall functionCall:
    //             {
    //                 // first, the function variable must be typed as a function
    //                 UBType functionType = variableTypes[functionCall.Function];
    //                 if (!UBGenericType.FunctionTypes.Contains(functionType.GenericType))
    //                 {
    //                     Console.WriteLine("non-function type!");
    //                     return false;
    //                 }
                    
    //                 // next, the number of arguments must match the number of parameters to the function - 1
    //                 int num_args = functionType.Parameters.Count - 1;
    //                 if(functionCall.Arguments.Count != num_args)
    //                 {
    //                     Console.WriteLine("wrong number of args!");
    //                     return false;
    //                 }

    //                 // next, all of the arguments must be typed correctly

    //             }
    //         case UBExpr.IntConstant intConstant:
    //             {
    //                 // variable will be typed as an int
    //                 variableTypes[variable] = UBType.IntType;
    //                 return true;
    //             }
    //         case UBExpr.VariableRead variableRead:
    //             {
    //                 // variable will be typed the same as the read variable
    //                 variableTypes[variable] = variableTypes[variableRead.Variable];
    //                 return true;
    //             }
    //         default:
    //             throw new Exception("Invalid expression");
    //     }
    // }
}