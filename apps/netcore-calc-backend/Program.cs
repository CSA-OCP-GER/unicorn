using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace netcore_calc_backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel()
                .UseApplicationInsights()
                .UseStartup<Startup>()
                .Build();
    }
}
