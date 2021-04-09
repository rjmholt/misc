using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace psapi
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args[0].Equals("server", StringComparison.OrdinalIgnoreCase))
            {
                RunServer(args.Skip(1).ToArray());
                return;
            }

            if (args[0].Equals("talk", StringComparison.OrdinalIgnoreCase))
            {
                Talk.Run();
                return;
            }

            if (args[0].Equals("example", StringComparison.OrdinalIgnoreCase))
            {
                Dictionary<string, IExample> examples = typeof(Program).Assembly
                    .GetTypes()
                    .Where(t => t.IsAssignableTo(typeof(IExample)))
                    .Select(t => t.GetConstructor(Array.Empty<Type>()))
                    .Where(ctor => ctor is not null && ctor.DeclaringType.IsClass && !ctor.DeclaringType.IsAbstract)
                    .ToDictionary(
                        ctor => ctor.DeclaringType.Name,
                        ctor => (IExample)ctor.Invoke(parameters: null),
                        StringComparer.OrdinalIgnoreCase);

                examples[args[1]].Run();
                return;
            }
        }

        private static void RunServer(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .Build()
                .Run();
        }
    }

    class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton<IPowerShellService>(serviceProvider => ExamplePowerShellService.Create(maxRunspaces: 10));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    interface IExample
    {
        void Run();
    }

    public interface IPowerShellService
    {
        Task<string> RunPowerShellAsync(string script, CancellationToken cancellationToken);
    }
}
