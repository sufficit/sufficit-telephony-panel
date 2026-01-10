using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Sufficit.Telephony.BlazorPanel
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // FIX: Configure minimum Thread Pool to avoid starvation
            // Increased values to support multiple Blazor Server connections and async operations
            System.Threading.ThreadPool.SetMinThreads(workerThreads: 100, completionPortThreads: 100);
            
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(server =>
                    {
                        server.ConfigureEndpointDefaults(listenOptions =>
                        {
                            listenOptions.Use(next => new ClearTextHttpMultiplexingMiddleware(next).OnConnectAsync);
                        });
                    });

                    webBuilder.UseIIS();
                    webBuilder.UseIISIntegration();

                    webBuilder.UseStartup<Startup>();
                });
    }
}