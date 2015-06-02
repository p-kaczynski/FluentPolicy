using System;
using System.Threading.Tasks;

namespace FluentPolicy
{
    public static class As
    {
        public static Func<TReturn> Func<TReturn>(Func<TReturn> expr)
        {
            return expr;
        }
    }
}