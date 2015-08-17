using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace FluentPolicy
{
    public abstract class PolicyBuilder
    {
        public Guid Id { get; private set; }

        protected PolicyBuilder()
        {
            Id = Guid.NewGuid();
        }
    }

    public class PolicyBuilder<TReturn> : PolicyBuilder, IPolicyBaseState<TReturn>, IPolicyConditionSelector<TReturn>,
        IPolicyExceptionConfigExpression<TReturn>, IPolicyReturnValueConfigExpression<TReturn>, IPolicyRepeatConfigExpressionContinuation<TReturn>, IPolicyEvents<TReturn>
    {
        private readonly Func<TReturn> _func;
        private readonly Func<Task<TReturn>> _funcAsync;

        private readonly ICollection<IPolicyModule> _modules = new List<IPolicyModule>(); 

        private readonly Stack<PredicatedBehaviour<Exception, TReturn>> _exceptionBehaviourStack =
            new Stack<PredicatedBehaviour<Exception, TReturn>>();

        private readonly Stack<PredicatedBehaviour<TReturn, TReturn>> _resultBehaviourStack =
            new Stack<PredicatedBehaviour<TReturn, TReturn>>();

        private PredicatedBehaviour<TReturn> _lastCreatedBehaviour;

        private readonly RepeatHelper _repeatHelper = new RepeatHelper();



        public PolicyBuilder(Func<TReturn> func)
        {
            _func = func;
        }

        public PolicyBuilder(Func<Task<TReturn>> func)
        {
            _funcAsync = func;
            _func =
                () =>
                {
                    throw new AsyncPolicyException(
                        "This policy was created from an async function, and must be executed using ExecuteAsync");
                };
        }

        public IPolicyBaseState<TReturn> AddModule(IPolicyModule module)
        {
            _modules.Add(module);
            module.RegisterEvents(this);
            
            return this;
        }

        TReturn IPolicyBaseState<TReturn>.Execute()
        {
            _repeatHelper.Reset();
            while (true)
            {
                try
                {
                    foreach(var module in _modules)
                        module.BeforeCall(this);

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
                        // Find behaviour
                        var exceptionBehaviour = GetExceptionBehaviour(b => b.Test(ex));

                        if (ExceptionThrown != null)
                            ExceptionThrown(this,
                                new ExceptionThrownEventArgs
                                {
                                    Exception = ex,
                                    HandlerBehaviourGuid =
                                        exceptionBehaviour != null ? exceptionBehaviour.Id : (Guid?) null
                                });

                        if (exceptionBehaviour == null) throw;

                        // apply
                        return exceptionBehaviour.Call(ex);
                    }
                    return PostCall(result);
                }
                catch (WaitAndRetry waitAndRetry)
                {
                    HandleWaitAndRetry(waitAndRetry);
                }
                catch (FailureLimitExceededException flee)
                {
                    return HandleFailureLimitExceeded(flee);
                }
            }
        }

        // TODO: somehow remove redundancy between nonasync and async
        async Task<TReturn> IPolicyBaseState<TReturn>.ExecuteAsync()
        {
            _repeatHelper.Reset();
            while (true)
            {
                try
                {
                    foreach (var module in _modules)
                        module.BeforeCall(this);

                    TReturn result;
                    try
                    {
                        result = await _funcAsync();
                    }
                    catch (Exception ex)
                    {
                        // Find behaviour
                        var exceptionBehaviour = GetExceptionBehaviour(b => b.Test(ex));

                        if (ExceptionThrown != null)
                            ExceptionThrown(this,
                                new ExceptionThrownEventArgs
                                {
                                    Exception = ex,
                                    HandlerBehaviourGuid =
                                        exceptionBehaviour != null ? exceptionBehaviour.Id : (Guid?)null
                                });

                        if (exceptionBehaviour == null) throw;

                        // apply
                        return exceptionBehaviour.Call(ex);
                    }
                    return PostCall(result);
                }
                catch (WaitAndRetry waitAndRetry)
                {
                    HandleWaitAndRetry(waitAndRetry);
                }
                catch (FailureLimitExceededException flee)
                {
                    return HandleFailureLimitExceeded(flee);
                }
            }
        }

        private TReturn PostCall(TReturn result)
        {
            // Find behaviour
            var resultBehaviour = _resultBehaviourStack.ToArray().Reverse().FirstOrDefault(b => b.Test(result));

            // Result obtained!
            if (ReturnValueObtained != null)
                ReturnValueObtained(resultBehaviour, result);

            // Call modules
            foreach (var module in _modules)
                module.AfterCall(this);

            // If behaviour found, apply, then return result
            return resultBehaviour == null ? result : resultBehaviour.Call(result);
        }

        private void HandleWaitAndRetry(WaitAndRetry waitAndRetry)
        {
            var sleepTime = waitAndRetry.WaitTime;
            if (sleepTime.TotalSeconds > 0)
                Thread.Sleep(sleepTime);
        }


        private TReturn HandleFailureLimitExceeded(FailureLimitExceededException flee)
        {
            var ex = flee.InnerException;
            var exceptionBehaviour = GetExceptionBehaviour(b => b.Test(ex) && b.Id != flee.FailedBehaviourId);
            return exceptionBehaviour.Call(ex);
        }

        private PredicatedBehaviour<Exception, TReturn> GetExceptionBehaviour(Func<PredicatedBehaviour<Exception, TReturn>, bool> predicate)
        {
            return _exceptionBehaviourStack.ToArray().Reverse().FirstOrDefault(predicate);
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

        IPolicyBaseState<TReturn> IPolicyExceptionConfigExpression<TReturn>.Rethrow()
        {
            _exceptionBehaviourStack.Peek().Behaviour = e =>
            {
                ExceptionDispatchInfo.Capture(e).Throw();

                throw new Exception("ExceptionDispatchInfo.Throw failed to break execution!");
            };

            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        Guid IPolicyConfigExpression<TReturn>.GetGuid()
        {
            return _lastCreatedBehaviour != null ? _lastCreatedBehaviour.Id : Guid.Empty;
        }

        IPolicyBaseState<TReturn> IPolicyConfigExpression<TReturn>.Return(TReturn returnObject)
        {
            _lastCreatedBehaviour.SetSimpleBehaviour(() => returnObject);

            return this;
        }

        public IPolicyBaseState<TReturn> ReturnDefault()
        {
            _lastCreatedBehaviour.SetSimpleBehaviour(()=>default(TReturn));

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

        IPolicyRepeatConfigExpressionContinuation<TReturn> IPolicyExceptionConfigExpression<TReturn>.Repeat()
        {
            return ((IPolicyExceptionConfigExpression<TReturn>) this).Repeat(1, (e, i) => { });
        }

        IPolicyRepeatConfigExpressionContinuation<TReturn> IPolicyExceptionConfigExpression<TReturn>.Repeat(Action<Exception> callback)
        {
            return ((IPolicyExceptionConfigExpression<TReturn>) this).Repeat(1, (e, i) => callback(e));
        }

        IPolicyRepeatConfigExpressionContinuation<TReturn> IPolicyExceptionConfigExpression<TReturn>.Repeat(int numberOfTimes)
        {
            return ((IPolicyExceptionConfigExpression<TReturn>)this).Repeat(numberOfTimes, (e, i) => { });
        }

        IPolicyRepeatConfigExpressionContinuation<TReturn> IPolicyExceptionConfigExpression<TReturn>.Repeat(int numberOfTimes, Action<Exception, int> callback)
        {
            _exceptionBehaviourStack.Peek()
                .SetGuidBehaviour(
                    (exception, guid) => _repeatHelper.Handle<TReturn>(guid, exception, numberOfTimes, callback));

            return this;
        }

        IPolicyRepeatConfigExpressionContinuation<TReturn> IPolicyExceptionConfigExpression<TReturn>.WaitAndRepeat(TimeSpan[] waitTimes)
        {
            return ((IPolicyExceptionConfigExpression<TReturn>) this).WaitAndRepeat(waitTimes, (e, i) => { });
        }

        IPolicyRepeatConfigExpressionContinuation<TReturn> IPolicyExceptionConfigExpression<TReturn>.WaitAndRepeat(TimeSpan[] waitTimes, Action<Exception, int> callback)
        {
            _exceptionBehaviourStack.Peek()
                .SetGuidBehaviour(
                    (exception, guid) =>
                        _repeatHelper.Handle<TReturn>(guid, exception, waitTimes.Length, waitTimes, callback));

            return this;
        }

        IPolicyRepeatConfigExpressionContinuation<TReturn> IPolicyExceptionConfigExpression<TReturn>.WaitAndRepeat(int numberOfTimes, Func<int, TimeSpan> getWaitTime)
        {
            return ((IPolicyExceptionConfigExpression<TReturn>) this).WaitAndRepeat(numberOfTimes, getWaitTime,
                (e, i) => { });
        }

        IPolicyRepeatConfigExpressionContinuation<TReturn> IPolicyExceptionConfigExpression<TReturn>.WaitAndRepeat(
            int numberOfTimes, Func<int, TimeSpan> getWaitTime, Action<Exception, int> callback)
        {
            _exceptionBehaviourStack.Peek()
                .SetGuidBehaviour(
                    (exception, guid) =>
                        _repeatHelper.Handle<TReturn>(guid, exception, numberOfTimes, getWaitTime, callback));

            return this;
        }

        IPolicyExceptionConfigExpression<TReturn> IPolicyRepeatConfigExpressionContinuation<TReturn>.Then()
        {
            var behaviour = new PredicatedBehaviour<Exception, TReturn>();
            _exceptionBehaviourStack.Peek().CopyPredicatesTo(behaviour);
            
            _lastCreatedBehaviour = behaviour;
            _exceptionBehaviourStack.Push(behaviour); 
            
            return this;
        }

        /// <summary>
        /// Event fired when exception is thrown from the underlying expression. The object passed is the exception, and the sender is behaviour that will handle it.
        /// </summary>
        public event EventHandler<ExceptionThrownEventArgs> ExceptionThrown;

        /// <summary>
        /// Event fired when exception is thrown from the underlying expression. The object passed is the exception, and the sender is behaviour that will handle it.
        /// </summary>
        public event EventHandler<TReturn> ReturnValueObtained;
    }

    internal class RepeatHelper
    {
        public void Reset()
        {
            _retryDictionary.Clear();
        }
        private readonly ConcurrentDictionary<Guid,int> _retryDictionary = new ConcurrentDictionary<Guid,int>();

        private static readonly TimeSpan[] ZeroTimeSpanArray = {TimeSpan.FromSeconds(0)};
        public T Handle<T>(Guid behaviourId, Exception ex, int maximumRetries,  Action<Exception, int> callback)
        {
            return Handle<T>(behaviourId, ex, maximumRetries, ZeroTimeSpanArray, callback);
        }

        public T Handle<T>(Guid behaviourId, Exception ex, int maximumRetries, TimeSpan[] waitTimes, Action<Exception, int> callback)
        {
            return Handle<T>(behaviourId, ex, maximumRetries,
                retry => retry > waitTimes.Length ? waitTimes.Last() : waitTimes[retry - 1], callback);
        }

        public T Handle<T>(Guid behaviourId, Exception ex, int maximumRetries, Func<int, TimeSpan> getWaitTime, Action<Exception, int> callback)
        {
            var retry = _retryDictionary.AddOrUpdate(behaviourId, 1, (id, count) => count + 1);

            if (retry > maximumRetries)
                throw new FailureLimitExceededException(ex, behaviourId);

            callback(ex, retry);
            var waitTime = getWaitTime(retry);
            throw new WaitAndRetry(waitTime);

#pragma warning disable 162
            // ReSharper disable once HeuristicUnreachableCode
            return default(T); // for compiler :)
#pragma warning restore 162
        }
    }
}
