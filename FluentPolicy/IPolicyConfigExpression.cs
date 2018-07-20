using System;
using System.ComponentModel;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace FluentPolicy
{
    public interface IPolicyConfigExpression<TReturn>
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        Guid GetGuid();

        [PublicAPI]
        IPolicyBaseState<TReturn> Return(TReturn returnObject);

        [PublicAPI]
        IPolicyBaseState<TReturn> Return(Func<TReturn> returnObjectFactory);

        [PublicAPI]
        IPolicyBaseState<TReturn> ReturnAsync(Task<TReturn> returnObject);

        [PublicAPI]
        IPolicyBaseState<TReturn> ReturnAsync(Func<Task<TReturn>> returnObjectFactory);

        [PublicAPI]
        IPolicyBaseState<TReturn> ReturnDefault();
    }
}