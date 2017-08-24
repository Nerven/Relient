using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Relient.Runner
{
    [PublicAPI]
    public static class RelientRunner
    {
        public static async Task<RelientRunner<TService, TJobInput, TJobOutput>> SetupAsync<TBuilder, TService, TJobInput, TJobOutput>(
            IRelientBuilder<TBuilder, TService, TJobInput, TJobOutput> builder,
            Action<RelientRunnerBuilder<TBuilder, TService, TJobInput, TJobOutput>> customize,
            CancellationToken cancellationToken = default(CancellationToken))
            where TBuilder : IRelientBuilder<TBuilder, TService, TJobInput, TJobOutput>
            where TService : IRelientService<TJobInput, TJobOutput>
        {
            var _builder = new RelientRunnerBuilder<TBuilder, TService, TJobInput, TJobOutput>((TBuilder)builder);

            customize?.Invoke(_builder);

            var _context = await _builder._BuildAsync(cancellationToken).ConfigureAwait(false);
            return new RelientRunner<TService, TJobInput, TJobOutput>(_context);
        }

        public static Task<RelientRunner<TService, TJobInput, TJobOutput>> SetupAsync<TBuilder, TService, TJobInput, TJobOutput>(
            IRelientBuilder<TBuilder, TService, TJobInput, TJobOutput> builder,
            CancellationToken cancellationToken = default(CancellationToken))
            where TBuilder : IRelientBuilder<TBuilder, TService, TJobInput, TJobOutput>
            where TService : IRelientService<TJobInput, TJobOutput>
        {
            return SetupAsync(builder, null, cancellationToken);
        }

        public static async Task<int> RunAsync<TBuilder, TService, TJobInput, TJobOutput>(
            IRelientBuilder<TBuilder, TService, TJobInput, TJobOutput> builder,
            Action<RelientRunnerBuilder<TBuilder, TService, TJobInput, TJobOutput>> customize,
            CancellationToken cancellationToken = default(CancellationToken))
            where TBuilder : IRelientBuilder<TBuilder, TService, TJobInput, TJobOutput>
            where TService : IRelientService<TJobInput, TJobOutput>
        {
            var _runner = await SetupAsync(builder, customize, cancellationToken).ConfigureAwait(false);
            return await _runner.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        public static Task<int> RunAsync<TBuilder, TService, TJobInput, TJobOutput>(
            IRelientBuilder<TBuilder, TService, TJobInput, TJobOutput> builder,
            CancellationToken cancellationToken = default(CancellationToken))
            where TBuilder : IRelientBuilder<TBuilder, TService, TJobInput, TJobOutput>
            where TService : IRelientService<TJobInput, TJobOutput>
        {
            return RunAsync(builder, null, cancellationToken);
        }

        public static int Run<TBuilder, TService, TJobInput, TJobOutput>(
            IRelientBuilder<TBuilder, TService, TJobInput, TJobOutput> builder,
            Action<RelientRunnerBuilder<TBuilder, TService, TJobInput, TJobOutput>> customize,
            CancellationToken cancellationToken = default(CancellationToken))
            where TBuilder : IRelientBuilder<TBuilder, TService, TJobInput, TJobOutput>
            where TService : IRelientService<TJobInput, TJobOutput>
        {
            var _runner = Task.Run(() => SetupAsync(builder, customize, cancellationToken), cancellationToken).Result;
            return _runner.Run(cancellationToken);
        }

        public static int Run<TBuilder, TService, TJobInput, TJobOutput>(
            IRelientBuilder<TBuilder, TService, TJobInput, TJobOutput> builder,
            CancellationToken cancellationToken = default(CancellationToken))
            where TBuilder : IRelientBuilder<TBuilder, TService, TJobInput, TJobOutput>
            where TService : IRelientService<TJobInput, TJobOutput>
        {
            return Run(builder, null, cancellationToken);
        }
    }
}
