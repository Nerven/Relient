using JetBrains.Annotations;
using Nerven.Taskuler;

namespace Nerven.Relient.TaskulerService
{
    [PublicAPI]
    public static class RelientTaskulerBuilderExtensions
    {
        public static TBuilder WithOwnedWorker<TBuilder, TJobInput, TJobOutput>(
            this IRelientTaskulerBuilder<TBuilder, TJobInput, TJobOutput> builder, 
            ITaskulerWorker worker)
            where TBuilder : IRelientTaskulerBuilder<TBuilder, TJobInput, TJobOutput> => builder.WithOwnedWorker(() => worker);

        public static TBuilder WithBorrowedWorker<TBuilder, TJobInput, TJobOutput>(
            this IRelientTaskulerBuilder<TBuilder, TJobInput, TJobOutput> builder, 
            ITaskulerWorker worker)
            where TBuilder : IRelientTaskulerBuilder<TBuilder, TJobInput, TJobOutput> => builder.WithBorrowedWorker(() => worker);
    }
}
