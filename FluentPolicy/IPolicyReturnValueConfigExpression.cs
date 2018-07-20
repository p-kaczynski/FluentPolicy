using System;
using JetBrains.Annotations;

namespace FluentPolicy
{
    public interface IPolicyReturnValueConfigExpression<TReturn> : IPolicyConfigExpression<TReturn>
    {
        [PublicAPI]
        IPolicyBaseState<TReturn> Return(Func<TReturn, TReturn> factory);

        [PublicAPI]
        IPolicyBaseState<TReturn> Throw(Func<TReturn, Exception> factory);

    }
}