using System.Threading.Tasks;

namespace FluentPolicy
{
    public interface IPolicyBaseState<TReturn>
    {
        IPolicyConditionSelector<TReturn> For();
        ILoggingExpression<TReturn> Log();
        IPolicyBaseState<TReturn> AddModule(IPolicyModule module);
        TReturn Execute();
        Task<TReturn> ExecuteAsync();
    }
}