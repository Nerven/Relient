using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Relient
{
    [PublicAPI]
    public sealed class RelientJobInstance<TJobInput, TJobOutput> : IRelientJobInstance<TJobInput, TJobOutput>
    {
        private readonly Func<IRelientJobContext, TJobInput, CancellationToken, Task<TJobOutput>> _RunAsync;

        internal RelientJobInstance(
            TimeSpan? interval,
            TimeSpan? timeout,
            Func<IRelientJobContext, TJobInput, CancellationToken, Task<TJobOutput>> runAsync)
        {
            _RunAsync = runAsync;
            Interval = interval;
            Timeout = timeout;
        }

        public TimeSpan? Interval { get; }

        public TimeSpan? Timeout { get; }

        public Task<TJobOutput> RunAsync(
            IRelientJobContext context,
            TJobInput input, 
            CancellationToken cancellationToken = new CancellationToken()) => _RunAsync(context, input, cancellationToken);
    }
}
