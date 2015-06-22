using System;

namespace FluentPolicy
{
    public interface IPolicyModule
    {
        void RegisterEvents<TReturn>(IPolicyEvents<TReturn> eventsSource);

        void BeforeCall();
        void AfterCall();
    }

    public interface IPolicyEvents<TReturn>
    {
        event EventHandler<Exception> ExceptionThrown;
        event EventHandler<TReturn> ReturnValueObtained;
    }
}