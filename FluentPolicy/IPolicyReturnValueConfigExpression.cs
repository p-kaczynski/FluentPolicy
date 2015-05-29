using System;

namespace FluentPolicy
{
    public interface IPolicyReturnValueConfigExpression<TReturn> : IPolicyConfigExpression<TReturn>
    {
        IPolicyBaseState<TReturn> Return(Func<TReturn, TReturn> factory);
        IPolicyBaseState<TReturn> Throw(Func<TReturn, Exception> factory);

    }
}