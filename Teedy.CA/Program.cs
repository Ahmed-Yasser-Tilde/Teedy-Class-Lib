using Microsoft.Extensions.Configuration;
using Teedy.CL;

namespace Teedy.CA
{
    public class Program
    {
        static void Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsetting.json")
                .Build();

            string connectionString = configuration["connectionDefualt:connection"];

            TeedyApiMethods apiMethods = new TeedyApiMethods(configuration);

        }
    }
}
