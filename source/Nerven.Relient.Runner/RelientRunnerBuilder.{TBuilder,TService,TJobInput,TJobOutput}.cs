using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Relient.Runner
{
    [PublicAPI]
    public class RelientRunnerBuilder<TBuilder, TService, TJobInput, TJobOutput>
        where TBuilder : IRelientBuilder<TBuilder, TService, TJobInput, TJobOutput>
        where TService : IRelientService<TJobInput, TJobOutput>
    {
        private readonly ConcurrentBag<Func<TService, CancellationToken, Task>> _ServiceHooks;

        public RelientRunnerBuilder(TBuilder builder)
        {
            Builder = builder;
            _ServiceHooks = new ConcurrentBag<Func<TService, CancellationToken, Task>>();
        }

        public TBuilder Builder { get; }

        public string ServiceName { get; private set; }

        public RelientRunnerBuilder<TBuilder, TService, TJobInput, TJobOutput> WithServiceName(string serviceName)
        {
            ServiceName = serviceName;

            return this;
        }

        public RelientRunnerBuilder<TBuilder, TService, TJobInput, TJobOutput> WithServiceHook(Func<TService, CancellationToken, Task> hookAsync)
        {
            _ServiceHooks.Add(hookAsync);

            return this;
        }

        internal async Task<_RelientRunnerContext<TService, TJobInput, TJobOutput>> _BuildAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var _service = await Builder.BuildAsync(cancellationToken).ConfigureAwait(false);

            return new _RelientRunnerContext<TService, TJobInput, TJobOutput>(_service, ServiceName ?? _service.GetType().Name, _ServiceHooks.ToArray());
        }
    }
}
