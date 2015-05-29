using System;

namespace FluentPolicy
{
    public interface IPolicyExceptionConfigExpression<TReturn> : IPolicyConfigExpression<TReturn>
    {
        // TODO: Possibly move to more specialized one, so that it doesn't show for AllOtherExceptions
        IPolicyExceptionConfigExpression<TReturn> Or<TException>()
            where TException : Exception;

        IPolicyExceptionConfigExpression<TReturn> Or<TException>(Func<TException, bool> predicate)
            where TException : Exception;

        IPolicyBaseState<TReturn> Rethrow();
        IPolicyBaseState<TReturn> Throw(Func<Exception, Exception> factory);
        IPolicyBaseState<TReturn> Return(Func<Exception, TReturn> factory);
    }
}