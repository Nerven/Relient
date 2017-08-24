using System;
using System.Threading.Tasks;
using Nerven.Relient.Runner;
using Nerven.Relient.TaskulerService;
using Nerven.Taskuler.Core;

namespace Nerven.Relient.Samples.DotNetCoreSample
{
    public static class Program
    {
        public static int Main()
        {
            var _random = new Random();
            var _randomLock = new object();

            int _rnd(int n)
            {
                lock (_randomLock)
                {
                    return _random.Next(n);
                }
            }

            return RelientRunner.Run(RelientTaskuler.CreateBuilder<int, int>(), _configuration =>
                {
                    _configuration.Builder
                        .AddJob("Test A - Good", TimeSpan.FromSeconds(11), TimeSpan.FromSeconds(10), async (_context, _input, _cancellationToken) =>
                            {
                                await Task.Delay(100, _cancellationToken).ConfigureAwait(false);
                                _context.Ok(null, null);
                                return 0;
                            })
                        .AddJob("Test B - Warn", TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15), async (_context, _input, _cancellationToken) =>
                            {
                                await Task.Delay(100, _cancellationToken).ConfigureAwait(false);
                                _context.Warn(null, null);
                                return 0;
                            })
                        .AddJob("Test C - Fail", TimeSpan.FromSeconds(21), TimeSpan.FromSeconds(10), async (_context, _input, _cancellationToken) =>
                            {
                                await Task.Delay(100, _cancellationToken).ConfigureAwait(false);
                                _context.Fail(null, null);
                                return 0;
                            })
                        .AddJob("Test D - Timeout", TimeSpan.FromSeconds(43), TimeSpan.FromSeconds(1), async (_context, _input, _cancellationToken) =>
                            {
                                await Task.Delay(10000, _cancellationToken).ConfigureAwait(false);
                                _context.Ok(null, null);
                                return 0;
                            })
                        .AddJob("Test E - Random", TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(10), async (_context, _input, _cancellationToken) =>
                            {
                                await Task.Delay(1000, _cancellationToken).ConfigureAwait(false);
                                if (_rnd(9) == 0)
                                {
                                    _context.Fail(null, null);
                                }

                                if (_rnd(7) == 0)
                                {
                                    _context.Warn(null, null);
                                }

                                await Task.Delay(1000, _cancellationToken).ConfigureAwait(false);

                                if (_rnd(1) == 0)
                                {
                                    _context.Ok(null, null);
                                }

                                await Task.Delay(1000, _cancellationToken).ConfigureAwait(false);

                                return 0;
                            });

                    _configuration.WithServiceHook(async (_service, _cancellationToken) =>
                        {
                            var _waitTaskSource = new TaskCompletionSource<int>();

                            using (_service.NotificationsSource.Subscribe(_notification =>
                                {
                                    Console.WriteLine($"[{_notification.JobInfo.Name} ({_notification.JobInfo.Status.State?.Status})] {_notification}");
                                }))
                            using (_service.TaskulerNotificationsSource.SubscribeConsole(Console.WriteLine))
                            using (_cancellationToken.Register(() => _waitTaskSource.SetResult(0)))
                            {
                                await _waitTaskSource.Task.ConfigureAwait(false);
                            }
                        });
                });
        }
    }
}
