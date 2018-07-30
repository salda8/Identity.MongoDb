using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;

namespace Identity.MongoDb.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
           .AddJsonFile("appsettings.json")
           
           .Build();

            Log.Logger = new LoggerConfiguration()
             //.WriteTo.
           .ReadFrom.Configuration(configuration)

          .CreateLogger();

            try
            {
                Log.Information("Starting web host");
                CreateWebHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseApplicationInsights()

                .UseStartup<Startup>().UseSerilog();
    }
}