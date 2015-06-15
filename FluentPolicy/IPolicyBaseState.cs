using System.Threading.Tasks;

namespace FluentPolicy
{
    public interface IPolicyBaseState<TReturn>
    {
        IPolicyConditionSelector<TReturn> For();
        ILoggingExpression<TReturn> Log();
        TReturn Execute();
        Task<TReturn> ExecuteAsync();
    }
}