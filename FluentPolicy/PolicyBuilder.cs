using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace FluentPolicy
{
    public class PolicyBuilder<TReturn> : IPolicyBaseState<TReturn>, IPolicyConditionSelector<TReturn>,
        IPolicyExceptionConfigExpression<TReturn>, IPolicyReturnValueConfigExpression<TReturn>
    {
        private readonly Func<TReturn> _func;
        private readonly Task<TReturn> _task;

        private readonly Stack<PredicatedBehaviour<Exception, TReturn>> _exceptionBehaviourStack =
            new Stack<PredicatedBehaviour<Exception, TReturn>>();

        private readonly Stack<PredicatedBehaviour<TReturn, TReturn>> _resultBehaviourStack =
            new Stack<PredicatedBehaviour<TReturn, TReturn>>();

        private PredicatedBehaviour<TReturn> _lastCreatedBehaviour;

        public PolicyBuilder(Func<TReturn> func)
        {
            _func = func;
        }

        public PolicyBuilder(Task<TReturn> task)
        {
            _task = task;
            _func =
                () =>
                {
                    throw new AsyncPolicyException(
                        "This policy was created from a task, and must be executed using ExecuteAsync");
                };
        }

        TReturn IPolicyBaseState<TReturn>.Execute()
        {
            while (true)
            {
                try
                {
                    TReturn result;
                    try
                    {
                        result = _func();
                    }
                    catch (AsyncPolicyException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        var exceptionBehaviour = _exceptionBehaviourStack.ToArray().Reverse().FirstOrDefault(b => b.Test(ex));
                        if (exceptionBehaviour == null) throw new NoMatchingPolicyException(ex);
                        return exceptionBehaviour.Behaviour(ex);
                    }
                    var resultBehaviour = _resultBehaviourStack.ToArray().Reverse().FirstOrDefault(b => b.Test(result));
                    return resultBehaviour == null ? result : resultBehaviour.Behaviour(result);
                }
                catch (WaitAndRetry waitAndRetry)
                {
                    Thread.Sleep(waitAndRetry.WaitTime);
                }
                catch (FailureLimitExceededException flee)
                {

                }
            }
        }

        // TODO: somehow remove redundancy between nonasync and async
        async Task<TReturn> IPolicyBaseState<TReturn>.ExecuteAsync()
        {
            while (true)
            {
                try
                {
                    TReturn result;
                    try
                    {
                        result = await _task;
                    }
                    catch (Exception ex)
                    {
                        var exceptionBehaviour = _exceptionBehaviourStack.ToArray().Reverse().FirstOrDefault(b => b.Test(ex));
                        if (exceptionBehaviour == null) throw new NoMatchingPolicyException(ex);
                        return exceptionBehaviour.Behaviour(ex);
                    }
                    var resultBehaviour = _resultBehaviourStack.ToArray().Reverse().FirstOrDefault(b => b.Test(result));
                    return resultBehaviour == null ? result : resultBehaviour.Behaviour(result);
                }
                catch (WaitAndRetry waitAndRetry)
                {
                    Thread.Sleep(waitAndRetry.WaitTime);
                }
                catch (FailureLimitExceededException flee)
                {

                }
            }
        }

        IPolicyConditionSelector<TReturn> IPolicyBaseState<TReturn>.For()
        {
            return this;
        }

        IPolicyExceptionConfigExpression<TReturn> IPolicyConditionSelector<TReturn>.Exception<TException>()
        {
            var behaviour = new PredicatedBehaviour<Exception, TReturn>();
            _lastCreatedBehaviour = behaviour;
            behaviour.AddPredicate(e => e is TException);

            _exceptionBehaviourStack.Push(behaviour);
            return this;
        }

        IPolicyExceptionConfigExpression<TReturn> IPolicyConditionSelector<TReturn>.Exception<TException>(
            Func<TException, bool> predicate)
        {
            var behaviour = new PredicatedBehaviour<Exception, TReturn>();
            _lastCreatedBehaviour = behaviour;
            behaviour.AddPredicate(e => e is TException && predicate((TException) e));

            _exceptionBehaviourStack.Push(behaviour);
            return this;
        }

        IPolicyExceptionConfigExpression<TReturn> IPolicyExceptionConfigExpression<TReturn>.Or<TException>()
        {
            _exceptionBehaviourStack.Peek().AddPredicate(e => e is TException);

            return this;
        }

        IPolicyExceptionConfigExpression<TReturn> IPolicyExceptionConfigExpression<TReturn>.Or<TException>(
            Func<TException, bool> predicate)
        {
            _exceptionBehaviourStack.Peek().AddPredicate(e => e is TException && predicate((TException) e));

            return this;
        }

        IPolicyRepeatConfigExpression<TReturn> IPolicyConfigExpression<TReturn>.Repeat()
        {
            throw new NotImplementedException();
        }

        IPolicyBaseState<TReturn> IPolicyExceptionConfigExpression<TReturn>.Rethrow()
        {
            _exceptionBehaviourStack.Peek().Behaviour = e =>
            {
                ExceptionDispatchInfo.Capture(e).Throw();

                throw new Exception("ExceptionDispatchInfo.Throw failed to break execution!");
            };

            return this;
        }

        IPolicyBaseState<TReturn> IPolicyConfigExpression<TReturn>.Return(TReturn returnObject)
        {
            _lastCreatedBehaviour.SetSimpleBehaviour(() => returnObject);

            return this;
        }

        IPolicyBaseState<TReturn> IPolicyExceptionConfigExpression<TReturn>.Return(Func<Exception, TReturn> factory)
        {
            _exceptionBehaviourStack.Peek().Behaviour = factory;

            return this;
        }

        IPolicyReturnValueConfigExpression<TReturn> IPolicyConditionSelector<TReturn>.ReturnValue(
            Func<TReturn, bool> predicate)
        {
            var behaviour = new PredicatedBehaviour<TReturn, TReturn>();
            _lastCreatedBehaviour = behaviour;
            behaviour.AddPredicate(predicate);

            _resultBehaviourStack.Push(behaviour);
            return this;
        }

        IPolicyBaseState<TReturn> IPolicyExceptionConfigExpression<TReturn>.Throw(Func<Exception, Exception> factory)
        {
            _exceptionBehaviourStack.Peek().Behaviour = e => { throw factory(e); };

            return this;
        }

        IPolicyBaseState<TReturn> IPolicyReturnValueConfigExpression<TReturn>.Return(Func<TReturn, TReturn> factory)
        {
            _resultBehaviourStack.Peek().Behaviour = factory;

            return this;
        }

        IPolicyBaseState<TReturn> IPolicyReturnValueConfigExpression<TReturn>.Throw(Func<TReturn, Exception> factory)
        {
            _resultBehaviourStack.Peek().Behaviour = r => { throw factory(r); };

            return this;
        }

        IPolicyExceptionConfigExpression<TReturn> IPolicyConditionSelector<TReturn>.AllOtherExceptions()
        {
            var behaviour = new PredicatedBehaviour<Exception, TReturn>();
            _lastCreatedBehaviour = behaviour;
            behaviour.AddPredicate(e => true);

            _exceptionBehaviourStack.Push(behaviour);
            return this;
        }

        IPolicyReturnValueConfigExpression<TReturn> IPolicyConditionSelector<TReturn>.AllOtherValues()
        {
            var behaviour = new PredicatedBehaviour<TReturn, TReturn>();
            _lastCreatedBehaviour = behaviour;
            behaviour.AddPredicate(rv => true);

            _resultBehaviourStack.Push(behaviour);
            return this;
        }
    }
}
