using System;

namespace FluentPolicy.Exceptions
{
    public sealed class AsyncPolicyException : Exception
    {
        internal AsyncPolicyException(string message) : base(message)
        {
        }
    }
}