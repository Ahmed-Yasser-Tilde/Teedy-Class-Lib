using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeedyPackage.Models.AlmesreyaModel;
using TeedyPackage.Models.Document;
using TeedyPackage.Models.Tags;
using TeedyPackage.Services.DatabaseService;
using TeedyPackage.Services.TeedyServices;
using Topshelf;

namespace TeedyService
{
    public class Program
    {
        static void Main(string[] args)
        {
            MainService mainService = new MainService();
            var rc = HostFactory.Run(x =>                                   //1
            {
                x.Service<MainService>(s =>                                   //2
                {
                    s.ConstructUsing(name => mainService);                //3
                    s.WhenStarted(tc => tc.Start());                         //4
                    s.WhenStopped(tc => tc.Stop());                          //5
                });
                x.RunAsLocalSystem();                                       //6

                x.SetDescription("uploud file ");                   //7
                x.SetDisplayName("TeedyService");                                  //8
                x.SetServiceName("TeedyService");                                  //9
            });                                                             //10

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());  //11
            Environment.ExitCode = exitCode;

        }
 }
}
