


internal abstract record class AVar(string Name);

internal record class AFunction(string Name, List<(AVar, AType)> Parameters, AType ReturnType)
{
    public Func<BVar, List<BVar>, Z3.Context, Z3.BoolExpr>? RefinementsOnAssignment = null;
}

internal abstract record class AExpr
{
    // Exprs can be:
    // Variable read
    // Function Call
    // Constant (so far, int and bool)
    public record class VariableRead(AVar Variable) : AExpr;
    public record class FunctionCall(AFunction Function, List<AExpr> Arguments) : AExpr;
    public record class IntConstant(int Value) : AExpr;
    public record class BoolConstant(bool Value) : AExpr;
}

internal abstract record class AStmt
{
    // Stmts can be:
    // Assignments
    // If
    public record class Assignment(AVar Variable, AType? Type, AExpr Value) : AStmt;
    public record class If(AExpr Condition, List<AStmt> IfTrue, List<AStmt> IfFalse) : AStmt;
}

internal record class ABaseType(string Name)
{
    public static ABaseType Int = new ABaseType("Int");
    public static ABaseType Bool = new ABaseType("Bool");
}

internal record class AType(ABaseType BaseType, TypeRefinement? Refinement);

internal record class ATypeRefinement(AVar ValueVariable, AExpr Refinement);

