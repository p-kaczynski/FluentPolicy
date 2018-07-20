using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Timer = System.Timers.Timer;

namespace FluentPolicy.Modules.CircuitBreaker
{
    /// <summary>
    ///     A simple implementation of <see cref="ICircuitBreakerPersistence" /> using in-memory persistance
    /// </summary>
    public class CircuitBreakerInMemoryPersistence : ICircuitBreakerPersistence, IDisposable
    {
        private const int DefaultCleanUpMilliseconds = 5*60*1000;
        private const int DefaultMaxRememberTimeMinutes = 60;

        /// <summary>
        ///     Gets or sets the maximum exception remember time.
        /// </summary>
        /// <value>
        ///     The maximum remember time.
        /// </value>
        [PublicAPI]
        public TimeSpan MaxRememberTime { get; set; }

        [PublicAPI]
        public int CleanUpMilliseconds
        {
            get => _cleanUpMilliseconds;
            set
            {
                _cleanUpMilliseconds = value;
                _selfCleanUpTimer.Interval = value;
            }
        }

        private readonly Timer _selfCleanUpTimer = new Timer
        {
            AutoReset = true,
            Enabled = true,
            Interval = DefaultCleanUpMilliseconds
        };

        private static int _cleanUpMilliseconds;

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, ConcurrentBag<DateTime>>> _storage =
            new ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, ConcurrentBag<DateTime>>>();

        private readonly ConcurrentDictionary<Guid, DateTime> _trips = new ConcurrentDictionary<Guid, DateTime>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="CircuitBreakerInMemoryPersistence" /> class.
        /// </summary>
        [PublicAPI]
        public CircuitBreakerInMemoryPersistence()
        {
            _selfCleanUpTimer.Elapsed += (sender, args) => CleanUp();
            // ReSharper disable once ExceptionNotDocumented -- will not be thrown
            MaxRememberTime = TimeSpan.FromMinutes(DefaultMaxRememberTimeMinutes);
        }

        private void CleanUp()
        {
            _lock.EnterWriteLock();
            try
            {
                var cutOffPoint = DateTime.Now - MaxRememberTime;
                foreach (var policy in _storage.Keys)
                {
                    foreach (var behaviour in _storage[policy].Keys)
                    {
                        var newBag = new ConcurrentBag<DateTime>();
                        foreach (var dt in _storage[policy][behaviour].Where(dt => dt > cutOffPoint))
                            newBag.Add(dt);
                        if (newBag.Any())
                            _storage[policy][behaviour] = newBag;
                        else
                        {
                            _storage[policy].TryRemove(behaviour, out _);
                        }
                    }
                    if (!_storage[policy].Any())
                    {
                        _storage.TryRemove(policy, out _);
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void IncrementExceptionCount(Guid policyId, Guid behaviourId)
        {
            _lock.EnterReadLock();
            try
            {
                _storage.GetOrAdd(policyId).GetOrAdd(behaviourId).Add(DateTime.Now);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public int GetExceptionCount(Guid policyId, Guid behaviourId, TimeSpan duringLast)
        {
            _lock.EnterReadLock();
            try
            {
                return _storage.GetOrAdd(policyId).GetOrAdd(behaviourId).Count(dt => dt > DateTime.Now - duringLast);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void ResetExceptionCount(Guid policyId, Guid behaviourId)
        {
            _lock.EnterWriteLock();
            try
            {
                _storage.GetOrAdd(policyId)
                    .AddOrUpdate(behaviourId, new ConcurrentBag<DateTime>(),
                        (guid, bag) => new ConcurrentBag<DateTime>());
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void SetPolicyAsTripped(Guid policyId)
        {
            _lock.EnterReadLock();
            try
            {
                _trips.AddOrUpdate(policyId, DateTime.Now, (guid, time) => DateTime.Now);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public DateTime? GetPolicyLastTripTime(Guid policyId)
        {
            _lock.EnterReadLock();
            try
            {
                return _trips.TryGetValue(policyId, out var dt) 
                    ? dt 
                    : (DateTime?) null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Dispose()
        {
            _lock.Dispose();
        }
    }

    internal static class ConcurrentDictionaryUtils
    {
        internal static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key)
            where TValue : new()
        {
            return dict.GetOrAdd(key, new TValue());
        }
    }
}