using System;

namespace FluentPolicy.Modules.Logging
{
    public class LambdaLogger : BaseModule
    {
        private readonly Action<Exception> _exceptionLogAction;

        public LambdaLogger(Action<Exception> exceptionLogAction)
        {
            _exceptionLogAction = exceptionLogAction;
        }

        /// <exception cref="Exception">A delegate callback throws an exception.</exception>
        public override void RegisterEvents<TReturn>(IPolicyEvents<TReturn> eventsSource)
        {
            eventsSource.ExceptionThrown += (sender, exceptionArgs) => _exceptionLogAction(exceptionArgs.Exception);
        }
    }

    public class LambdaLogger<TReturn> : LambdaLogger
    {
        private readonly Action<TReturn> _resultLogAction;

        public LambdaLogger(Action<Exception> exceptionLogAction, Action<TReturn> resultLogAction) : base(exceptionLogAction)
        {
            _resultLogAction = resultLogAction;
        }

        /// <exception cref="InvalidCastException">Type mismatch between <see cref="LambdaLogger{TReturn}"/> and <see cref="PolicyBuilder{TReturn}"/>.</exception>
        /// <exception cref="Exception">A delegate callback throws an exception.</exception>
        public override void RegisterEvents<T>(IPolicyEvents<T> eventsSource)
        {
            base.RegisterEvents(eventsSource);

            if (!typeof (TReturn).IsAssignableFrom(typeof (T)))
                throw new InvalidCastException(
                    string.Format("This LambdaLogger is valid for type {0}, and cannot be used with policy typed {1}",
                        typeof (TReturn).Name, typeof (T).Name));

            // This is a bit awful cast, but it makes sense - it will work due to the previous IsAssignableFrom check
            eventsSource.ReturnValueObtained += (sender, e) => _resultLogAction((TReturn)(object) e);
        }
    }
}