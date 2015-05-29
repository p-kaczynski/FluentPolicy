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
        private readonly List<Func<TPredicate, bool>> _predicates = new List<Func<TPredicate, bool>>();
        public Func<TPredicate, TReturn> Behaviour { get; set; }

        public void AddPredicate(Func<TPredicate, bool> predicate)
        {
            _predicates.Add(predicate);
        }

        public bool Test(TPredicate value)
        {
            return _predicates.Any(predicate => predicate(value));
        }

        public override void SetSimpleBehaviour(Func<TReturn> factory)
        {
            Behaviour = predicate => factory();
        }
    }
}