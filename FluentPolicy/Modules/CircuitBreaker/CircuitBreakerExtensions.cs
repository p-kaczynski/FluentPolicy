namespace FluentPolicy.Modules.CircuitBreaker
{
    public static class CircuitBreakerExtensions
    {
        public static IPolicyExceptionConfigExpression<TReturn> TripCircuitBreaker<TReturn>(
            this IPolicyExceptionConfigExpression<TReturn> thisExceptionExpression, CircuitBreaker breaker)
        {
            breaker.TripFor(thisExceptionExpression.GetGuid());

            return thisExceptionExpression;
        }
    }
}
