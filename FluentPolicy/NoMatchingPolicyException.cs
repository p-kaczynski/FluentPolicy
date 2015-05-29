using System;

namespace FluentPolicy
{
    public sealed class NoMatchingPolicyException : Exception
    {
        internal NoMatchingPolicyException(Exception innerException) : base(string.Empty, innerException)
        {
        }
    }
}