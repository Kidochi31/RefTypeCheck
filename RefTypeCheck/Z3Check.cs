
internal record class Z3ImplicationResult(Z3ImplicationStatus Status, Z3.Model? Counterexample);

internal enum Z3ImplicationStatus
{
    Proven,
    Disproven,
    Unprovable
}


internal static class Z3Check
{
    public static Z3ImplicationResult CheckImplication(Z3.BoolExpr premises, Z3.BoolExpr conclusion, Z3.Context context)
    {
        Z3.BoolExpr test = context.MkAnd(premises, context.MkNot(conclusion));
        Z3.Solver solver = context.MkSolver();
        solver.Add(test);

        // check for satisfiability
        Z3.Status status = solver.Check();
        if (status == Z3.Status.SATISFIABLE)
        {
            Z3.Model model = solver.Model;
            return new Z3ImplicationResult(Z3ImplicationStatus.Disproven, model);
        }else if (status == Z3.Status.UNKNOWN)
        {
            return new Z3ImplicationResult(Z3ImplicationStatus.Unprovable, null);
        }
        else
        {
            return new Z3ImplicationResult(Z3ImplicationStatus.Proven, null);
        }
    }
}