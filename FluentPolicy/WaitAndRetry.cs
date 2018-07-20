using System;

namespace FluentPolicy
{
    internal class WaitAndRetry : Exception
    {
        internal TimeSpan WaitTime { get; }

        internal WaitAndRetry(TimeSpan waitTime)
        {
            WaitTime = waitTime;
        }
    }
}