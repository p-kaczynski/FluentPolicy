using System;
using System.Threading.Tasks;

namespace FluentPolicy
{
    public static class PolicyAttachmentHelper
    {
        public static IPolicyBaseState<TReturn> WithPolicy<TReturn>(this Func<TReturn> func)
        {
            return new PolicyBuilder<TReturn>(func);
        }

        public static IPolicyBaseState<TReturn> WithPolicy<TReturn>(this Task<TReturn> task)
        {
            return new PolicyBuilder<TReturn>(task);
        }
    }
}