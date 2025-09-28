using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Teedy.CL.Models.AlmesreyaModel;
using Teedy.CL.Models.Document;
using Teedy.CL.Models.Tags;
using Teedy.CL.Services.DatabaseService;
using Teedy.CL.Services.TeedyServices;
using Topshelf;

namespace Teedy.CA
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
