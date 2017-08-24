using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Relient
{
    [PublicAPI]
    public interface IRelientJobProvider<TJobInput, TJobOutput>
    {
        Task<IReadOnlyCollection<KeyValuePair<string, IRelientJobInstance<TJobInput, TJobOutput>>>> GetJobInstancesAsync(
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
