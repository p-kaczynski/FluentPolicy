using System;

namespace FluentPolicy
{
    internal class WaitAndRetry : Exception
    {
        public int WaitTime { get; set; }
    }
}