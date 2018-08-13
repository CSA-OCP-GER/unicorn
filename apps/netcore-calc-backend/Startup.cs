using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using TelemetryClient = Microsoft.ApplicationInsights.TelemetryClient;

namespace netcore_calc_backend
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables();

            if (env.IsDevelopment()) builder.AddApplicationInsightsSettings(true);

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Configuration);

            services.AddApplicationInsightsTelemetry(options =>
            {
                options.InstrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"];
            });

            services.AddSingleton(new TelemetryClient(new TelemetryConfiguration
            {
                InstrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"]
            }));
        }

        private static void HandlePing(IApplicationBuilder app)
        {
            var tc = app.ApplicationServices.GetService<TelemetryClient>();

            app.Run(async context =>
            {
                tc.TrackEvent("ping-netcore-backend-received");
                Console.WriteLine("received ping");
                await context.Response.WriteAsync("Pong");
            });
        }

        private static void HandleCalculation(IApplicationBuilder app)
        {
            var tc = app.ApplicationServices.GetService<TelemetryClient>();
            app.Run(async context =>
            {
                var factors = new List<int>();
                var sw = Stopwatch.StartNew();

                if (int.TryParse(context.Request.Headers["number"], out var num))
                {
                    Console.WriteLine("received client request:");
                    Console.WriteLine(num);
                    tc.TrackEvent("calculation-netcore-backend-call");
                    try
                    {
                        factors = PrimeFactors(num);
                        Console.WriteLine("calculated:");
                        Console.WriteLine(JsonConvert.SerializeObject(factors));
                    }
                    catch (Exception e)
                    {
                        tc.TrackException(e);
                    }
                }
                else
                {
                    factors = new List<int> {0};
                }
                sw.Stop();
                tc.TrackEvent("calculation-netcore-backend-result");
                tc.TrackMetric("calculation-netcore-backend-duration", sw.ElapsedMilliseconds);
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new
                {
                    __v = "1.0",
                    timestamp = DateTimeOffset.UtcNow.ToString("u"),
                    value = factors,
                    host = Environment.MachineName
                    // timetocalc = sw.ElapsedMilliseconds
                }));
            });
        }

        private static void HandleDummy(IApplicationBuilder app)
        {
            var tc = app.ApplicationServices.GetService<TelemetryClient>();

            app.Run(async context =>
            {
                Console.WriteLine("received dummy request");
                tc.TrackEvent("dummy-netcore-backend-call");
                await context.Response.WriteAsync("42");
            });
        }

        private static List<int> PrimeFactors(int remainder)
        {
            var factors = new List<int>();

            for (var i = 2; i <= remainder; i++)
                while (remainder % i == 0)
                {
                    factors.Add(i);
                    remainder /= i;
                }

            return factors;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.Map("/ping", HandlePing);
            app.Map("/api/dummy", HandleDummy);
            app.Map("/api/calculation", HandleCalculation);

            app.Run(async context =>
            {
                Console.WriteLine("received request");
                await context.Response.WriteAsync("Hi!");
            });
        }
    }
}