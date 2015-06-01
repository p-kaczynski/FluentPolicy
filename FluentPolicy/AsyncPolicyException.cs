using System;

namespace FluentPolicy
{
    public sealed class AsyncPolicyException : Exception
    {
        internal AsyncPolicyException(string message) : base(message)
        {
        }
    }
}