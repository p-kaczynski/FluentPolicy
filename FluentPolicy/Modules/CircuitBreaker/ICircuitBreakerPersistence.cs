using System;

namespace FluentPolicy.Modules.CircuitBreaker
{
    public interface ICircuitBreakerPersistence
    {
        void IncrementExceptionCount(Guid policyId, Guid behaviourId);
        int GetExceptionCount(Guid policyId, Guid behaviourId, TimeSpan duringLast);
        void ResetExceptionCount(Guid policyId, Guid behaviourId);

        void SetPolicyAsTripped(Guid policyId);
        DateTime? GetPolicyLastTripTime(Guid policyId);
    }
}