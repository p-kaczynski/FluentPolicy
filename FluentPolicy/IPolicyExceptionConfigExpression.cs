using System;
using JetBrains.Annotations;

namespace FluentPolicy
{
    public interface IPolicyExceptionConfigExpression<TReturn> : IPolicyConfigExpression<TReturn>
    {
        // TODO: Possibly move to more specialized one, so that it doesn't show for AllOtherExceptions
        [PublicAPI]
        IPolicyExceptionConfigExpression<TReturn> Or<TException>()
            where TException : Exception;

        [PublicAPI]
        IPolicyExceptionConfigExpression<TReturn> Or<TException>(Func<TException, bool> predicate)
            where TException : Exception;

        [PublicAPI]
        IPolicyBaseState<TReturn> Rethrow();

        [PublicAPI]
        IPolicyBaseState<TReturn> Throw(Func<Exception, Exception> factory);

        [PublicAPI]
        IPolicyBaseState<TReturn> Return(Func<Exception, TReturn> factory);

        [PublicAPI]
        IPolicyRepeatConfigExpressionContinuation<TReturn> Repeat();

        [PublicAPI]
        IPolicyRepeatConfigExpressionContinuation<TReturn> Repeat(Action<Exception> callback);

        [PublicAPI]
        IPolicyRepeatConfigExpressionContinuation<TReturn> Repeat(int numberOfTimes);

        [PublicAPI]
        IPolicyRepeatConfigExpressionContinuation<TReturn> Repeat(int numberOfTimes, Action<Exception, int> callback);

        [PublicAPI]
        IPolicyRepeatConfigExpressionContinuation<TReturn> WaitAndRepeat(TimeSpan[] waitTimes);

        [PublicAPI]
        IPolicyRepeatConfigExpressionContinuation<TReturn> WaitAndRepeat(TimeSpan[] waitTimes, Action<Exception, int> callback);

        [PublicAPI]
        IPolicyRepeatConfigExpressionContinuation<TReturn> WaitAndRepeat(int numberOfTimes, Func<int, TimeSpan> getWaitTime);

        [PublicAPI]
        IPolicyRepeatConfigExpressionContinuation<TReturn> WaitAndRepeat(int numberOfTimes, Func<int, TimeSpan> getWaitTime, Action<Exception, int> callback);
    }
}