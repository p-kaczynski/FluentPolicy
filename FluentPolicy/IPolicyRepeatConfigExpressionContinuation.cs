namespace FluentPolicy
{
    public interface IPolicyRepeatConfigExpressionContinuation<TReturn>
    {
        IPolicyExceptionConfigExpression<TReturn> Then();
    }
}