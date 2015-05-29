namespace FluentPolicy
{
    public interface IPolicyRepeatConfigExpression<TReturn>
    {
        IPolicyRepeatConfigExpressionContinuation<TReturn> Twice(int firstTimeOut, int secondTimeout);
    }
}