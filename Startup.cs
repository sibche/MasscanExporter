using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MasscanExporter.HostedServices;
using MasscanExporter.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using RazorLight;

namespace MasscanExporter
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var engine = new RazorLightEngineBuilder()
                        .UseFileSystemProject(Directory.GetCurrentDirectory())
                        .UseMemoryCachingProvider()
                        .Build();

            services.AddSingleton(engine);
            services.AddSingleton<OpenPortService>();

            services.AddHostedService<PortCheckerHostedService>();
            services.AddHostedService<OpenPortRecheckerHostedService>();

            services.Configure<IpOptions>(Configuration.GetSection("ip"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
                endpoints.MapMetrics();
            });
        }
    }
}
