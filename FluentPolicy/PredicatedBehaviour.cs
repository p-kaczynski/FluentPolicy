using System;
using System.Collections.Generic;
using System.Linq;

namespace FluentPolicy
{
    internal abstract class PredicatedBehaviour<TReturn>
    {
        public abstract void SetSimpleBehaviour(Func<TReturn> factory);
    }

    internal class PredicatedBehaviour<TPredicate, TReturn> : PredicatedBehaviour<TReturn>
    {
        public Guid Id { get; private set; }

        public PredicatedBehaviour()
        {
            Id = Guid.NewGuid();
        } 

        private readonly List<Func<TPredicate, bool>> _predicates = new List<Func<TPredicate, bool>>();

        private Func<TPredicate, Guid, TReturn> _behaviour;

        public Func<TPredicate, TReturn> Behaviour
        {
            set { _behaviour = (input, guid) => value(input); }
        }

        public void AddPredicate(Func<TPredicate, bool> predicate)
        {
            _predicates.Add(predicate);
        }

        public void CopyPredicatesTo(PredicatedBehaviour<TPredicate, TReturn> target)
        {
            foreach(var predicate in _predicates)
                target.AddPredicate(predicate);
        }

        public bool Test(TPredicate value)
        {
            return _predicates.Any(predicate => predicate(value));
        }

        public override void SetSimpleBehaviour(Func<TReturn> factory)
        {
            _behaviour = (predicate, guid) => factory();
        }

        public void SetGuidBehaviour(Func<TPredicate, Guid, TReturn> behaviour)
        {
            _behaviour = behaviour;
        }

        public TReturn Call(TPredicate input)
        {
            return _behaviour(input, Id);
        }
    }
}