

internal record class RefinementAndType(Type Type, Refinement? Refinement);

internal enum Variance
{
    None,
    Covariant,
    Contravariant
}

internal record class Generic(string Name, List<Variance> Parameters, bool FunctionType);

internal record class Type(Generic Base, List<RefinementAndType> Arguments, List<Variable> FunctionVariables);

internal record class Refinement(Variable Value, Variable ConfirmVariable, List<TStmt> TypingStatements);



internal abstract record class TExpr
{
    // Exprs can be:
    // Variable read
    // Function Call
    // Constant (so far, int and bool)
    public record class VariableRead(Variable Variable) : TExpr;
    public record class FunctionCall(Variable Function, List<Variable> Arguments) : TExpr;
    public record class IntConstant(int Value) : TExpr;
    public record class BoolConstant(bool Value) : TExpr;
}

internal abstract record class TBlock()
{
    public record class Basic(List<TStmt> Body) : TBlock();
}

internal abstract record class TStmt
{
    // Stmts can be:
    // Assignments (which in SSA are also variable declarations)
    // Assumption
    // Assertion
    public record class Assignment(RefinementAndType Type, Variable Variable, TExpr Value) : TStmt;
    public record class AssumeType(RefinementAndType Type, Variable Variable) : TStmt;
    public record class AssertType(RefinementAndType Type, Variable Variable) : TStmt;
    public record class Z3Assumption(Z3AssumptionFunction AssumptionFunction, Variable CheckVar, List<(RefinementAndType, Variable)> Arguments) : TStmt;
}