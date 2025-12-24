global using System;
global using System.Collections.Generic;
global using System.Linq;
global using Z3 = Microsoft.Z3;
global using System.Numerics;

// type the following program:
// let a : {v : int | v >= 0 and v <= 5}
// let b : {v : int | v < 10 and v > a}
// let c : {v : int | v >= 1 and v <= 9} = b - a

Z3.Context context = new Z3.Context();
UVar.Int a = new("a", context);
UVar.Int b = new("b", context);
UVar.Int c = new("c", context);

// refinement for a is it is {v : int | v >= 0 and v <= 5}
UVar.Int a_v = new("a_v", context);
FundamentalBoolRefExpr a_v_ref = new FundamentalBoolRefExpr.And(
    // v >= 0
    new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.GE, new FundamentalIntRefExpr.VariableRead(a_v), new FundamentalIntRefExpr.Integer(0)),
    // v <= 5
    new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.LE, new FundamentalIntRefExpr.VariableRead(a_v), new FundamentalIntRefExpr.Integer(5))
);
UType a_type = new(a_v, [], a_v_ref);

// refinement for b is it is {v : int | v < 10 and v > a}
UVar.Int b_v = new("b_v", context);
FundamentalBoolRefExpr b_v_ref = new FundamentalBoolRefExpr.And(
    // v < 10
    new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.LT, new FundamentalIntRefExpr.VariableRead(b_v), new FundamentalIntRefExpr.Integer(10)),
    // v > a
    new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.GT, new FundamentalIntRefExpr.VariableRead(b_v), new FundamentalIntRefExpr.VariableRead(a))
);
UType b_type = new(b_v, [a], b_v_ref);

// refinement for c is it is {v : int | v >= 1 and v <= 9}
UVar.Int c_v = new("c_v", context);
FundamentalBoolRefExpr c_v_ref = new FundamentalBoolRefExpr.And(
    // v >= 1
    new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.GE, new FundamentalIntRefExpr.VariableRead(c_v), new FundamentalIntRefExpr.Integer(1)),
    // v <= 9
    new FundamentalBoolRefExpr.IntComparison(FundamentalBoolRefExpr.ComparisonOp.LE, new FundamentalIntRefExpr.VariableRead(c_v), new FundamentalIntRefExpr.Integer(9))
);
UType c_type = new(c_v, [], c_v_ref);

// next, c is assigned to b - a
UVar.Int assignment_v = new("assignment_v", context);
FundamentalBoolRefExpr c_assignment = new FundamentalBoolRefExpr.Equal(
    new FundamentalIntRefExpr.VariableRead(assignment_v),
    new FundamentalIntRefExpr.Subtract(new FundamentalIntRefExpr.VariableRead(b), new FundamentalIntRefExpr.VariableRead(a))
);
UType assignment_type = new(assignment_v, [a, b], c_assignment);

// now we can substitute in the types for a, b, and c
Z3.BoolExpr a_ref = a_type.GetSubstitutedZ3Expr(context, a.Z3Variable);
Z3.BoolExpr b_ref = b_type.GetSubstitutedZ3Expr(context, b.Z3Variable);
Z3.BoolExpr assignment_ref = assignment_type.GetSubstitutedZ3Expr(context, c.Z3Variable);

// this must imply c's refinement
Z3.BoolExpr c_ref = c_type.GetSubstitutedZ3Expr(context, c.Z3Variable);

// a -> b is true if (a and not b) is unsatisfiable
Z3.BoolExpr premises = context.MkAnd(a_ref, b_ref, assignment_ref);
Z3.BoolExpr conclusion = c_ref;

Z3.BoolExpr test = context.MkAnd(premises, context.MkNot(conclusion));
Z3.Solver solver = context.MkSolver();
solver.Add(test);
solver.Check();

