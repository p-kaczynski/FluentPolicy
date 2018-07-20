using JetBrains.Annotations;

namespace FluentPolicy
{
    public interface IPolicyRepeatConfigExpressionContinuation<TReturn>
    {
        [PublicAPI]
        IPolicyExceptionConfigExpression<TReturn> Then();
    }
}