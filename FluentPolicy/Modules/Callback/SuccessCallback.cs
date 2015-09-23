using System;

namespace FluentPolicy.Modules.Callback
{
    public class SuccessCallback : BaseModule
    {
        private readonly Action _action;

        public SuccessCallback(Action action)
        {
            _action = action;
        }

        #region Overrides of BaseModule

        public override void RegisterEvents<TReturn>(IPolicyEvents<TReturn> eventsSource)
        {
            eventsSource.ReturnValueObtained += (sender, @return) => _action();
        }

        #endregion
    }
}