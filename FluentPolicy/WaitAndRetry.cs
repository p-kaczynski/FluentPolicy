using System;

namespace FluentPolicy
{
    internal class WaitAndRetry : Exception
    {
        public TimeSpan WaitTime { get; set; }

        public WaitAndRetry(TimeSpan waitTime)
        {
            WaitTime = waitTime;
        }
    }
}