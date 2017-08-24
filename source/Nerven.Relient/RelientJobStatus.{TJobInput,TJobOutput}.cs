using System.Collections.Generic;
using JetBrains.Annotations;
using Nerven.Assertion;
using Nerven.Assertion.Extensions;

namespace Nerven.Relient
{
    [PublicAPI]
    public sealed class RelientJobStatus<TJobInput, TJobOutput>
    {
        public RelientJobStatus(
            RelientState state,
            IReadOnlyList<RelientResult<TJobInput, TJobOutput>> recentResults)
        {
            Must.Assertion
                .AssertArgumentNotNull(recentResults, nameof(recentResults))
                .Assert(state == null || recentResults.Count != 0);

            State = state;
            RecentResults = recentResults;
        }

        public RelientState State { get; }

        public IReadOnlyList<RelientResult<TJobInput, TJobOutput>> RecentResults { get; }
    }
}
