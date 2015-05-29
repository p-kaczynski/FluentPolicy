namespace FluentPolicy
{
    public interface IPolicyRepeatConfigExpressionContinuation<TReturn>
    {
        IPolicyConfigExpression<TReturn> Then();
    }
}