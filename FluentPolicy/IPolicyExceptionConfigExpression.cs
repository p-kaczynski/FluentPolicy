using System;
using System.Threading.Tasks;

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

        IPolicyRepeatConfigExpressionContinuation<TReturn> Repeat();
        IPolicyRepeatConfigExpressionContinuation<TReturn> Repeat(Action<Exception> callback);
        IPolicyRepeatConfigExpressionContinuation<TReturn> Repeat(int numberOfTimes);
        IPolicyRepeatConfigExpressionContinuation<TReturn> Repeat(int numberOfTimes, Action<Exception, int> callback);
        IPolicyRepeatConfigExpressionContinuation<TReturn> WaitAndRepeat(TimeSpan[] waitTimes);
        IPolicyRepeatConfigExpressionContinuation<TReturn> WaitAndRepeat(TimeSpan[] waitTimes, Action<Exception, int> callback);
        IPolicyRepeatConfigExpressionContinuation<TReturn> WaitAndRepeat(int numberOfTimes, Func<int, TimeSpan> getWaitTime);
        IPolicyRepeatConfigExpressionContinuation<TReturn> WaitAndRepeat(int numberOfTimes, Func<int, TimeSpan> getWaitTime, Action<Exception, int> callback);
    }
}