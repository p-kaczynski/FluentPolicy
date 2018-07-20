using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace FluentPolicy
{
    public static class PolicyAttachmentHelper
    {
        [PublicAPI]
        public static IPolicyBaseState<TReturn> WithPolicy<TReturn>(this Func<TReturn> func)
        {
            return new PolicyBuilder<TReturn>(func);
        }

        [PublicAPI]
        public static IPolicyBaseState<TReturn> WithAsyncPolicy<TReturn>(this Func<Task<TReturn>> func)
        {
            return new PolicyBuilder<TReturn>(func);
        }
    }
}