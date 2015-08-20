using System;

namespace FluentPolicy.Exceptions
{
    public abstract class FluentPolicyPublicExceptionBase : Exception
    {
        protected FluentPolicyPublicExceptionBase()
        {
        }

        protected FluentPolicyPublicExceptionBase(string message) : base(message)
        {
            
        }
        protected FluentPolicyPublicExceptionBase(string message, Exception innerException) : base(message, innerException)
        {
            
        }
    }
}