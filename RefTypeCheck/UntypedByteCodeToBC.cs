

// internal class UntypedByteCodeToBC()
// {
//     // base types are already checked
//     // need to check refinements with z3

//     public BBlock TypeBlock(UBBlock block)
//     {
//         switch (block)
//         {
//             case UBBlock.Basic basicBlock:
//                 return TypeBasicBlock(basicBlock);
//             default:
//                 throw new Exception("Invalid block");
//         }
//     }

//     public BBlock TypeBasicBlock(UBBlock.Basic basicBlock)
//     {
//         // go through all the statements and type them
//         List<BStmt> statements = new();
//         foreach(UBStmt statement in basicBlock.Body)
//         {
//             BStmt typedStatement = TypeStatement(statement);
//             statements.Add(typedStatement);
//         }
//         return new BBlock.Basic(statements);
//     }

//     public BStmt TypeStatement(UBStmt statement)
//     {
//         switch (statement)
//         {
//             case UBStmt.Assignment assignment:
//                 return TypeAssignmentStatement(assignment);
//             default:
//                 throw new Exception("Invalid statement");
//         }
//     }

//     public BStmt TypeAssignmentStatement(UBStmt.Assignment statement, Dictionary<Variable, ValueType> variables)
//     {
//         // assignment statement has two parts: variable, and expression
//         // the type of the expression must match the type of the variable
//         Variable variable = statement.Variable;
//         ValueType? expectedType = statement.ExpectedType;
//         ValueType calculatedType;
//         switch (statement.Value)
//         {
//             case UBExpr.BoolConstant boolConstant:
//                 {
//                     // newVariable will be typed as a bool
//                     calculatedType = new ValueType(BaseType.BoolType, new TypeRefinement(new Variable("v"), []));
//                     break;
//                 }
//             case UBExpr.FunctionCall functionCall:
//                 {
//                     // the inputs are already checked via assertions
//                     // the output is already typed via assumptions
//                     // 
//                 }
//             case UBExpr.IntConstant intConstant:
//                 {
//                     // newVariable will be typed as an int
//                     BVar newVariable = new BVar("intConst", types[UBGenericType.IntType]);
//                     variables[variable] = newVariable;
//                     return new BStmt.Assignment(newVariable, new BExpr.IntConstant(intConstant.Value));
//                 }
//             case UBExpr.VariableRead variableRead:
//                 {
//                     // newVariable will be typed the same as the read variable
//                     BVar readVariable = variables[variableRead.Variable];
//                     BVar newVariable = new BVar("variable", readVariable.Type);
//                     variables[variable] = newVariable;
//                     return new BStmt.Assignment(newVariable, new BExpr.VariableRead(readVariable));
//                 }
//             default:
//                 throw new Exception("Invalid expression");
//         }
//     }
// }