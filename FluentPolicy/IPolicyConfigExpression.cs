using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace FluentPolicy
{
    public interface IPolicyConfigExpression<TReturn>
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        Guid GetGuid();

        IPolicyBaseState<TReturn> Return(TReturn returnObject);
        IPolicyBaseState<TReturn> Return(Func<TReturn> returnObjectFactory);
        IPolicyBaseState<TReturn> ReturnAsync(Task<TReturn> returnObject);
        IPolicyBaseState<TReturn> ReturnAsync(Func<Task<TReturn>> returnObjectFactory);
        
        IPolicyBaseState<TReturn> ReturnDefault();
    }
}