using System;

namespace FluentPolicy.Modules.CircuitBreaker
{
    public sealed class CircuitBreakerException : Exception
    {
        public CircuitBreakerException(TimeSpan timeLeft) : base("The circuit breaker prevented completing this operation. The breaker will reset in: "+timeLeft)
        {
        }
    }
}