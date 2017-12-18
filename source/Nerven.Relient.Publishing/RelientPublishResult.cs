using JetBrains.Annotations;

namespace Nerven.Relient.Publishing
{
    [PublicAPI]
    public enum RelientPublishResult
    {
        Ok = -1,
        Fail = 1,
        Panic = 2,
    }
}
