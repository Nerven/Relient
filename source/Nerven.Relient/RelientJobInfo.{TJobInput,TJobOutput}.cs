using JetBrains.Annotations;
using Nerven.Assertion;
using Nerven.Assertion.Extensions;

namespace Nerven.Relient
{
    [PublicAPI]
    public sealed class RelientJobInfo<TJobInput, TJobOutput>
    {
        public RelientJobInfo(
            string name, 
            IRelientJobInstance<TJobInput, TJobOutput> instance,
            RelientJobStatus<TJobInput, TJobOutput> status)
        {
            Must.Assertion
                .AssertArgumentNotNull(name, nameof(name))
                .AssertArgumentNotNull(instance, nameof(instance))
                .AssertArgumentNotNull(status, nameof(status));

            Name = name;
            Instance = instance;
            Status = status;
        }

        public string Name { get; }

        public IRelientJobInstance<TJobInput, TJobOutput> Instance { get; }

        public RelientJobStatus<TJobInput, TJobOutput> Status { get; }
    }
}
