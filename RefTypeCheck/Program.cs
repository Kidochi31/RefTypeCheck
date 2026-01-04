global using System;
global using System.Collections.Generic;
global using System.Linq;
global using Z3 = Microsoft.Z3;
global using System.Numerics;
global using System.Diagnostics;
using System.ComponentModel;

// type the following program:
// let a : {v : int | v >= 0 and v <= 5}
// let b : {v : int | v < 10 and v > a}
// let c : {v : int | v >= 1 and v <= 9} = b - a

// Z3.Context context = new Z3.Context();
// UVar.Int a = new("a", context);
// UVar.Int b = new("b", context);
// UVar.Int c = new("c", context);

// // refinement for a is it is {v : int | v >= 0 and v <= 5}
// UVar.Int a_v = new("a_v", context);
// FundamentalBoolRefExpr a_v_ref = new FundamentalBoolRefExpr.And(
//     // v >= 0
//     new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.GE, new FundamentalIntRefExpr.VariableRead(a_v), new FundamentalIntRefExpr.Integer(0)),
//     // v <= 5
//     new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.LE, new FundamentalIntRefExpr.VariableRead(a_v), new FundamentalIntRefExpr.Integer(5))
// );
// UType a_type = new(a_v, [], context.MkIntSort(), a_v_ref);

// // refinement for b is it is {v : int | v < 10 and v > a}
// UVar.Int b_v = new("b_v", context);
// FundamentalBoolRefExpr b_v_ref = new FundamentalBoolRefExpr.And(
//     // v < 10
//     new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.LT, new FundamentalIntRefExpr.VariableRead(b_v), new FundamentalIntRefExpr.Integer(10)),
//     // v > a
//     new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.GT, new FundamentalIntRefExpr.VariableRead(b_v), new FundamentalIntRefExpr.VariableRead(a))
// );
// UType b_type = new(b_v, [a], context.MkIntSort(), b_v_ref);

// // refinement for c is it is {v : int | v >= 1 and v <= 9}
// UVar.Int c_v = new("c_v", context);
// FundamentalBoolRefExpr c_v_ref = new FundamentalBoolRefExpr.And(
//     // v >= 1
//     new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.GE, new FundamentalIntRefExpr.VariableRead(c_v), new FundamentalIntRefExpr.Integer(2)),
//     // v <= 9
//     new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.LE, new FundamentalIntRefExpr.VariableRead(c_v), new FundamentalIntRefExpr.Integer(9))
// );
// UType c_type = new(c_v, [], context.MkIntSort(), c_v_ref);

// // next, c is assigned to b - a
// UVar.Int assignment_v = new("assignment_v", context);
// FundamentalBoolRefExpr c_assignment = new FundamentalBoolRefExpr.Equal(
//     new FundamentalIntRefExpr.VariableRead(assignment_v),
//     new FundamentalIntRefExpr.Subtract(new FundamentalIntRefExpr.VariableRead(b), new FundamentalIntRefExpr.VariableRead(a))
// );
// UType assignment_type = new(assignment_v, [a, b], context.MkIntSort(), c_assignment);

// // now we can substitute in the types for a, b, and c
// Z3.BoolExpr a_ref = a_type.GetSubstitutedZ3Expr(context, a.Z3Variable);
// Z3.BoolExpr b_ref = b_type.GetSubstitutedZ3Expr(context, b.Z3Variable);
// Z3.BoolExpr assignment_ref = assignment_type.GetSubstitutedZ3Expr(context, c.Z3Variable);

// // this must imply c's refinement
// Z3.BoolExpr c_ref = c_type.GetSubstitutedZ3Expr(context, c.Z3Variable);

// // a -> b is true if (a and not b) is unsatisfiable
// Z3.BoolExpr premises = context.MkAnd(a_ref, b_ref, assignment_ref);
// Z3.BoolExpr conclusion = c_ref;

// Z3ImplicationResult result = Z3Check.CheckImplication(premises, conclusion, context);
// switch (result.Status)
// {
//     case Z3ImplicationStatus.Proven:
//         Console.WriteLine("proven -> the program may compile");
//         break;
//     case Z3ImplicationStatus.Disproven:
//         Console.WriteLine("disproven -> the program cannot compile");
//         Console.WriteLine("The following counterexample is given:");
//         Console.WriteLine(result.Counterexample);
//         break;
//     case Z3ImplicationStatus.Unprovable:
//         Console.WriteLine("Could not prove satisfiability or unsatisfiability -> the program cannot compile");
//         break;

// }


// type the following program:
// let a : {v : int | v >= 0 and v <= 5}
// let b : {v : int | v < 10 and v > a}
// let c : {v : int | v >= 1 and v <= 9} = b - a

// Z3.Context context = new Z3.Context();
// UBaseType IntBaseType = new UBaseType("int", context.MkIntSort());
// UType UnrefinedIntType = new UType(IntBaseType, null, new FundamentalBoolRefExpr.True());

// // let a : {v : int | v >= 0 and v <= 5}
// UType a_v_type = UnrefinedIntType;
// UVar a_v = new UVar("a_v", a_v_type);
// UType a_type = new UType(IntBaseType, a_v,
//         new FundamentalBoolRefExpr.And(
//         //v >= 0
//         new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.GE, new FundamentalIntRefExpr.VariableRead(a_v), new FundamentalIntRefExpr.Integer(0)),
//         // v <= 5
//         new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.LE, new FundamentalIntRefExpr.VariableRead(a_v), new FundamentalIntRefExpr.Integer(5))));
// UVar a = new("a", a_type);

// // let b : {v : int | v < 10 and v > a}
// UType b_v_type = UnrefinedIntType;
// UVar b_v = new UVar("b_v", b_v_type);
// UType b_type = new UType(IntBaseType, b_v,
//         new FundamentalBoolRefExpr.And(
//         //v >= 0
//         new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.LT, new FundamentalIntRefExpr.VariableRead(b_v), new FundamentalIntRefExpr.Integer(10)),
//         // v <= 5
//         new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.GT, new FundamentalIntRefExpr.VariableRead(b_v), new FundamentalIntRefExpr.VariableRead(a))));
// UVar b = new("b", b_type);

// // let c : {v : int | v >= 1 and v <= 9} = b - a
// UType c_v_type = UnrefinedIntType;
// UVar c_v = new UVar("c_v", c_v_type);
// UType c_type = new UType(IntBaseType, c_v,
//         new FundamentalBoolRefExpr.And(
//         //v >= 0
//         new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.GE, new FundamentalIntRefExpr.VariableRead(c_v), new FundamentalIntRefExpr.Integer(1)),
//         // v <= 5
//         new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.LE, new FundamentalIntRefExpr.VariableRead(c_v), new FundamentalIntRefExpr.Integer(9))));
// UVar c = new("c", c_type);

// // minus function
// // minus(x: int, y:int) -> int[v|v=x-y]

// UVar x = new UVar("x", UnrefinedIntType);
// UVar y = new UVar("y", UnrefinedIntType);
// UVar result_v = new UVar("z_v", UnrefinedIntType);
// UType result_type = new UType(IntBaseType, result_v, new FundamentalBoolRefExpr.Equal(new FundamentalIntRefExpr.VariableRead(result_v), new FundamentalIntRefExpr.Subtract(new FundamentalIntRefExpr.VariableRead(x), new FundamentalIntRefExpr.VariableRead(y))));
// UFunction minus = new UFunction("minus", [x, y], result_type, context);

// UStmt statement = new UStmt.Assignment(c, new UExpr.FunctionCall(minus, [b, a]));

// UBlock block = new UBlock.Basic([statement]);

// CheckBC checker = new CheckBC(context);
// Z3.BoolExpr checkContext = context.MkAnd(a_type.GetSubstitutedZ3Expr(context, a.GetZ3Variable(context)), b_type.GetSubstitutedZ3Expr(context, b.GetZ3Variable(context)));
// (bool valid, _, Z3.Model? counterexample) = checker.CheckBlock(block, checkContext);
// Console.WriteLine($"Valid: {valid}\n Counterexample: {counterexample}");



























// // THIS ONE IS WORKING HERE:





// // type the following program:
// // let a : {v : int | v >= 0 and v <= 5}
// // let b : {v : int | v < 10 and v > a}
// // let c : {v : int | v >= 1 and v <= 9} = b - a

// // this is written as:
// // create variable a : int
// // a0 = a >= 0
// // a1 = a <= 5
// // a2 = a0 and a1
// // assume a2

// // create variable b : int
// // b0 = b < 10
// // b1 = b > a
// // b2 = b0 and b1
// // assume b2

// // create variable c : int
// // create function minus : (int, int) -> int
// // c0 = minus(b, a)
// // // note that minus will carry with it the assumptions of c inside
// // c = c0
// // c1 = c>= 1
// // c2 = c <= 9
// // c3 = c1 and c2
// // assert c3


// Z3.Context context = new Z3.Context();
// List<BStmt> statements = new();
// Variable a = new Variable("a");
// Variable b = new Variable("b");
// Variable c = new Variable("c");


// Variable gte = new Variable("gte");
// // [a, b, c] => bool(c) = int(a) >= int(b)
// Z3AssumptionFunction gte_function = (manager, args) => manager.Context.MkEq(manager.GetZ3BoolVariable(args[2]), manager.Context.MkGe(manager.GetZ3IntVariable(args[0]), manager.GetZ3IntVariable(args[1])));

// Variable lte = new Variable("lte");
// // [a, b, c] => bool(c) = int(a) <= int(b)
// Z3AssumptionFunction lte_function = (manager, args) => manager.Context.MkEq(manager.GetZ3BoolVariable(args[2]), manager.Context.MkLe(manager.GetZ3IntVariable(args[0]), manager.GetZ3IntVariable(args[1])));

// Variable gt = new Variable("gt");
// // [a, b, c] => bool(c) = int(a) > int(b)
// Z3AssumptionFunction gt_function = (manager, args) => manager.Context.MkEq(manager.GetZ3BoolVariable(args[2]), manager.Context.MkGt(manager.GetZ3IntVariable(args[0]), manager.GetZ3IntVariable(args[1])));

// Variable lt = new Variable("lt");
// // [a, b, c] => bool(c) = int(a) < int(b)
// Z3AssumptionFunction lt_function = (manager, args) => manager.Context.MkEq(manager.GetZ3BoolVariable(args[2]), manager.Context.MkLt(manager.GetZ3IntVariable(args[0]), manager.GetZ3IntVariable(args[1])));

// Variable and = new Variable("and");
// // [a, b, c] => bool(c) = bool(a) and bool(b)
// Z3AssumptionFunction and_function = (manager, args) => manager.Context.MkEq(manager.GetZ3BoolVariable(args[2]), manager.Context.MkAnd(manager.GetZ3BoolVariable(args[0]), manager.GetZ3BoolVariable(args[1])));

// Variable minus = new Variable("minus");
// // [a, b, c] => int(c) = int(a) - int(b)
// Z3AssumptionFunction minus_function = (manager, args) => manager.Context.MkEq(manager.GetZ3IntVariable(args[2]), manager.Context.MkSub(manager.GetZ3IntVariable(args[0]), manager.GetZ3IntVariable(args[1])));


// // a
// // zero = 0
// // five = 5
// // a0 = a >= zero
// // a1 = a <= five
// // a2 = a0 and a1
// // assume a2

// Variable zero = new Variable("zero");
// statements.Add(new BStmt.Assignment(zero, new BExpr.IntConstant(0))); 
// Variable five = new Variable("five");
// statements.Add(new BStmt.Assignment(five, new BExpr.IntConstant(5))); 

// // a0 = a >= zero
// Variable a0 = new Variable("a0");
// statements.Add(new BStmt.Assignment(a0, new BExpr.FunctionCall(gte, [a, zero], 2)));
// statements.Add(new BStmt.Z3Assumption(gte_function, [a, zero, a0]));

// // a1 = a <= five
// Variable a1 = new Variable("a1");
// statements.Add(new BStmt.Assignment(a1, new BExpr.FunctionCall(lte, [a, five], 2)));
// statements.Add(new BStmt.Z3Assumption(lte_function, [a, five, a1]));

// // a2 = a0 and a1
// Variable a2 = new Variable("a2");
// statements.Add(new BStmt.Assignment(a2, new BExpr.FunctionCall(and, [a0, a1], 2)));
// statements.Add(new BStmt.Z3Assumption(and_function, [a0, a1, a2]));

// // assume a2
// statements.Add(new BStmt.Assumption(a2));

// // b
// // ten = 10
// // b0 = b < ten
// // b1 = b > a
// // b2 = b0 and b1
// // assume b2


// Variable ten = new Variable("ten");
// statements.Add(new BStmt.Assignment(ten, new BExpr.IntConstant(10)));

// // b0 = b < ten
// Variable b0 = new Variable("b0");
// statements.Add(new BStmt.Assignment(b0, new BExpr.FunctionCall(lt, [b, ten], 2)));
// statements.Add(new BStmt.Z3Assumption(lt_function, [b, ten, b0]));

// // b1 = b > a
// Variable b1 = new Variable("b1");
// statements.Add(new BStmt.Assignment(b1, new BExpr.FunctionCall(gt, [b, a], 2)));
// statements.Add(new BStmt.Z3Assumption(gt_function, [b, a, b1]));

// // b2 = b0 and b1
// Variable b2 = new Variable("b2");
// statements.Add(new BStmt.Assignment(b2, new BExpr.FunctionCall(and, [b0, b1], 2)));
// statements.Add(new BStmt.Z3Assumption(and_function, [b0, b1, b2]));

// // assume b2
// statements.Add(new BStmt.Assumption(b2));


// // one = 1
// // nine = 9
// //
// // c0 = minus(b, a)
// // // note that minus will carry with it the assumptions of c inside
// // c = c0
// // c1 = c>= 1
// // c2 = c <= 9
// // c3 = c1 and c2
// // assert c3

// // one = 1
// // nine = 9
// Variable one = new Variable("one");
// statements.Add(new BStmt.Assignment(one, new BExpr.IntConstant(1))); 
// Variable nine = new Variable("nine");
// statements.Add(new BStmt.Assignment(nine, new BExpr.IntConstant(9))); 

// // c0 = minus(b, a)
// Variable c0 = new Variable("c0");
// statements.Add(new BStmt.Assignment(c0, new BExpr.FunctionCall(minus, [b, a], 2)));
// statements.Add(new BStmt.Z3Assumption(minus_function, [b, a, c0]));

// // c = c0
// statements.Add(new BStmt.Assignment(c, new BExpr.VariableRead(c0)));

// // c1 = c>= one
// Variable c1 = new Variable("c1");
// statements.Add(new BStmt.Assignment(c1, new BExpr.FunctionCall(gte, [c, one], 2)));
// statements.Add(new BStmt.Z3Assumption(gte_function, [c, one, c1]));

// // c2 = c <= nine
// Variable c2 = new Variable("c2");
// statements.Add(new BStmt.Assignment(c2, new BExpr.FunctionCall(lte, [c, nine], 2)));
// statements.Add(new BStmt.Z3Assumption(lte_function, [c, nine, c2]));

// // c3 = c1 and c2
// Variable c3 = new Variable("c3");
// statements.Add(new BStmt.Assignment(c3, new BExpr.FunctionCall(and, [c1, c2], 2)));
// statements.Add(new BStmt.Z3Assumption(and_function, [c1, c2, c3]));
// // assert c3
// statements.Add(new BStmt.Assertion(c3));

// BBlock block = new BBlock.Basic(statements);

// CheckByteCode checker = new CheckByteCode(context);
// (bool valid, _, Z3.Model? counterexample) = checker.CheckBlock(block, context.MkTrue());
// Console.WriteLine($"Valid: {valid}");

// if(counterexample is not null)
// {
//     List<Variable> variables = new(){a, b, c};
//     foreach(var var in variables)
//     {
//         Console.WriteLine($"{var.Name}: {counterexample.Evaluate(checker.Management.GetZ3IntVariable(var))}");
//     }
// }























// type the following program:
// let a : {v : int | v >= 0 and v <= 5}
// let b : {v : int | v < 10 and v > a}
// let c : {v : int | v >= 1 and v <= 9} = b - a

// Z3.Context context = new Z3.Context();
// Z3.BoolExpr checkContext = context.MkTrue();
// List<BStmt> statements = new();
// Variable a = new Variable("a");
// Variable b = new Variable("b");
// Variable c = new Variable("c");

// CheckUntypedByteCode checker = new CheckUntypedByteCode(context);


// // need to declare the functions >=, <=, >, <
// BaseGenericType two_arg_function = BaseGenericType.FunctionTypes[2];

// Variable gte_arg_0 = new Variable("gte_arg_0");
// Variable gte_arg_1 = new Variable("gte_arg_1");
// Variable gte_result = new Variable("gte_result");
// Variable gte_confirm = new Variable("gte_confirm");
// ValueType gte_result_type = new ValueType(BaseType.BoolType, new TypeRefinement(gte_result, gte_confirm, [new UBStmt<Variable>.AssignZ3Expr(gte_confirm, ValueType.UnrefinedBoolType, (f) => context.MkEq(f(gte_result), context.MkGe((Z3.ArithExpr)f(gte_arg_0), (Z3.ArithExpr)f(gte_arg_1))))]));
// ValueType gte_type = new ValueType(new BaseType(two_arg_function, [ValueType.UnrefinedIntType, ValueType.UnrefinedIntType, ValueType.UnrefinedBoolType], new InvokeType([gte_arg_0, gte_arg_1], [ValueType.UnrefinedIntType, ValueType.UnrefinedIntType], gte_result_type)), new TypeRefinement(new Variable("a"), null, []));
// Variable gte = new Variable("gte");
// (TypedVariable typed_gte, bool valid, checkContext, var model) =checker.DeclareAssumedVariable(gte, gte_type, checkContext);

// Variable lte_arg_0 = new Variable("lte_arg_0");
// Variable lte_arg_1 = new Variable("lte_arg_1");
// Variable lte_result = new Variable("lte_result");
// Variable lte_confirm = new Variable("lte_confirm");
// ValueType lte_result_type = new ValueType(BaseType.BoolType, new TypeRefinement(lte_result, lte_confirm, [new UBStmt<Variable>.AssignZ3Expr(lte_confirm, ValueType.UnrefinedBoolType, (f) => context.MkEq(f(lte_result), context.MkLe((Z3.ArithExpr)f(lte_arg_0), (Z3.ArithExpr)f(lte_arg_1))))]));
// ValueType lte_type = new ValueType(new BaseType(two_arg_function, [ValueType.UnrefinedIntType, ValueType.UnrefinedIntType, ValueType.UnrefinedBoolType], new InvokeType([lte_arg_0, lte_arg_1], [ValueType.UnrefinedIntType, ValueType.UnrefinedIntType], lte_result_type)), new TypeRefinement(new Variable("a"), null, []));
// Variable lte = new Variable("lte");
// (TypedVariable typed_lte, valid, checkContext, model) =checker.DeclareAssumedVariable(lte, lte_type, checkContext);

// Variable lt_arg_0 = new Variable("lt_arg_0");
// Variable lt_arg_1 = new Variable("lt_arg_1");
// Variable lt_result = new Variable("lt_result");
// Variable lt_confirm = new Variable("lt_confirm");
// ValueType lt_result_type = new ValueType(BaseType.BoolType, new TypeRefinement(lt_result, lt_confirm, [new UBStmt<Variable>.AssignZ3Expr(lt_confirm, ValueType.UnrefinedBoolType, (f) => context.MkEq(f(lt_result), context.MkLt((Z3.ArithExpr)f(lt_arg_0), (Z3.ArithExpr)f(lt_arg_1))))]));
// ValueType lt_type = new ValueType(new BaseType(two_arg_function, [ValueType.UnrefinedIntType, ValueType.UnrefinedIntType, ValueType.UnrefinedBoolType], new InvokeType([lt_arg_0, lt_arg_1], [ValueType.UnrefinedIntType, ValueType.UnrefinedIntType], lt_result_type)), new TypeRefinement(new Variable("a"), null, []));
// Variable lt = new Variable("lt");
// (TypedVariable typed_lt, valid, checkContext, model) =checker.DeclareAssumedVariable(lt, lt_type, checkContext);

// Variable gt_arg_0 = new Variable("gt_arg_0");
// Variable gt_arg_1 = new Variable("gt_arg_1");
// Variable gt_result = new Variable("gt_result");
// Variable gt_confirm = new Variable("gt_confirm");
// ValueType gt_result_type = new ValueType(BaseType.BoolType, new TypeRefinement(gt_result, gt_confirm, [new UBStmt<Variable>.AssignZ3Expr(gt_confirm, ValueType.UnrefinedBoolType, (f) => context.MkEq(f(gt_result), context.MkGt((Z3.ArithExpr)f(gt_arg_0), (Z3.ArithExpr)f(gt_arg_1))))]));
// ValueType gt_type = new ValueType(new BaseType(two_arg_function, [ValueType.UnrefinedIntType, ValueType.UnrefinedIntType, ValueType.UnrefinedBoolType], new InvokeType([gt_arg_0, gt_arg_1], [ValueType.UnrefinedIntType, ValueType.UnrefinedIntType], gt_result_type)), new TypeRefinement(new Variable("a"), null, []));
// Variable gt = new Variable("gt");
// (TypedVariable typed_gt, valid, checkContext, model) =checker.DeclareAssumedVariable(gt, gt_type, checkContext);

// Variable and_arg_0 = new Variable("and_arg_0");
// Variable and_arg_1 = new Variable("and_arg_1");
// Variable and_result = new Variable("and_result");
// Variable and_confirm = new Variable("and_confirm");
// ValueType and_result_type = new ValueType(BaseType.BoolType, new TypeRefinement(and_result, and_confirm, [new UBStmt<Variable>.AssignZ3Expr(and_confirm, ValueType.UnrefinedBoolType, (f) => context.MkEq(f(and_result), context.MkAnd((Z3.BoolExpr)f(and_arg_0), (Z3.BoolExpr)f(and_arg_1))))]));
// ValueType and_type = new ValueType(new BaseType(two_arg_function, [ValueType.UnrefinedBoolType, ValueType.UnrefinedBoolType, ValueType.UnrefinedBoolType], new InvokeType([and_arg_0, and_arg_1], [ValueType.UnrefinedBoolType, ValueType.UnrefinedBoolType], and_result_type)), new TypeRefinement(new Variable("a"), null, []));
// Variable and = new Variable("and");
// (TypedVariable typed_and, valid, checkContext, model) =checker.DeclareAssumedVariable(and, and_type, checkContext);

// Variable minus_arg_0 = new Variable("minus_arg_0");
// Variable minus_arg_1 = new Variable("minus_arg_1");
// Variable minus_result = new Variable("minus_result");
// Variable minus_confirm = new Variable("minus_confirm");
// ValueType minus_result_type = new ValueType(BaseType.IntType, new TypeRefinement(minus_result, minus_confirm, [new UBStmt<Variable>.AssignZ3Expr(minus_confirm, ValueType.UnrefinedBoolType, (f) => context.MkEq(f(minus_result), context.MkSub((Z3.ArithExpr)f(minus_arg_0), (Z3.ArithExpr)f(minus_arg_1))))]));
// ValueType minus_type = new ValueType(new BaseType(two_arg_function, [ValueType.UnrefinedIntType, ValueType.UnrefinedIntType, ValueType.UnrefinedIntType], new InvokeType([minus_arg_0, minus_arg_1], [ValueType.UnrefinedIntType, ValueType.UnrefinedIntType], minus_result_type)), new TypeRefinement(new Variable("a"), null, []));
// Variable minus = new Variable("minus");
// (TypedVariable typed_minus, valid, checkContext, model) =checker.DeclareAssumedVariable(minus, minus_type, checkContext);

// // need to assume the types of a and b
// // a : {v : int | v >= 0 and v <= 5}
// // zero = 0
// // five = 5
// // a0 = a_v >= zero
// // a1 = a_v <= five
// // a2 = a0 and a1
// // check a2
// Variable a_v = new Variable("a_v");
// Variable zero = new Variable("zero");
// Variable five = new Variable("five");
// Variable a0 = new("a0");
// Variable a1 = new("a1");
// Variable a2 = new("a2");
// (TypedVariable typed_a, valid, checkContext, model) = checker.DeclareAssumedVariable(a, new ValueType(BaseType.IntType, new TypeRefinement(a_v, a2, [
//     new UBStmt<Variable>.Assignment(zero, null, new UBExpr<Variable>.IntConstant(0)),
//     new UBStmt<Variable>.Assignment(five, null, new UBExpr<Variable>.IntConstant(5)),
//     new UBStmt<Variable>.Assignment(a0, null, new UBExpr<Variable>.FunctionCall(gte, [a_v, zero])),
//     new UBStmt<Variable>.Assignment(a1, null, new UBExpr<Variable>.FunctionCall(lte, [a_v, five])),
//     new UBStmt<Variable>.Assignment(a2, null, new UBExpr<Variable>.FunctionCall(and, [a0, a1])),
// ])), checkContext);


// // need to assume the types of a and b
// // let b : {v : int | v < 10 and v > a}
// // ten = 10
// // b0 = b_v < 10
// // b1 = b_v > a
// // b2 = b0 and b1
// // check b2
// Variable b_v = new Variable("b_v");
// Variable ten = new Variable("zero");
// Variable b0 = new("b0");
// Variable b1 = new("b1");
// Variable b2 = new("b2");
// (TypedVariable typed_b, valid, checkContext, model) = checker.DeclareAssumedVariable(b, new ValueType(BaseType.IntType, new TypeRefinement(b_v, b2, [
//     new UBStmt<Variable>.Assignment(ten, null, new UBExpr<Variable>.IntConstant(10)),
//     new UBStmt<Variable>.Assignment(b0, null, new UBExpr<Variable>.FunctionCall(lt, [b_v, ten])),
//     new UBStmt<Variable>.Assignment(b1, null, new UBExpr<Variable>.FunctionCall(gt, [b_v, a])),
//     new UBStmt<Variable>.Assignment(b2, null, new UBExpr<Variable>.FunctionCall(and, [b0, b1])),
// ])), checkContext);

// // now need to check the type of c
// // let c : {v : int | v >= 1 and v <= 9} = b - a
// // one = 1
// // nine = 9
// // c0 = c_v >= 1
// // c1 = c <= 9
// // c2 = c0 and c1
// // check c2
// Variable c_v = new Variable("c_v");
// Variable one = new("one");
// Variable nine = new("nine");
// Variable c0 = new("c0");
// Variable c1 = new("c1");
// Variable c2 = new("c2");
// var c_type = new ValueType(BaseType.IntType, new TypeRefinement(c_v, c2, [
//     new UBStmt<Variable>.Assignment(one, null, new UBExpr<Variable>.IntConstant(1)),
//     new UBStmt<Variable>.Assignment(nine, null, new UBExpr<Variable>.IntConstant(9)),
//     new UBStmt<Variable>.Assignment(c0, null, new UBExpr<Variable>.FunctionCall(lt, [c_v, one])),
//     new UBStmt<Variable>.Assignment(c1, null, new UBExpr<Variable>.FunctionCall(gt, [c_v, nine])),
//     new UBStmt<Variable>.Assignment(c2, null, new UBExpr<Variable>.FunctionCall(and, [c0, c1])),
// ]));

// // Now create the assignment statement
// UBStmt<Variable> assignmentStatement = new UBStmt<Variable>.Assignment(c, c_type, new UBExpr<Variable>.FunctionCall(minus, [b, a]));
// UBBlock<Variable>.Basic block = new([assignmentStatement]);

// (var typedBlock, checkContext, model) =checker.TypeBlock(block, checkContext);
// Console.WriteLine($"Valid: {typedBlock is not null}\n Counterexample: {model}");























// NEW ONE, TRYING

// type the following program:
// let a : {v : int | v >= 0 and v <= 5}
// let b : {v : int | v < 10 and v > a}
// let c : {v : int | v >= 1 and v <= 9} = b - a

// this is written as:
// create variable a : int
// a0 = a >= 0
// a1 = a <= 5
// a2 = a0 and a1
// assume a2

// create variable b : int
// b0 = b < 10
// b1 = b > a
// b2 = b0 and b1
// assume b2

// create variable c : int
// create function minus : (int, int) -> int
// c0 = minus(b, a)
// // note that minus will carry with it the assumptions of c inside
// c = c0
// c1 = c>= 1
// c2 = c <= 9
// c3 = c1 and c2
// assert c3


Z3.Context context = new Z3.Context();
List<TStmt> statements = new();
Generic intGeneric = new Generic("int", [], false);
Type intType = new Type(intGeneric, [], []);
RefinementAndType unrefinedInt = new RefinementAndType(intType, null);
Generic boolGeneric = new Generic("bool", [], false);
Type boolType = new Type(boolGeneric, [], []);
RefinementAndType unrefinedBool = new RefinementAndType(boolType, null);
Generic func3Generic = new Generic("func3", [Variance.Contravariant, Variance.Contravariant, Variance.Covariant], false);


Variable gte = new Variable("gte");
// [a, b, c] => bool(c) = int(a) >= int(b)
Variable gte_a = new("gte_a");
Variable gte_b = new("gte_b");
Variable gte_c = new("gte_c");
Variable gte_return_v = new("gte_return_v");
Variable gte_check_v = new("gte_check_v");
Z3AssumptionFunction gte_function = new("gte", (manager, args) => manager.Context.MkEq(manager.GetZ3BoolVariable(args[2]), manager.Context.MkGe(manager.GetZ3IntVariable(args[0]), manager.GetZ3IntVariable(args[1]))));
RefinementAndType gte_return_type = new RefinementAndType(boolType, new Refinement(gte_return_v, gte_check_v, [new TStmt.Z3Assumption(gte_function, gte_check_v, [(unrefinedInt, gte_a), (unrefinedInt, gte_b), (unrefinedBool, gte_c)])]));
Type gte_type = new Type(func3Generic, [unrefinedInt, unrefinedInt, gte_return_type], [gte_a, gte_b, gte_c]);
statements.Add(new TStmt.AssumeType(new RefinementAndType(gte_type, null), gte));

// Variable lte = new Variable("lte");
// // [a, b, c] => bool(c) = int(a) <= int(b)
// Z3AssumptionFunction lte_function = (manager, args) => manager.Context.MkEq(manager.GetZ3BoolVariable(args[2]), manager.Context.MkLe(manager.GetZ3IntVariable(args[0]), manager.GetZ3IntVariable(args[1])));

// Variable gt = new Variable("gt");
// // [a, b, c] => bool(c) = int(a) > int(b)
// Z3AssumptionFunction gt_function = (manager, args) => manager.Context.MkEq(manager.GetZ3BoolVariable(args[2]), manager.Context.MkGt(manager.GetZ3IntVariable(args[0]), manager.GetZ3IntVariable(args[1])));

// Variable lt = new Variable("lt");
// // [a, b, c] => bool(c) = int(a) < int(b)
// Z3AssumptionFunction lt_function = (manager, args) => manager.Context.MkEq(manager.GetZ3BoolVariable(args[2]), manager.Context.MkLt(manager.GetZ3IntVariable(args[0]), manager.GetZ3IntVariable(args[1])));

// Variable and = new Variable("and");
// // [a, b, c] => bool(c) = bool(a) and bool(b)
// Z3AssumptionFunction and_function = (manager, args) => manager.Context.MkEq(manager.GetZ3BoolVariable(args[2]), manager.Context.MkAnd(manager.GetZ3BoolVariable(args[0]), manager.GetZ3BoolVariable(args[1])));

// Variable minus = new Variable("minus");
// // [a, b, c] => int(c) = int(a) - int(b)
// Z3AssumptionFunction minus_function = (manager, args) => manager.Context.MkEq(manager.GetZ3IntVariable(args[2]), manager.Context.MkSub(manager.GetZ3IntVariable(args[0]), manager.GetZ3IntVariable(args[1])));


// create variable a : int
// type variable a_v
// a0 = a_v >= 0
// a1 = a_v <= 5
// a2 = a0 and a1
// confirm a2

Variable a = new Variable("a");

Variable a_v = new("a_v");
Variable zero = new("zero");
Variable a_0 = new("a_0");

RefinementAndType a_type = new RefinementAndType(intType, new Refinement(a_v, a_0, [
    new TStmt.Assignment(unrefinedInt, zero, new TExpr.IntConstant(0)),
    new TStmt.Assignment(unrefinedBool, a_0, new TExpr.FunctionCall(gte, [a_v, zero]))
]));

statements.Add(new TStmt.AssumeType(a_type, a));

// assert a >= -1

Variable a_v2 = new("a_v2");
Variable neg_one = new("neg_one");
Variable a_02 = new("a_02");

RefinementAndType a_type2 = new RefinementAndType(intType, new Refinement(a_v2, a_02, [
    new TStmt.Assignment(unrefinedInt, neg_one, new TExpr.IntConstant(-1)),
    new TStmt.Assignment(unrefinedBool, a_02, new TExpr.FunctionCall(gte, [a_v2, neg_one]))
]));

statements.Add(new TStmt.AssertType(a_type2, a));


Variable b = new Variable("b");
Variable c = new Variable("c");

TBlock typedBlock = new TBlock.Basic(statements);

CheckTypedByteCode typedChecker = new CheckTypedByteCode(intType, boolType, [func3Generic]);

(bool typeValid, BBlock? block) = typedChecker.CheckBlock(typedBlock);

if (!typeValid || block is null)
{
    Console.WriteLine("NOT VALID!");
    return;
}

Console.WriteLine($"bytecode: \n{block}");

CheckByteCode checker = new CheckByteCode(context);
(bool valid, Z3.BoolExpr boolExpr, Z3.Model? counterexample) = checker.CheckBlock(block, context.MkTrue());
Console.WriteLine($"Valid: {valid}, Context: {boolExpr}");

if(counterexample is not null)
{
    List<Variable> variables = new(){a, b, c};
    foreach(var var in variables)
    {
        Console.WriteLine($"{var.Name}: {counterexample.Evaluate(checker.Management.GetZ3IntVariable(var))}");
    }
}








// a
// zero = 0
// five = 5
// a0 = a >= zero
// a1 = a <= five
// a2 = a0 and a1
// assume a2

// Variable zero = new Variable("zero");
// statements.Add(new BStmt.Assignment(zero, new BExpr.IntConstant(0))); 
// Variable five = new Variable("five");
// statements.Add(new BStmt.Assignment(five, new BExpr.IntConstant(5))); 

// // a0 = a >= zero
// Variable a0 = new Variable("a0");
// statements.Add(new BStmt.Assignment(a0, new BExpr.FunctionCall(gte, [a, zero], 2)));
// statements.Add(new BStmt.Z3Assumption(gte_function, [a, zero, a0]));

// // a1 = a <= five
// Variable a1 = new Variable("a1");
// statements.Add(new BStmt.Assignment(a1, new BExpr.FunctionCall(lte, [a, five], 2)));
// statements.Add(new BStmt.Z3Assumption(lte_function, [a, five, a1]));

// // a2 = a0 and a1
// Variable a2 = new Variable("a2");
// statements.Add(new BStmt.Assignment(a2, new BExpr.FunctionCall(and, [a0, a1], 2)));
// statements.Add(new BStmt.Z3Assumption(and_function, [a0, a1, a2]));

// // assume a2
// statements.Add(new BStmt.Assumption(a2));

// b
// ten = 10
// b0 = b < ten
// b1 = b > a
// b2 = b0 and b1
// assume b2


// Variable ten = new Variable("ten");
// statements.Add(new BStmt.Assignment(ten, new BExpr.IntConstant(10)));

// // b0 = b < ten
// Variable b0 = new Variable("b0");
// statements.Add(new BStmt.Assignment(b0, new BExpr.FunctionCall(lt, [b, ten], 2)));
// statements.Add(new BStmt.Z3Assumption(lt_function, [b, ten, b0]));

// // b1 = b > a
// Variable b1 = new Variable("b1");
// statements.Add(new BStmt.Assignment(b1, new BExpr.FunctionCall(gt, [b, a], 2)));
// statements.Add(new BStmt.Z3Assumption(gt_function, [b, a, b1]));

// // b2 = b0 and b1
// Variable b2 = new Variable("b2");
// statements.Add(new BStmt.Assignment(b2, new BExpr.FunctionCall(and, [b0, b1], 2)));
// statements.Add(new BStmt.Z3Assumption(and_function, [b0, b1, b2]));

// // assume b2
// statements.Add(new BStmt.Assumption(b2));


// one = 1
// nine = 9
//
// c0 = minus(b, a)
// // note that minus will carry with it the assumptions of c inside
// c = c0
// c1 = c>= 1
// c2 = c <= 9
// c3 = c1 and c2
// assert c3

// one = 1
// nine = 9
// Variable one = new Variable("one");
// statements.Add(new BStmt.Assignment(one, new BExpr.IntConstant(1))); 
// Variable nine = new Variable("nine");
// statements.Add(new BStmt.Assignment(nine, new BExpr.IntConstant(9))); 

// // c0 = minus(b, a)
// Variable c0 = new Variable("c0");
// statements.Add(new BStmt.Assignment(c0, new BExpr.FunctionCall(minus, [b, a], 2)));
// statements.Add(new BStmt.Z3Assumption(minus_function, [b, a, c0]));

// // c = c0
// statements.Add(new BStmt.Assignment(c, new BExpr.VariableRead(c0)));

// // c1 = c>= one
// Variable c1 = new Variable("c1");
// statements.Add(new BStmt.Assignment(c1, new BExpr.FunctionCall(gte, [c, one], 2)));
// statements.Add(new BStmt.Z3Assumption(gte_function, [c, one, c1]));

// // c2 = c <= nine
// Variable c2 = new Variable("c2");
// statements.Add(new BStmt.Assignment(c2, new BExpr.FunctionCall(lte, [c, nine], 2)));
// statements.Add(new BStmt.Z3Assumption(lte_function, [c, nine, c2]));

// // c3 = c1 and c2
// Variable c3 = new Variable("c3");
// statements.Add(new BStmt.Assignment(c3, new BExpr.FunctionCall(and, [c1, c2], 2)));
// statements.Add(new BStmt.Z3Assumption(and_function, [c1, c2, c3]));
// // assert c3
// statements.Add(new BStmt.Assertion(c3));

// BBlock block = new BBlock.Basic(statements);

// CheckByteCode checker = new CheckByteCode(context);
// (bool valid, _, Z3.Model? counterexample) = checker.CheckBlock(block, context.MkTrue());
// Console.WriteLine($"Valid: {valid}");

// if(counterexample is not null)
// {
//     List<Variable> variables = new(){a, b, c};
//     foreach(var var in variables)
//     {
//         Console.WriteLine($"{var.Name}: {counterexample.Evaluate(checker.Management.GetZ3IntVariable(var))}");
//     }
// }