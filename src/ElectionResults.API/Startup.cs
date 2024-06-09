using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ElectionResults.API.Converters;
using ElectionResults.API.Options;
using ElectionResults.Core.Configuration;
using ElectionResults.Core.Elections;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace ElectionResults.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDefaultIdentity<IdentityUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>();

            /*services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<GzipCompressionProvider>();
            });*/
            services.AddControllersWithViews();
            services.AddRazorPages();

            services
                .AddMvc()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(new SnakeCaseNamingPolicy()));
                });
            services.AddLazyCache();
            RegisterDependencies(services, Configuration);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Rezultate Vot API", Version = "v1" });
            });

            var connectionString = Configuration["ConnectionStrings:DefaultConnection"]!;

            services.AddDbContextPool<ApplicationDbContext>(options =>
            {
                options.UseMySQL(connectionString);
            });

            services.AddLazyCache();
            services.AddCors(options =>
            {
                options.AddPolicy("origins",
                    builder =>
                    {
                        builder.WithOrigins("*")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowAnyOrigin();
                    });
            });

            services.AddHealthChecks().AddMySql(name: "domain-db", connectionString: connectionString);
        }

        private static void RegisterDependencies(IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IResultsAggregator, ResultsAggregator>();

            services.AddTransient<IAuthorsRepository, AuthorsRepository>();
            services.AddTransient<IWinnersAggregator, WinnersAggregator>();

            services.AddTransient<IArticleRepository, ArticleRepository>();
            services.AddTransient<IElectionsRepository, ElectionsRepository>();
            services.AddTransient<ITerritoryRepository, TerritoryRepository>();
            services.AddTransient<IPicturesRepository, PicturesRepository>();
            services.AddTransient<IBallotsRepository, BallotsRepository>();
            services.AddTransient<IPartiesRepository, PartiesRepository>();
            services.Configure<AWSS3Settings>(configuration.GetSection(AWSS3Settings.SectionKey));
            services.Configure<MemoryCacheSettings>(configuration.GetSection(MemoryCacheSettings.SectionKey));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ApplicationDbContext context, ILoggerFactory loggerFactory)
        {
            Log.SetLogger(loggerFactory.CreateLogger<Startup>());
            app.UseSwagger();
            Console.WriteLine($"Environment: {env.EnvironmentName}");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            MigrateDatabase(context);
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rezultate Vot API V2");
            });
            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors("origins");

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseHealthChecks("/health");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();

                if (env.IsProduction())
                {
                    endpoints.MapGet("/Identity/Account/Register", context => Task.Factory.StartNew(() => context.Response.Redirect("/Identity/Account/Login", true)));
                    endpoints.MapPost("/Identity/Account/Register", context => Task.Factory.StartNew(() => context.Response.Redirect("/Identity/Account/Login", true)));
                }
            });
        }

        private static void MigrateDatabase(ApplicationDbContext context)
        {
            try
            {
                context.Database.Migrate();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
