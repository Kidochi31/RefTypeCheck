

// This is the untyped bytecode

internal abstract record class UBExpr<TVariable>
{
    // Exprs can be:
    // Variable read
    // Function Call
    // Constant (so far, int and bool)
    public record class VariableRead(TVariable Variable) : UBExpr<TVariable>;
    public record class FunctionCall(TVariable Function, List<TVariable> Arguments) : UBExpr<TVariable>;
    public record class IntConstant(int Value) : UBExpr<TVariable>;
    public record class BoolConstant(bool Value) : UBExpr<TVariable>;
    
}

internal abstract record class UBBlock<TVariable>
{
    public record class Basic(List<UBStmt<TVariable>> Body) : UBBlock<TVariable>;
}

internal abstract record class UBStmt<TVariable>
{
    // Stmts can be:
    // Assignments (which in SSA are also variable declarations)
    // Assumption
    // Assertion
    public record class Assignment(TVariable Variable, ValueType? ExpectedType, UBExpr<TVariable> Value) : UBStmt<TVariable>;
    public record class AssignZ3Expr(TVariable Variable, ValueType ExpectedType, Func<Func<TVariable,Z3.Expr>,Z3.Expr> Function) : UBStmt<TVariable>;
}