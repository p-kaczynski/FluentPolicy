using System;
using JetBrains.Annotations;

namespace FluentPolicy
{
    public interface IPolicyConditionSelector<TReturn>
    {
        [PublicAPI]
        IPolicyExceptionConfigExpression<TReturn> Exception<TException>()
            where TException : Exception;

        [PublicAPI]
        IPolicyExceptionConfigExpression<TReturn> Exception<TException>(Func<TException, bool> predicate)
            where TException : Exception;

        [PublicAPI]
        IPolicyReturnValueConfigExpression<TReturn> ReturnValue(Func<TReturn, bool> predicate);

        [PublicAPI]
        IPolicyExceptionConfigExpression<TReturn> AllOtherExceptions();

        [PublicAPI]
        IPolicyReturnValueConfigExpression<TReturn> AllOtherValues();
    }
}