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
        galleryServer = new GalleryServer();
        await galleryServer.Start();

        var factory = new StdSchedulerFactory();
        var scheduler = await factory.GetScheduler();

        var galleryResetTrigger = TriggerBuilder.Create();
        galleryResetTrigger.WithCronSchedule("0 0 0 ? * MON *"); //elke maandag om 08:00 uur 's ochtends

        var galleryResetJob = JobBuilder.Create<GalleryResetJob>();

        await scheduler.ScheduleJob(
            galleryResetJob.Build(),
            galleryResetTrigger.Build());

        await scheduler.Start();

        while (galleryServer?.IsRunning ?? false)
        {
            var input = Console.ReadLine();
            switch (input)
            {
                case "quit":
                    if (galleryServer != null)
                        await galleryServer.Stop();
                    break;
                case "publish":
                    Console.WriteLine("publish started");
                    galleryServer?.ResetAndPublish();
                    Console.WriteLine("publish success");
                    break;
                default:
                    Console.WriteLine("unknown command");
                    break;
            }
        }

        await scheduler.Shutdown();
    }

    private class GalleryResetJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Reset job executed!!");
            galleryServer?.ResetAndPublish();
            await Task.Delay(100);
        }
    }
}
