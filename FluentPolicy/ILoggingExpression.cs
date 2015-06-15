using System;

namespace FluentPolicy
{
    public interface ILoggingExpression<TReturn>
    {
        IPolicyBaseState<TReturn> Exceptions(Action<Exception> exceptionLogAction);
        IPolicyBaseState<TReturn> ReturnValues(Action<TReturn> returnValueLogAction);
    }
}