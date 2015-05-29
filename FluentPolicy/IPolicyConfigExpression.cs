namespace FluentPolicy
{
    public interface IPolicyConfigExpression<TReturn>
    {
        IPolicyRepeatConfigExpression<TReturn> Repeat();
        IPolicyBaseState<TReturn> Return(TReturn returnObject);
    }
}