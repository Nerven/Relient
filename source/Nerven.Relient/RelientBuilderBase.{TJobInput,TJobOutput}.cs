using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Nerven.Assertion;
using Nerven.Assertion.Extensions;

namespace Nerven.Relient
{
    [PublicAPI]
    public abstract class RelientBuilderBase<TBuilder, TService, TJobInput, TJobOutput> : IRelientBuilder<TBuilder, TService, TJobInput, TJobOutput>
        where TBuilder : IRelientBuilder<TBuilder, TService, TJobInput, TJobOutput>
        where TService : IRelientService<TJobInput, TJobOutput>
    {
        private bool _Valid;

        protected RelientBuilderBase()
        {
            _Valid = true;
            Lock = new object();
            Jobs = new ConcurrentDictionary<string, IRelientJobInstance<TJobInput, TJobOutput>>(StringComparer.OrdinalIgnoreCase);
            JobProviders = new ConcurrentBag<IRelientJobProvider<TJobInput, TJobOutput>>();
        }

        protected object Lock { get; }

        protected ConcurrentDictionary<string, IRelientJobInstance<TJobInput, TJobOutput>> Jobs { get; }

        protected ConcurrentBag<IRelientJobProvider<TJobInput, TJobOutput>> JobProviders { get; }

        protected Func<RelientJobInfo<TJobInput, TJobOutput>, CancellationToken, Task<TJobInput>> CreateInputAsync { get; set; }

        protected abstract TBuilder Builder { get; }

        public virtual TBuilder AddJob(
            string name,
            IRelientJobInstance<TJobInput, TJobOutput> instance)
        {
            Must.Assertion
                .AssertArgumentNotNull(name, nameof(name))
                .AssertArgumentNotNull(instance, nameof(instance));

            return Mutate(() =>
                {
                    Must.Assertion
                        .Assert(Jobs.TryAdd(name, instance));
                });
        }

        public virtual TBuilder AddJob(
            string name,
            TimeSpan? interval,
            TimeSpan? timeout,
            Func<IRelientJobContext, TJobInput, CancellationToken, Task<TJobOutput>> runAsync)
        {
            Must.Assertion
                .AssertArgumentNotNull(name, nameof(name))
                .AssertArgumentNotNull(runAsync, nameof(runAsync));

            return AddJob(name, RelientJobInstance.Create(interval, timeout, runAsync));
        }

        public virtual TBuilder AddJob(
            string name,
            Func<IRelientJobContext, TJobInput, CancellationToken, Task<TJobOutput>> runAsync)
        {
            Must.Assertion
                .AssertArgumentNotNull(name, nameof(name))
                .AssertArgumentNotNull(runAsync, nameof(runAsync));

            return AddJob(name, RelientJobInstance.Create(null, null, runAsync));
        }

        public TBuilder AddJobs(IRelientJobProvider<TJobInput, TJobOutput> jobProvider)
        {
            Must.Assertion
                .AssertArgumentNotNull(jobProvider, nameof(jobProvider));

            return Mutate(() =>
                {
                    JobProviders.Add(jobProvider);
                });
        }

        public TBuilder AddJobs(IEnumerable<IRelientJobProvider<TJobInput, TJobOutput>> jobProviders)
        {
            // TODO: Avoid PossibleMultipleEnumeration with latest Assertion
            //// ReSharper disable once PossibleMultipleEnumeration
            Must.Assertion
                .AssertArgumentNotNull(jobProviders, nameof(jobProviders));

            //// ReSharper disable once PossibleMultipleEnumeration
            return Mutate(() =>
                {
                    foreach (var _jobProvider in jobProviders)
                    {
                        Must.Assertion
                            .Assert(_jobProvider != null);

                        JobProviders.Add(_jobProvider);
                    }
                });
        }

        public virtual TBuilder WithInputConstructor(Func<RelientJobInfo<TJobInput, TJobOutput>, CancellationToken, Task<TJobInput>> createInputAsync)
        {
            Must.Assertion
                .AssertArgumentNotNull(createInputAsync, nameof(createInputAsync));

            return Mutate(() => CreateInputAsync = createInputAsync);
        }

        public async Task<TService> BuildAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Mutate(() => _Valid = false);

            var _service = await CreateServiceAsync(cancellationToken).ConfigureAwait(false);

            Must.Assertion
                .Assert(_service != null);

            return _service;
        }

        protected TBuilder Mutate(Action action)
        {
            lock (Lock)
            {
                Must.Assertion.Assert(_Valid);

                try
                {
                    action();
                }
                catch
                {
                    _Valid = false;
                    throw;
                }
            }

            return Builder;
        }

        protected async Task<IReadOnlyList<KeyValuePair<string, IRelientJobInstance<TJobInput, TJobOutput>>>> CollectJobsAsync(CancellationToken cancellationToken)
        {
            Must.Assertion.Assert(!_Valid);

            var _jobInstances = Jobs.ToArray();
            var _jobProviders = JobProviders.ToArray();

            if (_jobProviders.Length == 0)
            {
                return _jobInstances;
            }

            var _jobInstancesFromProviders = await Task.WhenAll(_jobProviders.Select(_jobProvider => _jobProvider.GetJobInstancesAsync(cancellationToken))).ConfigureAwait(false);

            return _jobInstances.Concat(_jobInstancesFromProviders.SelectMany(_jobInstanceCollection => _jobInstanceCollection)).ToList();
        }

        protected abstract Task<TService> CreateServiceAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
