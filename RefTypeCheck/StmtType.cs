
// Okay, so this is the rundown
// Types will be implemented in two completely separate parts
// The first part is the base part type - int, bool, char etc.
// the second part is the refinement part type - [v|v > 0] etc.
// both of these will be implemented via bytecode
// as "statement types"
// E.g. [v|v>=0]
// is implemented as a refinement type
// using variable v:
// var0 = 0
// var1 = v >= var0
// confirm var1

// this "confirm" statement will become an assert/assume statement
// depending on whether the value is being put into somewhere
// or taken out


internal record class TypedVariable(Variable Variable, BaseType Type)
{
    private Z3.Expr? Z3Variable = null;
    public Z3.Expr GetZ3Variable(Z3SortFinder sortFinder) => Z3Variable ?? (Z3Variable = sortFinder.Context.MkConst(Variable.Name, sortFinder.GetSort(Type.GenericType.Sort)));
}

internal record class ValueType(BaseType BaseType, TypeRefinement Refinements)
{
    public static ValueType UnrefinedIntType = new ValueType(BaseType.IntType, new TypeRefinement(new Variable("int"), null, []));
    public static ValueType UnrefinedBoolType = new ValueType(BaseType.BoolType, new TypeRefinement(new Variable("int"), null, []));
}

internal record class TypeRefinement(Variable Value, Variable? ConfirmVariable, List<UBStmt<Variable>> TypingStatements);

internal record class Z3SortFinder(Z3.Context Context)
{
    Z3.Sort? OtherSort = null;
    Z3.Sort? BoolSort = null;
    Z3.Sort? IntSort = null;
    public Z3.Sort GetSort(Z3Sort Sort)
    {
        switch (Sort)
        {
            case Z3Sort.Bool:
                return BoolSort is not null? BoolSort : (BoolSort = Context.MkBoolSort());
            case Z3Sort.Int:
                return IntSort is not null? IntSort : (IntSort = Context.MkBoolSort());
            case Z3Sort.Other:
                return OtherSort is not null ? OtherSort : (OtherSort = Context.MkUninterpretedSort("Other"));
            default:
                throw new Exception("unexpected sort!");
        }
    }

    Dictionary<List<Z3.Sort>, Z3.FuncDecl> Functions = new Dictionary<List<Z3.Sort>, Z3.FuncDecl>(new ListEqualityComparer<Z3.Sort>());

    public Z3.FuncDecl GetFuncDecl(List<Z3Sort> Parameters, Z3Sort ReturnType)
    {
        List<Z3.Sort> functionSignature = [.. from param in Parameters select GetSort(param), GetSort(ReturnType)];
        if(Functions.ContainsKey(functionSignature)){
            return Functions[functionSignature];
        }
        return Functions[functionSignature] = Context.MkFuncDecl("function", [GetSort(Z3Sort.Other), .. from param in Parameters select GetSort(param)], GetSort(ReturnType));
    }
}

internal enum Z3Sort
{
    Int,
    Bool,
    Other
}

internal record class BaseGenericType(string Name, int Args, Z3Sort Sort)
{
    public static BaseGenericType BoolType = new BaseGenericType("Bool", 0, Z3Sort.Bool);
    public static BaseGenericType IntType = new BaseGenericType("Int", 0, Z3Sort.Int);
    public static List<BaseGenericType> FunctionTypes = new List<BaseGenericType>
    {
        new BaseGenericType("Function0", 1, Z3Sort.Other),
        new BaseGenericType("Function1", 2, Z3Sort.Other),
        new BaseGenericType("Function2", 3, Z3Sort.Other),
    };
}

internal record class BaseType(BaseGenericType GenericType, List<ValueType> Parameters, InvokeType? InvokeType)
{
    public static BaseType BoolType = new BaseType(BaseGenericType.BoolType, [], null);
    public static BaseType IntType = new BaseType(BaseGenericType.IntType, [], null);
}

internal record class InvokeType(List<Variable> Parameters, List<ValueType> ParameterTypes, ValueType ResultType);