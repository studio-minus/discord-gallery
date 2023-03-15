using gallery.bot;
using gallery.shared;
using Quartz;
using Quartz.Impl;

namespace gallery.server;

internal class Program
{
    private static GalleryServer? galleryServer;

    private static async Task Main(string[] args)
    {
        Configuration.Load(args.Length > 0 ? args[0] : "config.json");
        await RunServer();
    }

    private static async Task RunServer()
    {
        var factory = new StdSchedulerFactory();
        var scheduler = await factory.GetScheduler();

        var galleryResetTrigger = TriggerBuilder.Create();
        galleryResetTrigger.WithCronSchedule("0 0 0 ? * MON *");

        var galleryResetJob = JobBuilder.Create<GalleryResetJob>();

        await scheduler.ScheduleJob(
            galleryResetJob.Build(),
            galleryResetTrigger.Build());

        await scheduler.Start();

        bool shouldExitForReal = false;
        while (!shouldExitForReal)
        {
            try
            {
                galleryServer?.Dispose();
                galleryServer = new GalleryServer();
                await galleryServer.Start();

                while (galleryServer?.IsRunning ?? false)
                {
                    var input = Console.ReadLine();
                    switch (input)
                    {
                        case "quit":
                            if (galleryServer != null)
                                await galleryServer.Stop();
                            shouldExitForReal = true;
                            break;
                        case "publish":
                            Console.WriteLine("publish started");
                            galleryServer?.Publish();
                            Console.WriteLine("publish success");
                            break;
                        case "messageTest":
                            if (galleryServer != null)
                                _ = Task.Run(() => galleryServer.Bot.SendPublishMessage(galleryServer.Bot.Curator.GetBestArtwork(5).ToArray()));
                            break;
                        default:
                            Console.WriteLine("unknown command");
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Server crashed: {0}", e.Message);
            }
            finally
            {
                try
                {
                    galleryServer?.Stop();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Gallery server failed to stop: {0}", e.Message);
                }
            }

        }

        await scheduler.Shutdown();
    }

    private class GalleryResetJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Reset job executed!!");
            try
            {
                galleryServer?.Publish();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Publish job failed: {0}", e.Message);
            }
            await Task.Delay(100);
        }
    }
}
