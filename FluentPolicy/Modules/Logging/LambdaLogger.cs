using System;
using JetBrains.Annotations;

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

        [PublicAPI]
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
                    $"This LambdaLogger is valid for type {typeof(TReturn).Name}, and cannot be used with policy typed {typeof(T).Name}");

            // This is a bit awful cast, but it makes sense - it will work due to the previous IsAssignableFrom check
            eventsSource.ReturnValueObtained += (sender, e) => _resultLogAction((TReturn)(object) e);
        }
    }
}