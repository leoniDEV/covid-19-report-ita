using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net.Sockets;

using Covid19Report.Ita.Api.Abstraction;
using Covid19Report.Ita.Api.Abstraction.Service;
using Covid19Report.Ita.Api.Infrastructure;
using Covid19Report.Ita.Api.Service;

using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Azure.Cosmos;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

using Octokit;

namespace Covid19Report.Ita.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(o => o.AllowEmptyInputInBodyModelBinding = true);
            services.AddRazorPages().AddRazorPagesOptions(options => options.Conventions.AllowAnonymousToPage("/"));

            services.AddAuthentication("BasicAuthentication").AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("BasicAuthentication", null);

            services.AddHttpClient();

            var appInsightOptions = new ApplicationInsightsServiceOptions
            {
                DeveloperMode = true,
                EnableEventCounterCollectionModule = false,
            };

            services.AddApplicationInsightsTelemetry(appInsightOptions);

            services.AddScoped<IGitHubClient, GitHubClient>(sp => {
                return new GitHubClient(new ProductHeaderValue("covid19-ita-report"))
                {
                    Credentials = new Credentials(Configuration.GetSection("GitHubConfig:GitHubApiKey").Value)
                };
            });
            services.AddSingleton<CosmosSerializer, CosmosCovidSerializer>();
            services.AddSingleton<ICosmosClientFactory, CosmosClientFactory>();
            services.AddSingleton<ICosmosServiceFactory, CosmosServiceFactory>();
            services.AddScoped<IDataCollectorSerializer, JsonDataCollectorSerializer>();
            services.AddScoped<IDataCollector, DataCollector>();
            services.AddScoped<ICosmosRepository, CosmosRepository>();
            services.AddTransient<DbConnection>(sp => new SqlConnection(Configuration.GetConnectionString("covidDb")));

            // Register configurations
            services.Configure<CosmosRepositoryOptions>(o => o.Databases = Configuration.GetSection("cosmos:databases")
                                                .GetChildren().ToDictionary(d => d.Key, d => d.GetSection("containers")
                                                                      .GetChildren()
                                                                      .ToDictionary(c => c.Key, c => c.Value)));
            services.Configure<CosmosClientFactoryOptions>(Configuration.GetSection("cosmos"));
            services.Configure<GitHubConfig>(Configuration.GetSection("GitHubConfig"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseFileServer(new FileServerOptions
                {
                    FileProvider = new PhysicalFileProvider(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\", "TestData"))),
                    EnableDirectoryBrowsing = true,
                    RequestPath = "/testdata"
                });
            }

            if (env.IsStaging())
            {
                app.UseDeveloperExceptionPage();
            }

            if (!env.IsDevelopment())
            {
                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });
            }

            app.UseRouting();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }
    }
}
