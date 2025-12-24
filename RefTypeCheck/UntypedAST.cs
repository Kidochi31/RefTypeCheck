



internal abstract record class UVar(string Name, Z3.Context Context)
{
    public abstract Z3.Expr Z3Variable {get;}

    public record class Int(string Name, Z3.Context Context) : UVar(Name, Context)
    {
        public override Z3.IntExpr Z3Variable {get;} = (Z3.IntExpr)Context.MkConst(Name, Context.MkIntSort());
    }

    public record class Bool(string Name, Z3.Context Context) : UVar(Name, Context)
    {
        public override Z3.BoolExpr Z3Variable {get;} = (Z3.BoolExpr)Context.MkConst(Name, Context.MkBoolSort());
    }

    public record class Double(string Name, Z3.Context Context) : UVar(Name, Context)
    {
        public override Z3.FPExpr Z3Variable {get;} = (Z3.FPExpr)Context.MkConst(Name, Context.MkFPSort64());
    }

    // public record class Float32(string Name, Z3.Context Context) : UVar(Name, Context)
    // {
    //     public override Z3.FPExpr Z3Variable {get;} = (Z3.FPExpr)Context.MkConst(Name, Context.MkFPSort32());
    // }
}

internal abstract record class UExpr
{
    // Exprs can be:
    // Variable read
    // Phi node
    // Function Call
    // Constant (so far, int and bool)
    public record class VariableRead(UVar Variable) : UExpr;

    public record class PhiNode(List<(UBlock, UVar)> Selections) : UExpr;

    public record class FunctionCall(UExpr Function, List<UExpr> Arguments) : UExpr;

    public record class IntConstant(int Value) : UExpr;
    public record class BoolConstant(bool Value) : UExpr;
}

internal record class UBlock(List<UStmt> Body, UTerminator Terminator)
{
    public List<UBlock?> EntryFrom = new(); // null = start of function
    public List<UBlock?> ExitTo = new(); // null = end of function
}

internal abstract record class UStmt
{
    // Stmts can be:
    // Assignments (which in SSA are also variable declarations)
    public record class Assignment(UVar Variable, UExpr Value) : UStmt;
}

internal abstract record class UTerminator
{
    // Terminators can be:
    // If statements
    // Return
    public record class If(UExpr Condition, UBlock IfTrue, UBlock IfFalse) : UTerminator;
    public record class Return(UExpr? Value) : UTerminator;
}

internal abstract record class UFunction(string Name, List<(UVar, UType)> Parameters, UType ReturnType);

// A type can be written as some z3 formula which has parameter variables (the value and possible other values) that returns a z3 boolean expression
// E.g. x = 3; x is of the type (IntSort I) -> I = 3
// This can be applied to x by replacing I with the variable fo x. This creates the global refinement x = 3.
internal record class UType(UVar ValueVariable, List<UVar> VariableParameters, FundamentalBoolRefExpr Refinement)
{
    public Z3.BoolExpr GetSubstitutedZ3Expr(Z3.Context Context, Z3.Expr VariableExpression)
    {
        return (Z3.BoolExpr)Refinement.GetZ3Expression(Context).Substitute(ValueVariable.Z3Variable, VariableExpression);
    }
}




// Fundamental refinements are directly convertable into z3 expressions (with substitution)
internal abstract record class FundamentalRefExpr(List<FundamentalRefExpr> Components)
{
    public virtual bool IsConstant() => Components.All(refinement => refinement.IsConstant());
    public abstract Z3.Expr GetZ3Expression(Z3.Context Context);
}

internal abstract record class FundamentalBoolRefExpr(List<FundamentalRefExpr> Components) : FundamentalRefExpr(Components)
{
    public enum ComparisonOp
    {
        LT,
        LE,
        GT,
        GE
    }

    public override abstract Z3.BoolExpr GetZ3Expression(Z3.Context Context);

    // boolean refinements include:
    // a == b
    // a != b
    // a >/>=/</<= b, where a and b are arithmetic
    // a and b, where a and b are boolean
    // a or b, where a and b are boolean
    // a xor b, where a and b are boolean
    // not a, where a is boolean
    // a implies b, where a and b are boolean
    // a iff b, where a and b are boolean
    // true
    // false
    // variable read
    public record class Equal(FundamentalRefExpr A, FundamentalRefExpr B) : FundamentalBoolRefExpr([A, B])
    {
        public override Z3.BoolExpr GetZ3Expression(Z3.Context Context) => Context.MkEq(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
    }
    public record class NotEqual(FundamentalRefExpr A, FundamentalRefExpr B) : FundamentalBoolRefExpr([A, B])
    {
        public override Z3.BoolExpr GetZ3Expression(Z3.Context Context) => Context.MkDistinct(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
    }
    public record class IntComparison(ComparisonOp Op, FundamentalIntRefExpr A, FundamentalIntRefExpr B) : FundamentalBoolRefExpr([A, B])
    {
        public override Z3.BoolExpr GetZ3Expression(Z3.Context Context) {
            switch (Op)
            {
                case ComparisonOp.LT:
                    return Context.MkLt(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
                case ComparisonOp.LE:
                    return Context.MkLe(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
                case ComparisonOp.GT:
                    return Context.MkGt(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
                case ComparisonOp.GE:
                    return Context.MkGe(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
            }
            throw new Exception($"Invalid Comparison op {Op} for Fundmental Bool Ref Expr (Should not get here)!");
        }
    }
    public record class FloatComparison(ComparisonOp Op, FundamentalFloatRefExpr A, FundamentalFloatRefExpr B) : FundamentalBoolRefExpr([A, B])
    {
        public override Z3.BoolExpr GetZ3Expression(Z3.Context Context) {
            switch (Op)
            {
                case ComparisonOp.LT:
                    return Context.MkFPLt(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
                case ComparisonOp.LE:
                    return Context.MkFPLEq(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
                case ComparisonOp.GT:
                    return Context.MkFPGt(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
                case ComparisonOp.GE:
                    return Context.MkFPGEq(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
            }
            throw new Exception($"Invalid Comparison op {Op} for Fundmental Bool Ref Expr (Should not get here)!");
        }
    }
    public record class And(FundamentalBoolRefExpr A, FundamentalBoolRefExpr B) : FundamentalBoolRefExpr([A, B])
    {
        public override Z3.BoolExpr GetZ3Expression(Z3.Context Context) => Context.MkAnd(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
    }
    public record class Or(FundamentalBoolRefExpr A, FundamentalBoolRefExpr B) : FundamentalBoolRefExpr([A, B])
    {
        public override Z3.BoolExpr GetZ3Expression(Z3.Context Context) => Context.MkOr(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
    }
    public record class Xor(FundamentalBoolRefExpr A, FundamentalBoolRefExpr B) : FundamentalBoolRefExpr([A, B])
    {
        public override Z3.BoolExpr GetZ3Expression(Z3.Context Context) => Context.MkXor(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
    }
    public record class Not(FundamentalBoolRefExpr A) : FundamentalBoolRefExpr([A])
    {
        public override Z3.BoolExpr GetZ3Expression(Z3.Context Context) => Context.MkNot(A.GetZ3Expression(Context));
    }
    public record class Implies(FundamentalBoolRefExpr A, FundamentalBoolRefExpr B) : FundamentalBoolRefExpr([A, B])
    {
        public override Z3.BoolExpr GetZ3Expression(Z3.Context Context) => Context.MkImplies(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
    }
    public record class Iff(FundamentalBoolRefExpr A, FundamentalBoolRefExpr B) : FundamentalBoolRefExpr([A, B])
    {
        public override Z3.BoolExpr GetZ3Expression(Z3.Context Context) => Context.MkIff(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
    }

    public record class True(): FundamentalBoolRefExpr([])
    {
        public override Z3.BoolExpr GetZ3Expression(Z3.Context Context) => Context.MkTrue();
    }

    public record class False() : FundamentalBoolRefExpr([])
    {
        public override Z3.BoolExpr GetZ3Expression(Z3.Context Context) => Context.MkFalse();
    }

    public record class VariableRead(UVar.Bool Variable) : FundamentalBoolRefExpr([])
    {
        public override bool IsConstant() => false;
        public override Z3.BoolExpr GetZ3Expression(Z3.Context Context) => Variable.Z3Variable;
    }
}

internal abstract record class FundamentalIntRefExpr(List<FundamentalRefExpr> Components) : FundamentalRefExpr(Components)
{
    public override abstract Z3.IntExpr GetZ3Expression(Z3.Context Context);

    // arithmetic refinements include:
    // a + b, where a and b are arithmetic
    // a - b, where a and b are arithmetic
    // -a, where a is arithmetic
    // a * b, where a and b are arithmetic, and a is constant
    // a / b, where a and b are arithmetic, and b is a constant
    // if a then b else c, where a is boolean, and b and c are arithmetic
    // integer constant
    public record class Add(FundamentalIntRefExpr A, FundamentalIntRefExpr B) : FundamentalIntRefExpr([A, B])
    {
        public override Z3.IntExpr GetZ3Expression(Z3.Context Context) => (Z3.IntExpr)Context.MkAdd(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
    }
    public record class Subtract(FundamentalIntRefExpr A, FundamentalIntRefExpr B) : FundamentalIntRefExpr([A, B])
    {
        public override Z3.IntExpr GetZ3Expression(Z3.Context Context) => (Z3.IntExpr)Context.MkSub(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
    }
    public record class Negate(FundamentalIntRefExpr A) : FundamentalIntRefExpr([A])
    {
        public override Z3.IntExpr GetZ3Expression(Z3.Context Context) => (Z3.IntExpr)Context.MkUnaryMinus(A.GetZ3Expression(Context));
    }
    public record class Multiply : FundamentalIntRefExpr
    {
        public FundamentalIntRefExpr A;
        public FundamentalIntRefExpr B;

        public Multiply(FundamentalIntRefExpr A, FundamentalIntRefExpr B) : base([A, B])
        {
            // a must be constant
            if (!A.IsConstant())
            {
                throw new Exception("a must be constant in fundamental multiplication expression a * b!");
            }
            this.A = A;
            this.B = B;
        }

        public override Z3.IntExpr GetZ3Expression(Z3.Context Context) => (Z3.IntExpr)Context.MkMul(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
    }
    public record class Divide : FundamentalIntRefExpr
    {
        public FundamentalIntRefExpr A;
        public FundamentalIntRefExpr B;

        public Divide(FundamentalIntRefExpr A, FundamentalIntRefExpr B) : base([A, B])
        {
            // b must be constant
            if (!B.IsConstant())
            {
                throw new Exception("b must be constant in fundamental multiplication expression a / b!");
            }
            this.A = A;
            this.B = B;
        }

        public override Z3.IntExpr GetZ3Expression(Z3.Context Context) => (Z3.IntExpr)Context.MkDiv(A.GetZ3Expression(Context), B.GetZ3Expression(Context));
    }
    public record class If(FundamentalBoolRefExpr A, FundamentalIntRefExpr B, FundamentalIntRefExpr C) : FundamentalIntRefExpr([A, B, C])
    {
        public override Z3.IntExpr GetZ3Expression(Z3.Context Context) => (Z3.IntExpr)Context.MkITE(A.GetZ3Expression(Context), B.GetZ3Expression(Context), C.GetZ3Expression(Context));
    }

    public record class Integer(BigInteger Value) : FundamentalIntRefExpr([])
    {
        public override Z3.IntExpr GetZ3Expression(Z3.Context Context) => Context.MkInt(Value.ToString());
    }

    public record class FloatToArithCast(FundamentalFloatRefExpr A): FundamentalIntRefExpr([A])
    {
        public override Z3.IntExpr GetZ3Expression(Z3.Context Context) => Context.MkReal2Int(Context.MkFPToReal(A.GetZ3Expression(Context)));
    }

    public record class VariableRead(UVar.Int Variable) : FundamentalIntRefExpr([])
    {
        public override bool IsConstant() => false;
        public override Z3.IntExpr GetZ3Expression(Z3.Context Context) => Variable.Z3Variable;
    }
    
}

internal abstract record class FundamentalFloatRefExpr(List<FundamentalRefExpr> Components) : FundamentalRefExpr(Components)
{
    public override abstract Z3.FPExpr GetZ3Expression(Z3.Context Context);

    // floating point refinements include:
    // a + b, where a and b are arithmetic
    // a - b, where a and b are arithmetic
    // -a, where a is arithmetic
    // a * b, where a and b are arithmetic, and a is constant
    // a / b, where a and b are arithmetic, and b is a constant
    // if a then b else c, where a is boolean, and b and c are arithmetic
    // floating point constant (single- and double-precision)
    // arithmetic -> floating cast
    public record class Add(FundamentalFloatRefExpr A, FundamentalFloatRefExpr B) : FundamentalFloatRefExpr([A, B])
    {
        public override Z3.FPExpr GetZ3Expression(Z3.Context Context) => Context.MkFPAdd(Context.MkFPRNE(), A.GetZ3Expression(Context), B.GetZ3Expression(Context));
    }
    public record class Subtract(FundamentalFloatRefExpr A, FundamentalFloatRefExpr B) : FundamentalFloatRefExpr([A, B])
    {
        public override Z3.FPExpr GetZ3Expression(Z3.Context Context) => Context.MkFPSub(Context.MkFPRNE(), A.GetZ3Expression(Context), B.GetZ3Expression(Context));
    }
    public record class Negate(FundamentalFloatRefExpr A) : FundamentalFloatRefExpr([A])
    {
        public override Z3.FPExpr GetZ3Expression(Z3.Context Context) => Context.MkFPNeg(A.GetZ3Expression(Context));
    }
    public record class Multiply : FundamentalFloatRefExpr
    {
        public FundamentalFloatRefExpr A;
        public FundamentalFloatRefExpr B;

        public Multiply(FundamentalFloatRefExpr A, FundamentalFloatRefExpr B) : base([A, B])
        {
            // a must be constant
            if (!A.IsConstant())
            {
                throw new Exception("a must be constant in fundamental multiplication expression a * b!");
            }
            this.A = A;
            this.B = B;
        }

        public override Z3.FPExpr GetZ3Expression(Z3.Context Context) => Context.MkFPMul(Context.MkFPRNE(), A.GetZ3Expression(Context), B.GetZ3Expression(Context));
    }
    public record class Divide : FundamentalFloatRefExpr
    {
        public FundamentalFloatRefExpr A;
        public FundamentalFloatRefExpr B;

        public Divide(FundamentalFloatRefExpr A, FundamentalFloatRefExpr B) : base([A, B])
        {
            // b must be constant
            if (!B.IsConstant())
            {
                throw new Exception("b must be constant in fundamental multiplication expression a / b!");
            }
            this.A = A;
            this.B = B;
        }

        public override Z3.FPExpr GetZ3Expression(Z3.Context Context) => Context.MkFPDiv(Context.MkFPRNE(), A.GetZ3Expression(Context), B.GetZ3Expression(Context));
    }
    public record class If(FundamentalBoolRefExpr A, FundamentalFloatRefExpr B, FundamentalFloatRefExpr C) : FundamentalFloatRefExpr([A, B, C])
    {
        public override Z3.FPExpr GetZ3Expression(Z3.Context Context) => (Z3.FPExpr)Context.MkITE(A.GetZ3Expression(Context), B.GetZ3Expression(Context), C.GetZ3Expression(Context));
    }

    // public record class Float(float Value) : FundamentalFloatRefExpr([])
    // {
    //     public override Z3.FPExpr GetZ3Expression(Z3.Context Context) => Context.MkFP(Value, Context.MkFPSort32());
    // }

    public record class Double(double Value) : FundamentalFloatRefExpr([])
    {
        public override Z3.FPExpr GetZ3Expression(Z3.Context Context) => Context.MkFP(Value, Context.MkFPSort64());
    }

    public record class ArithToFloatCast(FundamentalIntRefExpr A): FundamentalFloatRefExpr([A])
    {
        public override Z3.FPExpr GetZ3Expression(Z3.Context Context) => Context.MkFPToFP(Context.MkFPRNE(), Context.MkInt2Real(A.GetZ3Expression(Context)), Context.MkFPSort64());
    }
    
    public record class VariableRead(UVar.Double Variable) : FundamentalFloatRefExpr([])
    {
        public override bool IsConstant() => false;
        public override Z3.FPExpr GetZ3Expression(Z3.Context Context) => Variable.Z3Variable;
    }
}