using System.Threading.Tasks;

namespace FluentPolicy
{
    public interface IPolicyBaseState<TReturn>
    {
        IPolicyConditionSelector<TReturn> For();
        IPolicyBaseState<TReturn> AddModule(IPolicyModule module);
        TReturn Execute();
        Task<TReturn> ExecuteAsync();
    }
}