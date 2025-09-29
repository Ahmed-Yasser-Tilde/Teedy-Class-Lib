using Microsoft.Extensions.Configuration;
using TeedyService;
using Topshelf;

class Program
{
    static void Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var language = config["ServiceConfig:Language"] ?? "en-US";
        var mode = config["ServiceConfig:Mode"] ?? "Service";

        Console.WriteLine($"Running in mode: {mode}, language: {language}");

        HostFactory.Run(x =>
        {
            x.Service<MainService>(s =>
            {
                s.ConstructUsing(_ => new MainService());
                s.WhenStarted(tc => tc.Start());
                s.WhenStopped(tc => tc.Stop());
            });

            x.RunAsLocalSystem();

            x.SetDescription("upload file");
            x.SetDisplayName("TeedyService");
            x.SetServiceName("TeedyService");
        });
    }
}
