

// The byte code has no concept of refinement types
// Refinement types are communicated via assumptions and assertions



internal record class BType(string Name, Z3.Sort Z3Sort)
{
    internal record class Function(string Name, Z3.Sort Z3Sort, List<BType> ParameterTypes, BType ReturnType) : BType(Name, Z3Sort)
    {
        public Func<BVar, List<BArg>, Z3.Context, Z3.BoolExpr>? RefinementsOnAssignment = null;
        public (List<BVar>, List<BStmt>) ArgumentRefinements = new();
        public (BVar, List<BVar>, List<BStmt>) ReturnRefinements = new();
    }
}

internal record class BTypeRefinementStatements(BVar TargetVariable, BStmt RefinementStatements);

internal record class BVar(string Name, BType Type)
{
    private Z3.Expr? Z3Variable = null;
    public Z3.Expr GetZ3Variable(Z3.Context Context) => Z3Variable ?? (Z3Variable = Context.MkConst(Name, Type.Z3Sort));
    internal record class Function(string Name, BType.Function FunctionType ) : BVar(Name, FunctionType)
    {
        private Z3.FuncDecl? Z3Function = null;
        public Z3.FuncDecl GetZ3Function(Z3.Context Context)=> Z3Function ?? (Z3Function = Context.MkFuncDecl(Name, (from p in FunctionType.ParameterTypes select p.Z3Sort).ToArray(), FunctionType.ReturnType.Z3Sort));
    }
}

internal abstract record class BArg()
{
    public abstract Z3.Expr GetZ3Variable(Z3.Context Context);
    public record class Variable(BVar Var) : BArg
    {
        public override Z3.Expr GetZ3Variable(Z3.Context Context) => Var.GetZ3Variable(Context);

    }
    public record class IntConstant(int Value) : BArg
    {
        public override Z3.Expr GetZ3Variable(Z3.Context Context) => Context.MkInt(Value);

    }
}



internal abstract record class BExpr
{
    // Exprs can be:
    // Variable read
    // Function Call
    // Constant (so far, int and bool)
    public record class VariableRead(BVar Variable) : BExpr;
    public record class FunctionCall(BVar Function, List<BArg> Arguments) : BExpr;
    public record class IntConstant(int Value) : BExpr;
    public record class BoolConstant(bool Value) : BExpr;
}

internal abstract record class BBlock()
{
    public record class Basic(List<BStmt> Body) : BBlock();
}

internal abstract record class BStmt
{
    // Stmts can be:
    // Assignments (which in SSA are also variable declarations)
    // Assumption
    // Assertion
    public record class Assignment(BVar Variable, BExpr Value) : BStmt;
    public record class Assumption(BVar Variable) : BStmt;
    public record class Assertion(BVar Variable) : BStmt;
}