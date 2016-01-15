﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace FluentPolicy.Modules.CircuitBreaker
{
    /// <summary>
    ///     An overridable implementation of the Circuit Breaker pattern for use with the <see cref="PolicyBuilder" />.
    ///     Allows monitoring of errors or invalid returns from a policy implementation, and cuts off access to it, if treshold
    ///     is reached
    /// </summary>
    [SuppressMessage("ReSharper", "ThrowingSystemException",
        Justification = "Actually throws whatever exception is created by ExceptionFactory")]
    public class CircuitBreaker : BaseModule
    {
        private static readonly Func<CircuitBreakerException, Exception> DefaultExceptionFactory = c => c;

        /// <summary>
        ///     The persistence layer for circuit breaker
        /// </summary>
        protected readonly ICircuitBreakerPersistence Persistence;

        /// <summary>
        ///     A list of behaviour Guids for which the CircuitBreaker will be active
        /// </summary>
        protected readonly List<Guid> TripForBehaviours = new List<Guid>();

        /// <summary>
        ///     If false, only exceptions caught by behaviours that explicitly registered themselves with this circuit breaker will
        ///     trigger it.
        ///     Default: false
        /// </summary>
        public bool TripForAll { get; set; }

        /// <summary>
        ///     If true, each exception behaviour configured by policy will have separate counter.
        ///     Default: true
        /// </summary>
        public bool DifferentiateBetweenBehaviours { get; set; }

        /// <summary>
        ///     How long to remember exceptions
        ///     Default: 24 hours
        /// </summary>
        public TimeSpan ExceptionsExpireAfter { get; set; }

        /// <summary>
        ///     How long until the circuit breaker resets itself
        ///     Default: 1 minute
        /// </summary>
        public TimeSpan CooldownTime { get; set; }

        /// <summary>
        ///     How many exceptions in the period of time (<see cref="ExceptionsExpireAfter" />) will trip the breaker
        ///     Default: 5
        /// </summary>
        public int Sensitivity { get; set; }

        /// <summary>
        ///     What exception should be raised when the function is called, but the circuit breaker is tripped
        /// </summary>
        public Func<CircuitBreakerException, Exception> ExceptionFactory { get; set; }

        /// <summary>
        ///     Creates new instance of the CircuitBreaker
        /// </summary>
        /// <param name="persistence">Implementation of interface that provides persistence between calls</param>
        public CircuitBreaker(ICircuitBreakerPersistence persistence) : this()
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

        /// <summary>
        ///     The <see cref="IPolicyEvents{TReturn}.ExceptionThrown" /> event handler
        /// </summary>
        /// <param name="policyObj">The policy object.</param>
        /// <param name="args">The <see cref="ExceptionThrownEventArgs" /> instance containing the event data.</param>
        /// <exception cref="Exception">
        ///     If the exception count in a given timeframe exceed allowed limit. Created by
        ///     <see cref="ExceptionFactory" />
        /// </exception>
        protected virtual void OnException(object policyObj, ExceptionThrownEventArgs args)
        {
            var guid = args.HandlerBehaviourGuid;

            if (!guid.HasValue) return;

            var policy = policyObj as PolicyBuilder;
            if (policy == null) return;

            if (!TripForAll && !TripForBehaviours.Contains(guid.Value)) return;

            // don't increment if already tripped, that could lead up to a build-up when cooldown < exception expire
            var lastTripTime = Persistence.GetPolicyLastTripTime(policy.Id);
            if (IsTripped(lastTripTime)) return;


            var behaviourGuid = DifferentiateBetweenBehaviours ? guid.Value : Guid.Empty;
            Persistence.IncrementExceptionCount(policy.Id, behaviourGuid);

            Check(policy.Id, behaviourGuid);
        }

        [ContractAnnotation("null=>false")]
        private bool IsTripped(DateTime? lastTripTime)
        {
            return lastTripTime.HasValue && lastTripTime.Value + CooldownTime >= DateTime.Now;
        }

        /// <summary>
        ///     Checks the error count for the specified policy identifier.
        /// </summary>
        /// <param name="policyId">The policy identifier.</param>
        /// <param name="behaviourGuid">The behaviour unique identifier.</param>
        /// <exception cref="Exception">
        ///     If the exception count in a given timeframe exceed allowed limit. Created by
        ///     <see cref="ExceptionFactory" />
        /// </exception>
        protected virtual void Check(Guid policyId, Guid behaviourGuid)
        {
            var errorsInTimeSpan = Persistence.GetExceptionCount(policyId, behaviourGuid, ExceptionsExpireAfter);
            if (errorsInTimeSpan < Sensitivity) return;

            Persistence.SetPolicyAsTripped(policyId);
            throw ExceptionFactory(new CircuitBreakerException(CooldownTime));
        }

        /// <summary>
        ///     Registers the events. Uses <see cref="OnException" /> by default.
        /// </summary>
        /// <typeparam name="TReturn">The type of the return.</typeparam>
        /// <param name="eventsSource">The events source.</param>
        public override void RegisterEvents<TReturn>(IPolicyEvents<TReturn> eventsSource)
        {
            eventsSource.ExceptionThrown += OnException;
        }

        /// <summary>
        ///     Executes before the policy calls the implementation. Here the check for tripped policy is made.
        /// </summary>
        /// <param name="policy">The policy.</param>
        /// <exception cref="Exception">If policy is tripped. Created by <see cref="ExceptionFactory" /></exception>
        public override void BeforeCall(PolicyBuilder policy)
        {
            var lastTripTime = Persistence.GetPolicyLastTripTime(policy.Id);
            if (!IsTripped(lastTripTime)) return;
            throw ExceptionFactory(new CircuitBreakerException(lastTripTime.Value + CooldownTime - DateTime.Now));
        }

        /// <summary>
        ///     Adds specified Guid to the <see cref="TripForBehaviours" />
        /// </summary>
        /// <param name="behaviourGuid">The behaviour unique identifier.</param>
        protected internal void TripFor(Guid behaviourGuid)
        {
            TripForBehaviours.Add(behaviourGuid);
        }
    }
}