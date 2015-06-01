using System;

namespace FluentPolicy
{
    internal class FailureLimitExceededException : Exception
    {
        public Guid FailedBehaviourId { get; private set; }
        public FailureLimitExceededException(Exception innerException, Guid failedBehaviourId) : base(string.Empty, innerException)
        {
            FailedBehaviourId = failedBehaviourId;
        }
    }
}