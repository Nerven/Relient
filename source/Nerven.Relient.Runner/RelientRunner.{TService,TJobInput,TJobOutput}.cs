using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Nerven.Assertion;

namespace Nerven.Relient.Runner
{
    [PublicAPI]
    public class RelientRunner<TService, TJobInput, TJobOutput>
        where TService : IRelientService<TJobInput, TJobOutput>
    {
        private readonly _RelientRunnerContext<TService, TJobInput, TJobOutput> _Context;
        private readonly object _Lock;
        private TaskCompletionSource<int> _RunTaskSource;
        private CancellationTokenRegistration _CancellationRegistration;
        private CancellationTokenSource _CancellationTokenSource;
        private Task[] _HookTasks;

        internal RelientRunner(_RelientRunnerContext<TService, TJobInput, TJobOutput> context)
        {
            _Context = context;
            _Lock = new object();
            Mode = RelientRunnerMode.None;
        }

        public RelientRunnerMode Mode { get; private set; }
        
        public TService Service => _Context.Service;

        public Task<int> RunAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _RunAsync(cancellationToken)
                .ContinueWith(_runTask =>
                    {
                        lock (_Lock)
                        {
                            Mode = RelientRunnerMode.None;
                            _RunTaskSource = null;
                            _CancellationRegistration.Dispose();
                            _CancellationTokenSource?.Dispose();
                        }

                        return Task
                            .WhenAll(_HookTasks)
                            .ContinueWith(_hookTasks => _runTask, TaskContinuationOptions.OnlyOnRanToCompletion)
                            .Unwrap();
                    },
                    TaskContinuationOptions.None)
                .Unwrap();
        }

        public int Run(CancellationToken cancellationToken = default(CancellationToken))
        {
            return RunAsync(cancellationToken).Result;
        }

        private Task<int> _RunAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            lock (_Lock)
            {
                Must.Assertion
                    .Assert(Mode == RelientRunnerMode.None)
                    .Assert(_RunTaskSource == null);
                
                _RunTaskSource = new TaskCompletionSource<int>();
                _CancellationTokenSource = new CancellationTokenSource();

                _CancellationRegistration = cancellationToken.Register(() =>
                    {
                        _CancellationTokenSource.Cancel();
                        _RunTaskSource.SetCanceled();
                    });

                _HookTasks = _Context.ServiceHooks.Select(_hookAsync => _hookAsync(_Context.Service, _CancellationTokenSource.Token)).ToArray();
                
                Mode = RelientRunnerMode.Cli;
                return _ChainTaskToCompletionSource(_RunCli(_CancellationTokenSource), _RunTaskSource);
            }
        }

        private Task<int> _RunCli(CancellationTokenSource cancellationTokenSource)
        {
            Console.CancelKeyPress += (_sender, _args) => cancellationTokenSource.Cancel();

            return _Context.Service
                .RunAsync(cancellationTokenSource.Token)
                .ContinueWith(_task => 0, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private static Task<T> _ChainTaskToCompletionSource<T>(Task<T> task, TaskCompletionSource<T> target)
        {
            if (task.IsCompleted)
            {
                target.SetResult(task.Result);
            }
            else if (task.IsCanceled)
            {
                target.SetCanceled();
            }
            else if (task.IsFaulted)
            {
                target.SetException(task.Exception);
            }
            else
            {
                task.ContinueWith(_completedTask => _ChainTaskToCompletionSource(_completedTask, target));
            }

            return target.Task;
        }
    }
}
