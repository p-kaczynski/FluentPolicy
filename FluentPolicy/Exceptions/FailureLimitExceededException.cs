using System;

namespace FluentPolicy.Exceptions
{
    internal class FailureLimitExceededException : Exception
    {
        public Guid FailedBehaviourId { get; }
        public FailureLimitExceededException(Exception innerException, Guid failedBehaviourId) : base(string.Empty, innerException)
        {
            FailedBehaviourId = failedBehaviourId;
        }
    }
}