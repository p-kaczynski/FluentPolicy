using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FluentPolicy
{
    internal abstract class PredicatedBehaviour<TReturn>
    {
        public Guid Id { get; }
        public abstract void SetSimpleBehaviour(Func<TReturn> factory);
        public abstract void SetSimpleBehaviourAsync(Func<Task<TReturn>> factory);

        protected PredicatedBehaviour()
        {
            Id = Guid.NewGuid();
        } 
    }

    internal class PredicatedBehaviour<TPredicate, TReturn> : PredicatedBehaviour<TReturn>
    {
        private readonly List<Func<TPredicate, bool>> _predicates = new List<Func<TPredicate, bool>>();

        private Func<TPredicate, Guid, TReturn> _behaviour;
        private Func<TPredicate, Guid, Task<TReturn>> _behaviourAsync;

        internal Func<TPredicate, TReturn> Behaviour
        {
            set { _behaviour = (input, guid) => value(input); }
        }

        internal Func<TPredicate, Task<TReturn>> BehaviourAsync
        {
            set { _behaviourAsync = (input, guid) => value(input); }
        }

        internal void AddPredicate(Func<TPredicate, bool> predicate)
        {
            _predicates.Add(predicate);
        }

        internal void CopyPredicatesTo(PredicatedBehaviour<TPredicate, TReturn> target)
        {
            foreach(var predicate in _predicates)
                target.AddPredicate(predicate);
        }

        internal bool Test(TPredicate value)
        {
            return _predicates.Any(predicate => predicate(value));
        }

        public override void SetSimpleBehaviour(Func<TReturn> factory)
        {
            _behaviour = (predicate, guid) => factory();
        }

        public override void SetSimpleBehaviourAsync(Func<Task<TReturn>> factory)
        {
            _behaviourAsync = (predicate, guid) => factory();
        }

        public void SetGuidBehaviour(Func<TPredicate, Guid, TReturn> behaviour)
        {
            _behaviour = behaviour;
        }

        public TReturn Call(TPredicate input)
        {
            return _behaviour(input, Id);
        }

        public Task<TReturn> CallAsync(TPredicate input)
        {
            if (_behaviourAsync != null)
                return _behaviourAsync(input, Id);
            return Task.FromResult(_behaviour(input, Id));
        }
    }
}