using System;

namespace FluentPolicy
{
    public interface IPolicyModule
    {
        void RegisterEvents<TReturn>(IPolicyEvents<TReturn> eventsSource);

        void BeforeCall(PolicyBuilder policy);
        void AfterCall(PolicyBuilder policy);
    }

    public interface IPolicyEvents<TReturn>
    {
        /// <summary>
        /// Event fired when exception is thrown from the underlying expression. The object passed is the exception, and the sender is behaviour that will handle it.
        /// </summary>
        event EventHandler<ExceptionThrownEventArgs> ExceptionThrown;

        /// <summary>
        /// Event fired when exception is thrown from the underlying expression. The object passed is the exception, and the sender is behaviour that will handle it.
        /// </summary>
        event EventHandler<TReturn> ReturnValueObtained;
    }
}