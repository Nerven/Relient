using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Relient.Publishing
{
    [PublicAPI]
    public static class RelientConsolePublisher
    {
        private static readonly object _ConsoleLock = new object();

        public static IRelientPublisher<TJobInput, TJobOutput> Create<TJobInput, TJobOutput>()
        {
            return RelientPublisher.Create<TJobInput, TJobOutput>((_notification, _error, _cancellationToken) =>
                {
                    lock (_ConsoleLock)
                    {
                        switch (_notification.NotificationType)
                        {
                            case RelientNotificationType.JobState:
                                Console.ForegroundColor = ConsoleColor.Black;
                                switch (_notification.JobState.Status)
                                {
                                    case RelientStatus.Ok:
                                        Console.BackgroundColor = ConsoleColor.Green;
                                        break;
                                    case RelientStatus.Warn:
                                        Console.BackgroundColor = ConsoleColor.Yellow;
                                        break;
                                    case RelientStatus.Fail:
                                        Console.BackgroundColor = ConsoleColor.Red;
                                        break;
                                }
                                break;
                            case RelientNotificationType.JobResult:
                                Console.BackgroundColor = ConsoleColor.White;
                                switch (_notification.JobResult.Status)
                                {
                                    case RelientStatus.Ok:
                                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                                        break;
                                    case RelientStatus.Warn:
                                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                                        break;
                                    case RelientStatus.Fail:
                                        Console.ForegroundColor = ConsoleColor.DarkRed;
                                        break;
                                    default:
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                        break;
                                }
                                break;
                            case RelientNotificationType.JobRecord:
                                Console.BackgroundColor = ConsoleColor.DarkGray;
                                switch (_notification.JobStatusRecord.Status)
                                {
                                    case RelientStatus.Ok:
                                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                                        break;
                                    case RelientStatus.Warn:
                                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                                        break;
                                    case RelientStatus.Fail:
                                        Console.ForegroundColor = ConsoleColor.DarkRed;
                                        break;
                                    default:
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                        break;
                                }
                                break;
                        }

                        Console.Write(_notification);
                        Console.ResetColor();
                        Console.WriteLine();
                    }

                    return Task.CompletedTask;
                });
        }
    }
}
