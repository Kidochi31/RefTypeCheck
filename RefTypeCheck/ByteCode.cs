

// The byte code has no concept of refinement types
// Refinement types are communicated via assumptions and assertions



// internal record class BType(string Name)
// {
//     internal record class Function(string Name, List<BType> ParameterTypes, BType ReturnType) : BType(Name)
//     {
//         public Func<Variable, List<Variable>, Z3.Context, Z3.BoolExpr>? RefinementsOnAssignment = null;
//         public (List<Variable>, List<BStmt>) ArgumentRefinements = new();
//         public (Variable, List<Variable>, List<BStmt>) ReturnRefinements = new();
//     }
// }

using Microsoft.Z3;

internal record class Z3Management(Z3.Context Context)
{
    Z3.Sort VariableSort = Context.MkIntSort();//Context.MkUninterpretedSort("Variable");

    Z3.FuncDecl? _toBool = null;
    Z3.FuncDecl ToBool => _toBool ??= Context.MkFuncDecl("toBool", [VariableSort], Context.MkBoolSort());
    Z3.FuncDecl? _toInt = null;
    Z3.FuncDecl ToInt => _toInt ??= Context.MkFuncDecl("toInt", [VariableSort], Context.MkIntSort());

    Dictionary<int, Z3.FuncDecl> Functions = new Dictionary<int, Z3.FuncDecl>();
    Dictionary<Variable, Z3.Expr> Variables = new Dictionary<Variable, Z3.Expr>();

    public Z3.FuncDecl GetZ3FuncDecl(int parameters)
    {
        if(Functions.ContainsKey(parameters)){
            return Functions[parameters];
        }
        return Functions[parameters] = Context.MkFuncDecl("function", [VariableSort, Context.MkIntSort(), .. Enumerable.Repeat(VariableSort, parameters)], VariableSort);
    }

    public Z3.Expr ApplyFunc(Variable function, List<Variable> parameters, int outputIndex)
    {
        Z3.FuncDecl funcDecl = GetZ3FuncDecl(parameters.Count);
        return Context.MkApp(funcDecl, [GetZ3Variable(function), Context.MkInt(outputIndex), ..from var in parameters select GetZ3Variable(var)]);
    }

    public Z3.Expr GetZ3Variable(Variable variable)
    {
        if (Variables.ContainsKey(variable))
        {
            return Variables[variable];
        }
        return Variables[variable] = Context.MkConst(variable.ToString(), VariableSort);
    }

    public Z3.BoolExpr GetZ3BoolVariable(Variable variable)
    {
        Z3.Expr z3Variable = GetZ3Variable(variable);
        return (Z3.BoolExpr) Context.MkApp(ToBool, [z3Variable]);
    }

    public Z3.IntExpr GetZ3IntVariable(Variable variable)
    {
        Z3.Expr z3Variable = GetZ3Variable(variable);
        return (Z3.IntExpr) Context.MkApp(ToInt, [z3Variable]);
    }
}

internal class Variable(string name)
{

    static int numberCounter = 0;
    public string Name = name;
    public int Id = numberCounter++;

    public static bool operator ==(Variable a, Variable b)
    {
        return ReferenceEquals(a, b);
    }
    public static bool operator !=(Variable a, Variable b)
    {
        return !ReferenceEquals(a, b);
    }

    public override string ToString()
    {
        return $"{Name}%{Id}";
    }
}


internal abstract record class BExpr
{
    // Exprs can be:
    // Variable read
    // Function Call
    // Constant (so far, int and bool)
    public record class VariableRead(Variable Variable) : BExpr;
    public record class FunctionCall(Variable Function, List<Variable> Arguments, int OutputIndex) : BExpr
    {
        public override string ToString()
        {
            return $"FunctionCall({Function}, [{string.Join(", ",Arguments)}], {OutputIndex})";
        }
    }
    public record class IntConstant(int Value) : BExpr;
    public record class BoolConstant(bool Value) : BExpr;
}

internal abstract record class BBlock()
{
    public record class Basic(List<BStmt> Body) : BBlock()
    {
        public override string ToString()
        {
            return $"BasicBlock([{string.Join(",\n",Body)}])";
        }
    }
}

internal abstract record class BStmt
{
    // Stmts can be:
    // Assignments (which in SSA are also variable declarations)
    // Assumption
    // Assertion
    public record class Assignment(Variable Variable, BExpr Value) : BStmt;
    public record class Assumption(Variable Variable) : BStmt;
    public record class Assertion(Variable Variable) : BStmt;
    public record class Z3Assumption(Z3AssumptionFunction AssumptionFunction, Variable CheckVar, List<Variable> Arguments) : BStmt
    {
        public override string ToString()
        {
            return $"Z3Assumption {{ {AssumptionFunction}, {CheckVar}, [{string.Join(", ",Arguments)}] }}";
        }
    }
}


internal record class Z3AssumptionFunction(string Name, Z3AssumptionDelegate Function);
internal delegate Z3.BoolExpr Z3AssumptionDelegate(Z3Management management, List<Variable> variables);
