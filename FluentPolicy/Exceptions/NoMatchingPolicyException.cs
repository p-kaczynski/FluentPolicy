using System;

namespace FluentPolicy.Exceptions
{
    public sealed class NoMatchingPolicyException : FluentPolicyPublicExceptionBase
    {
        internal NoMatchingPolicyException(Exception innerException) : base(string.Empty, innerException)
        {
        }
    }
}