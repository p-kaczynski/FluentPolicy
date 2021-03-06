﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using FluentPolicy.Exceptions;
using JetBrains.Annotations;

namespace FluentPolicy
{
    public abstract class PolicyBuilder
    {
        public Guid Id { get; }

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

        [PublicAPI]
        public PolicyBuilder()
        {
            
        }

        [PublicAPI]
        public IPolicyBaseState<TReturn> Setup()
        {
            return this;
        }

        [PublicAPI]
        public PolicyBuilder(Func<TReturn> func)
        {
            _func = func;
        }

        [PublicAPI]
        public PolicyBuilder(Func<Task<TReturn>> func)
        {
            _funcAsync = func;
            _func =
                () => throw new AsyncPolicyException(
                    "This policy was created from an async function, and must be executed using ExecuteAsync");
        }

        [PublicAPI]
        public IPolicyBaseState<TReturn> AddModule(IPolicyModule module)
        {
            _modules.Add(module);
            module.RegisterEvents(this);
            
            return this;
        }

        TReturn IPolicyBaseState<TReturn>.Execute()
        {
            if(_func == null)
                throw new ImplementationNotSetException();

            return ((IPolicyBaseState<TReturn>) this).Execute(_func);
        }

        TReturn IPolicyBaseState<TReturn>.Execute(Func<TReturn> implementation ){
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
                        result = implementation();
                    }
                    catch (AsyncPolicyException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // Find behaviour
                        var exceptionBehaviour = GetExceptionBehaviour(b => b.Test(ex));

                        ExceptionThrown?.Invoke(this,
    new ExceptionThrownEventArgs
    {
        Exception = ex,
        HandlerBehaviourGuid =
            exceptionBehaviour?.Id
    });

                        if (exceptionBehaviour == null) throw;

                        // apply
                        return exceptionBehaviour.Call(ex);
                    }

                    // Find behaviour
                    var resultBehaviour = _resultBehaviourStack.ToArray().Reverse().FirstOrDefault(b => b.Test(result));

                    // Result obtained!
                    ReturnValueObtained?.Invoke(resultBehaviour, result);

                    // Call modules
                    foreach (var module in _modules)
                        module.AfterCall(this);

                    // If behaviour found, apply, then return result
                    return resultBehaviour == null ? result : resultBehaviour.Call(result);
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
        Task<TReturn> IPolicyBaseState<TReturn>.ExecuteAsync()
        {
            if(_funcAsync == null)
                throw new ImplementationNotSetException();

            return ((IPolicyBaseState<TReturn>) this).ExecuteAsync(_funcAsync);
        }

        async Task<TReturn> IPolicyBaseState<TReturn>.ExecuteAsync(Func<Task<TReturn>> implementation)
        {
            _repeatHelper.Reset();
            while (true)
            {
                try
                {
                    foreach (var module in _modules)
                        module.BeforeCall(this);

                    TReturn result = default;

                    Task<TReturn> exceptionReturn = null;
                    try
                    {
                        result = await implementation();
                    }
                    catch (Exception ex)
                    {
                        // Find behaviour
                        var exceptionBehaviour = GetExceptionBehaviour(b => b.Test(ex));

                        ExceptionThrown?.Invoke(this,
    new ExceptionThrownEventArgs
    {
        Exception = ex,
        HandlerBehaviourGuid =
            exceptionBehaviour?.Id
    });

                        if (exceptionBehaviour == null) throw;

                        // apply
                        exceptionReturn = exceptionBehaviour.CallAsync(ex);
                    }

                    // This is in outer scope do to inability to await in catch clause
                    if (exceptionReturn != null)
                        return await exceptionReturn;

                    // Find behaviour
                    var resultBehaviour = _resultBehaviourStack.ToArray().Reverse().FirstOrDefault(b => b.Test(result));

                    // Result obtained!
                    ReturnValueObtained?.Invoke(resultBehaviour, result);

                    // Call modules
                    foreach (var module in _modules)
                        module.AfterCall(this);

                    // If behaviour found, apply, then return result
                    return resultBehaviour == null ? result : await resultBehaviour.CallAsync(result);
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
            behaviour.AddPredicate(e => e is TException exception && predicate(exception));

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
            _exceptionBehaviourStack.Peek().AddPredicate(e => e is TException exception && predicate(exception));

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
            return _lastCreatedBehaviour?.Id ?? Guid.Empty;
        }

        IPolicyBaseState<TReturn> IPolicyConfigExpression<TReturn>.Return(TReturn returnObject)
        {
            _lastCreatedBehaviour.SetSimpleBehaviour(() => returnObject);

            return this;
        }

        public IPolicyBaseState<TReturn> Return(Func<TReturn> returnObjectFactory)
        {
            _lastCreatedBehaviour.SetSimpleBehaviour(returnObjectFactory);

            return this;
        }

        public IPolicyBaseState<TReturn> ReturnAsync(Task<TReturn> returnObject)
        {
            _lastCreatedBehaviour.SetSimpleBehaviourAsync(()=>returnObject);

            return this;
        }

        public IPolicyBaseState<TReturn> ReturnAsync(Func<Task<TReturn>> returnObjectFactory)
        {
            _lastCreatedBehaviour.SetSimpleBehaviourAsync(returnObjectFactory);

            return this;
        }

        public IPolicyBaseState<TReturn> ReturnDefault()
        {
            _lastCreatedBehaviour.SetSimpleBehaviour(()=>default);

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
            _exceptionBehaviourStack.Peek().Behaviour = e => throw factory(e);

            return this;
        }

        IPolicyBaseState<TReturn> IPolicyReturnValueConfigExpression<TReturn>.Return(Func<TReturn, TReturn> factory)
        {
            _resultBehaviourStack.Peek().Behaviour = factory;

            return this;
        }

        IPolicyBaseState<TReturn> IPolicyReturnValueConfigExpression<TReturn>.Throw(Func<TReturn, Exception> factory)
        {
            _resultBehaviourStack.Peek().Behaviour = r => throw factory(r);

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

        /// <exception cref="FailureLimitExceededException">Failure limit exceeded.</exception>
        /// <exception cref="WaitAndRetry">Handled internally.</exception>
        [AssertionMethod]
        public T Handle<T>(Guid behaviourId, Exception ex, int maximumRetries, Func<int, TimeSpan> getWaitTime, Action<Exception, int> callback)
        {
            var retry = _retryDictionary.AddOrUpdate(behaviourId, 1, (id, count) => count + 1);

            if (retry > maximumRetries)
                throw new FailureLimitExceededException(ex, behaviourId);

            callback(ex, retry);
            var waitTime = getWaitTime(retry);
            throw new WaitAndRetry(waitTime);
        }
    }
}
