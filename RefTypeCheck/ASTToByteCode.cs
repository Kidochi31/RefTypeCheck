

// internal class ASTToByteCode(Z3.Context context)
// {
//     Z3.Context Context = context;



//     public BVar ConvertExpression(List<BStmt> statements, AExpr expression, Dictionary<AVar, BVar> variables, Dictionary<ABaseType, BType> types, Dictionary<AFunction, BVar> functions)
//     {
//         switch (expression)
//         {
//             case AExpr.VariableRead VariableRead:
//                 {
//                     return variables[VariableRead.Variable];
//                 }
//             case AExpr.FunctionCall FunctionCall:
//                 {
//                     BVar.Function function = (BVar.Function)functions[FunctionCall.Function];
//                     // evaluate all of the BVars in the arguments
//                     List<BVar> arguments = (from arg in FunctionCall.Arguments select ConvertExpression(statements, arg, variables, types, functions)).ToList();
//                     // next, need to assert the types of all of the argument variables
//                     (List<BVar> parameters, List<BStmt> argRefinements) = function.FunctionType.ArgumentRefinements;
//                     argRefinements = CloneAndSubstituteStatements(argRefinements, parameters, arguments);
//                     statements.AddRange(argRefinements);                                  
//                     // now, can calculate the function
//                     BVar resultVar = new BVar("FunctionVar", function.FunctionType.ReturnType);
//                     //statements.Add(new BStmt.Assignment(resultVar, new BExpr.FunctionCall(function, arguments)));

//                     // finally, can assume the type of the resultvar
//                     (BVar dummyReturnVar, parameters, List<BStmt> returnRefinements) = function.FunctionType.ReturnRefinements;
//                     returnRefinements = CloneAndSubstituteStatements(returnRefinements, [dummyReturnVar, ..parameters], [resultVar, ..arguments]);
//                     statements.AddRange(returnRefinements); 
//                     return resultVar;
//                 }
//             case AExpr.BoolConstant BoolConstant:
//                 {
//                     BVar newVar = new BVar("BoolVar", types[ABaseType.Bool]);
//                     statements.Add(new BStmt.Assignment(newVar, new BExpr.BoolConstant(BoolConstant.Value)));
//                     return newVar;
//                 }
//             case AExpr.IntConstant IntConstant:
//                 {
//                     BVar newVar = new BVar("IntVar", types[ABaseType.Int]);
//                     statements.Add(new BStmt.Assignment(newVar, new BExpr.IntConstant(IntConstant.Value)));
//                     return newVar;
//                 }
//             default:
//                 throw new Exception("Invalid AExpr");
//         }
//     }

//     // public BBlock ConvertStatement(BStmt statement, Dictionary<AVar, BVar> variables, Dictionary<ABaseType, BType> types, Dictionary<AFunction, BVar> functions)
//     // {
//     //     switch(statement){
//     //         case AStmt.Assignment Assignment:
//     //             {
//     //                 List<BStmt> statements = new();
//     //                 BVar expression = ConvertExpression(statements, Assignment.Value, variables, types, functions);
//     //                 BVar newVariable = new BVar(Assignment.Variable.Name, Assignment.Type is null ? expression.Type : types[Assignment.Type.BaseType]);
//     //                 variables[Assignment.Variable] = newVariable;
//     //                 // also need to add the type assertions for this new variable
//     //             }
//     //         case AStmt.If If:
//     //         default:
//     //             throw new Exception("Invalid AStmt");
//     //     }
//     // }

//     public List<BStmt> CloneAndSubstituteStatements(List<BStmt> statements, List<BVar> from, List<BVar> to)
//     {
//         List<BStmt> newStatements = new();
//         Dictionary<BVar, BVar> substitutions = new Dictionary<BVar, BVar>(from.Zip(to).Select(x => new KeyValuePair<BVar, BVar>(x.First, x.Second)));
//         foreach(BStmt statement in statements)
//         {
//                 switch (statement)
//             {
//                 case BStmt.Assignment assignment:
//                     newStatements.Add(CloneAndSubstituteAssignment(assignment, substitutions));
//                     break;
//                 case BStmt.Assertion assertion:
//                     newStatements.Add(CloneAndSubstituteAssertion(assertion, substitutions));
//                     break;
//                 case BStmt.Assumption assumption:
//                     newStatements.Add(CloneAndSubstituteAssumption(assumption, substitutions));
//                     break;
//                 default:
//                     throw new Exception("Invalid statement");
//             }
//         }
//         return newStatements;
//     }

//     public BStmt CloneAndSubstituteAssertion (BStmt.Assertion statement, Dictionary<BVar, BVar> substitutions)
//     {
//         if (substitutions.ContainsKey(statement.Variable))
//         {
//             return new BStmt.Assertion(substitutions[statement.Variable]);
//         }
//         return statement;
//     }

//     public BStmt CloneAndSubstituteAssumption (BStmt.Assumption statement, Dictionary<BVar, BVar> substitutions)
//     {
//         if (substitutions.ContainsKey(statement.Variable))
//         {
//             return new BStmt.Assumption(substitutions[statement.Variable]);
//         }
//         return statement;
//     }

//     public BStmt CloneAndSubstituteAssignment(BStmt.Assignment statement, Dictionary<BVar, BVar> substitutions)
//     {
//         // create a clone of the assigned variable
//         BVar variable = statement.Variable;
//         substitutions[variable] = new BVar(variable.Name, variable.Type);
//         BExpr value = statement.Value;
//         switch (value)
//         {
//             case BExpr.BoolConstant boolConstant:
//                 {
//                     // no substitution needed
//                     break;
//                 }
//             case BExpr.FunctionCall functionCall:
//                 {
//                     // substitute the arguments and function
//                     //value = new BExpr.FunctionCall(substitutions.ContainsKey(functionCall.Function) ? substitutions[functionCall.Function] : functionCall.Function, (from arg in functionCall.Arguments select (substitutions.ContainsKey(arg) ? substitutions[functionCall.Function] : functionCall.Function)).ToList());
//                     break;
//                 }
//             case BExpr.IntConstant intConstant:
//                 {
//                     // no substitution needed
//                     break;
//                 }
//             case BExpr.VariableRead variableRead:
//                 {
//                     // substitute the read
//                     if (substitutions.ContainsKey(variableRead.Variable))
//                     {
//                         value = new BExpr.VariableRead(substitutions[variableRead.Variable]);
//                     }
//                     break;
//                 }
//             default:
//                 throw new Exception("Invalid expression");
//         }
//         return new BStmt.Assignment(substitutions[variable], value);
//     }
// }