using System;
using JetBrains.Annotations;
using Nerven.Taskuler;

namespace Nerven.Relient.TaskulerService
{
    [PublicAPI]
    public interface IRelientTaskulerBuilder<out TBuilder, TJobInput, TJobOutput> : IRelientBuilder<TBuilder, IRelientTaskulerService<TJobInput, TJobOutput>, TJobInput, TJobOutput>
        where TBuilder : IRelientTaskulerBuilder<TBuilder, TJobInput, TJobOutput>
    {
        TBuilder WithOwnedWorker(Func<ITaskulerWorker> createWorker);

        TBuilder WithBorrowedWorker(Func<ITaskulerWorker> getWorker);
    }
}
