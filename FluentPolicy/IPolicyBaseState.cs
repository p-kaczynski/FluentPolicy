using System.Threading.Tasks;

namespace FluentPolicy
{
    public interface IPolicyBaseState<TReturn>
    {
        IPolicyConditionSelector<TReturn> For();
        TReturn Execute();
        Task<TReturn> ExecuteAsync();
    }
}