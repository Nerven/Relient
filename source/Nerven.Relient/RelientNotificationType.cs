using JetBrains.Annotations;

namespace Nerven.Relient
{
    [PublicAPI]
    public enum RelientNotificationType
    {
        JobState = 2,
        JobResult = 3,
        JobRecord = 4,
    }
}
