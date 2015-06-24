using System;

namespace FluentPolicy
{
    public class ExceptionThrownEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
        public Guid? HandlerBehaviourGuid { get; set; }
    }
}