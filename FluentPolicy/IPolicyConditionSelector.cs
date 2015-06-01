using System;

namespace FluentPolicy
{
    public interface IPolicyConditionSelector<TReturn>
    {
        IPolicyExceptionConfigExpression<TReturn> Exception<TException>()
            where TException : Exception;

        IPolicyExceptionConfigExpression<TReturn> Exception<TException>(Func<TException, bool> predicate)
            where TException : Exception;

        IPolicyReturnValueConfigExpression<TReturn> ReturnValue(Func<TReturn, bool> predicate);

        IPolicyExceptionConfigExpression<TReturn> AllOtherExceptions();
        IPolicyReturnValueConfigExpression<TReturn> AllOtherValues();
    }
}