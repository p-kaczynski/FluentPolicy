namespace FluentPolicy
{
    public interface IPolicyConfigExpression<TReturn>
    {
        IPolicyBaseState<TReturn> Return(TReturn returnObject);
    }
}