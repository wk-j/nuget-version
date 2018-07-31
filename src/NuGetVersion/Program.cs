using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;

namespace NuGetVersion
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var package = args[0];
                var client = new HttpClient();
                var nuget = new NuGetClient(client);
                var info = await nuget.GetPackageInfo(package);
                var version = info.Versions.Where(x => !x.IsPrerelease).First();
                Console.WriteLine(version);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"~ Failed {ex.Message}");
            }
        }
    }
}