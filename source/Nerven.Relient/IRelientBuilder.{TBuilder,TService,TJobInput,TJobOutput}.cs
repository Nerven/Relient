using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Relient
{
    [PublicAPI]
    public interface IRelientBuilder<out TBuilder, TService, TJobInput, TJobOutput>
        where TBuilder : IRelientBuilder<TBuilder, TService, TJobInput, TJobOutput>
        where TService : IRelientService<TJobInput, TJobOutput>
    {
        TBuilder AddJob(string name, IRelientJobInstance<TJobInput, TJobOutput> instance);

        TBuilder AddJob(string name, Func<IRelientJobContext, TJobInput, CancellationToken, Task<TJobOutput>> runAsync);

        TBuilder AddJob(string name, TimeSpan? interval, TimeSpan? timeout, Func<IRelientJobContext, TJobInput, CancellationToken, Task<TJobOutput>> runAsync);

        TBuilder AddJobs(IRelientJobProvider<TJobInput, TJobOutput> jobProvider);

        TBuilder AddJobs(IEnumerable<IRelientJobProvider<TJobInput, TJobOutput>> jobProvider);

        TBuilder WithInputConstructor(Func<RelientJobInfo<TJobInput, TJobOutput>, CancellationToken, Task<TJobInput>> createInput);

        Task<TService> BuildAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
