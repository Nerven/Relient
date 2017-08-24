using JetBrains.Annotations;

namespace Nerven.Relient
{
    [PublicAPI]
    public enum RelientStatus
    {
        Ok = 1,
        Warn = 2,
        Fail = 3,
    }
}
