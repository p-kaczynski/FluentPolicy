namespace FluentPolicy.Modules
{
    public abstract class BaseModule : IPolicyModule
    {
        public virtual void RegisterEvents<TReturn>(IPolicyEvents<TReturn> eventsSource)
        {
        }

        public virtual void BeforeCall(PolicyBuilder policy)
        {
        }

        public virtual void AfterCall(PolicyBuilder policy)
        {
        }
    }
}