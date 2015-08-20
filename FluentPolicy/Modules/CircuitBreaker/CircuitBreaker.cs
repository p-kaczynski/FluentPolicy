using System;
using System.Collections.Generic;

namespace FluentPolicy.Modules.CircuitBreaker
{
    public class CircuitBreaker : BaseModule
    {
        public static readonly Func<CircuitBreakerException, Exception> DefaultExceptionFactory = c => c;

        protected readonly ICircuitBreakerPersistence Persistence;
        protected readonly List<Guid> TripForBehaviours = new List<Guid>();
        
        /// <summary>
        /// If false, only exceptions caught by behaviours that explicitly registered themselves with this circuit breaker will trigger it.
        /// Default: false
        /// </summary>
        public bool TripForAll { get; set; }

        /// <summary>
        /// If true, each exception behaviour configured by policy will have separate counter.
        /// Default: true
        /// </summary>
        public bool DifferentiateBetweenBehaviours { get; set; }

        /// <summary>
        /// How long to remember exceptions
        /// Default: 24 hours
        /// </summary>
        public TimeSpan ExceptionsExpireAfter { get; set; }

        /// <summary>
        /// How long until the circuit breaker resets itself
        /// Default: 1 minute
        /// </summary>
        public TimeSpan CooldownTime { get; set; }

        /// <summary>
        /// How many exceptions in the period of time (<see cref="ExceptionsExpireAfter"/>) will trip the breaker
        /// Default: 5
        /// </summary>
        public int Sensitivity { get; set; }

        /// <summary>
        /// What exception should be raised when the function is called, but the circuit breaker is tripped
        /// </summary>
        public Func<CircuitBreakerException, Exception> ExceptionFactory { get; set; } 

        /// <summary>
        /// Creates new instance of the CircuitBreaker
        /// </summary>
        /// <param name="persistence">Implementation of interface that provides persistence between calls</param>
        public CircuitBreaker(ICircuitBreakerPersistence persistence) :this()
        {
            Persistence = persistence;
        }

        private CircuitBreaker()
        {
            // Set Defaults:
            TripForAll = false;
            DifferentiateBetweenBehaviours = true;
            ExceptionsExpireAfter = TimeSpan.FromHours(24);
            CooldownTime = TimeSpan.FromMinutes(1);
            Sensitivity = 5;
            ExceptionFactory = DefaultExceptionFactory;
        }

        protected virtual void OnException(object policyObj, ExceptionThrownEventArgs args)
        {
            var guid = args.HandlerBehaviourGuid;

            if (!guid.HasValue) return;

            var policy = policyObj as PolicyBuilder;
            if (policy == null) return;

            if (!TripForAll && !TripForBehaviours.Contains(guid.Value)) return;

            var behaviourGuid = DifferentiateBetweenBehaviours ? guid.Value : Guid.Empty;
            Persistence.IncrementExceptionCount(policy.Id, behaviourGuid);

            Check(policy.Id, behaviourGuid);
        }

        protected virtual void Check(Guid policyId, Guid behaviourGuid)
        {
            var errorsInTimeSpan = Persistence.GetExceptionCount(policyId, behaviourGuid, ExceptionsExpireAfter);
            if (errorsInTimeSpan < Sensitivity) return;

            Persistence.SetPolicyAsTripped(policyId);
            throw ExceptionFactory(new CircuitBreakerException(CooldownTime));
        }

        public override void RegisterEvents<TReturn>(IPolicyEvents<TReturn> eventsSource)
        {
            eventsSource.ExceptionThrown += OnException;
        }

        public override void BeforeCall(PolicyBuilder policy)
        {
            var lastTripTime = Persistence.GetPolicyLastTripTime(policy.Id);
            if (!lastTripTime.HasValue || lastTripTime.Value + CooldownTime < DateTime.Now) return;
            throw ExceptionFactory(new CircuitBreakerException(lastTripTime.Value + CooldownTime - DateTime.Now));
        }

        protected internal void TripFor(Guid behaviourGuid)
        {
            TripForBehaviours.Add(behaviourGuid);
        }
    }
}