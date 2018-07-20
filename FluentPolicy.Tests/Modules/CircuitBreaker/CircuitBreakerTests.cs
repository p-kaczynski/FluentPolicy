using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentPolicy.Modules.CircuitBreaker;
using FluentAssertions;
using Xunit;

namespace FluentPolicy.Tests.Modules.CircuitBreaker
{
    public class CircuitBreakerTests : IDisposable
    {
        private const string SampleExceptionMessage = "qwertyuiop";
        private const string SampleReturnString = "zxccvbnm";
        
        private static readonly CircuitBreakerInMemoryPersistence Persistence = new CircuitBreakerInMemoryPersistence();
        private readonly CircuitBreakerInMemoryPersistence _freshPersistence;

        public CircuitBreakerTests()
        {
            _freshPersistence = new CircuitBreakerInMemoryPersistence();
        }

        public void Dispose()
        {
            _freshPersistence.Dispose();
        }

        [Fact]
        public void CircuitBreaker_AllowsNormalExecution()
        {
            var breaker = new FluentPolicy.Modules.CircuitBreaker.CircuitBreaker(Persistence)
            {
                TripForAll = true,
                DifferentiateBetweenBehaviours = false,
                Sensitivity = 1
            };
            for (var i = 0; i < 5; ++i)
            {
                As.Func(TestFunction).WithPolicy()
                    .For().AllOtherExceptions().Rethrow()
                    .AddModule(breaker)
                    .Execute()
                    .Should().Be(SampleReturnString);
            }
        }

        [Fact]
        public void CircuitBreaker_Trips()
        {
            var breaker = new FluentPolicy.Modules.CircuitBreaker.CircuitBreaker(Persistence)
            {
                Sensitivity = 4
            };
            var policy = As.Func(TestFunctionException).WithPolicy()
                .For().Exception<TestException>().TripCircuitBreaker(breaker).Return(SampleReturnString)
                .For().AllOtherExceptions().Rethrow()
                .AddModule(breaker);

            for (var i = 0; i < 3; ++i)
            {
                // 4 exceptions
                policy
                    .Execute()
                    .Should().Be(SampleReturnString);
            }
            new Action(() => policy.Execute()).Should().Throw<CircuitBreakerException>();
        }

        [Fact]
        public void CircuitBreaker_Resets()
        {
            var breaker = new FluentPolicy.Modules.CircuitBreaker.CircuitBreaker(Persistence)
            {
                Sensitivity = 4,
                CooldownTime = TimeSpan.FromSeconds(2)
            };
            var policy = As.Func(TestFunctionException).WithPolicy()
                .For().Exception<TestException>().TripCircuitBreaker(breaker).Return(SampleReturnString)
                .For().AllOtherExceptions().Rethrow()
                .AddModule(breaker);

            for (var i = 0; i < 3; ++i)
            {
                // 4 exceptions
                policy
                    .Execute()
                    .Should().Be(SampleReturnString);
            }
            new Action(() => policy.Execute()).Should().Throw<CircuitBreakerException>();
            new Action(() => policy.Execute(TestFunction)).Should().Throw<CircuitBreakerException>();
            Thread.Sleep(breaker.CooldownTime);
            policy.Execute(TestFunction).Should().Be(SampleReturnString);
        }

        // Tests for built in in-memory persistence
        [Fact]
        public void Persistence_StoresFromManyThreads()
        {
            const int root = 200;
            const int multiplier = 100;
            _freshPersistence.CleanUpMilliseconds = 1000000;
            const int threadNo = multiplier * root;

            var policies = Enumerable.Repeat(0, multiplier).Select(_ => Guid.NewGuid()).ToArray();
            var behaviours = Enumerable.Repeat(0, multiplier).Select(_ => Guid.NewGuid()).ToArray();
            var tasks = new Task[threadNo];
            for (var i = 0; i < threadNo; ++i)
            {
                var threadId = i;
                tasks[i] =
                    Task.Run(
                        () =>
                            _freshPersistence.IncrementExceptionCount(policies[threadId % multiplier], behaviours[threadId % multiplier]));
            }
            Task.WaitAll(tasks.ToArray());
            // there are multiplier * root calls, spread evently amongst multiplier policy/behaviour guids. That means, that each behaviour should have root exceptions... I think
            for (var i = 0; i < multiplier; ++i)
                _freshPersistence.GetExceptionCount(policies[i], behaviours[i], TimeSpan.FromMilliseconds(1000000))
                    .Should().Be(root);
        }

        [Fact]
        public void Persistence_PoliciesCanBeMarked()
        {
            var policy = Guid.NewGuid();
            _freshPersistence.SetPolicyAsTripped(policy);
            var first = _freshPersistence.GetPolicyLastTripTime(policy);
            first.Should().HaveValue();
            first.Value.Should().BeAfter(DateTime.Now - TimeSpan.FromMinutes(10));

            _freshPersistence.SetPolicyAsTripped(policy);
            var second = _freshPersistence.GetPolicyLastTripTime(policy);
            second.Should().HaveValue();
            second.Value.Should().BeAfter(first.Value);
        }

        // Helper methods
        private static string TestFunction()
        {
            return SampleReturnString;
        }

        private static string TestFunctionException()
        {
            throw new TestException();
        }

        private static string TestFunctionExceptionWithMessage()
        {
            throw new TestException(SampleExceptionMessage);
        }

        private static readonly TestException ReusableException = new TestException(SampleExceptionMessage);

        private static string TestFunctionReusableException()
        {
            throw ReusableException;
        }
    }
}