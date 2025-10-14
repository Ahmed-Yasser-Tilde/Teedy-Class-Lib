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
        string workingService = config["TeedySettings:WorkingService"];

        HostFactory.Run(x =>
        {
            
            if(workingService == nameof(MainService))
            {
                x.Service<MainService>(s =>
                {
                    s.ConstructUsing(_ => new MainService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
            }
            else
            {
                x.Service<DeleteService>(s =>
                {
                    s.ConstructUsing(_ => new DeleteService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });

            }
            x.RunAsLocalSystem();
            x.SetDescription("upload file");
            x.SetDisplayName("TeedyServiceTemp");
            x.SetServiceName("TeedyServiceTemp");

        });
    }
}
