using System;
using System.ComponentModel;

namespace FluentPolicy
{
    public interface IPolicyConfigExpression<TReturn>
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        Guid GetGuid();

        IPolicyBaseState<TReturn> Return(TReturn returnObject);
        
        IPolicyBaseState<TReturn> ReturnDefault();
    }
}