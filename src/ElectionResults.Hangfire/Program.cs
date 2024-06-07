using ElectionResults.Core.Repositories;
using ElectionResults.Hangfire;
using ElectionResults.Hangfire.Apis;
using Hangfire;
using Serilog;
using ElectionResults.Hangfire.Options;
using ElectionResults.Hangfire.Jobs;
using Microsoft.EntityFrameworkCore;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.Refit.Destructurers;
using ElectionResults.Hangfire.Repository;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureAppOptions();
builder.Services.ConfigureApis();
builder.Services.RegisterJobs();

builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
{
    options.UseMySQL(builder.Configuration["ConnectionStrings:DefaultConnection"]!);
});

builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));

builder.Services.AddLogging(logging =>
{
    Serilog.Debugging.SelfLog.Enable(Console.WriteLine);

    var loggerConfiguration = new LoggerConfiguration()
        .WriteTo.Console()
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentUserName()
        .Enrich
        .WithExceptionDetails(new DestructuringOptionsBuilder()
            .WithDefaultDestructurers()
            .WithDestructurers(new[] { new ApiExceptionDestructurer(destructureHttpContent: true) }));

    var logger = Log.Logger = loggerConfiguration.CreateLogger();

    logging.AddSerilog(logger);
});

builder.Services.AddHangfire((sp, config) =>
{
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSerilogLogProvider()
        .UseColouredConsoleLogProvider()
        .UseInMemoryStorage();

    config.UseActivator(new ContainerJobActivator(sp));
    config.UseFilter(new AutomaticRetryAttribute { Attempts = 3 });
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 10;
});

builder.Services
    .AddHealthChecks()
    .AddHangfire(name: "hangfire", setup: options => { options.MinimumAvailableServers = 1; });


var app = builder.Build();
app.WithHangfireDashboard();
app.WithJobs();

app.MapHealthChecks("/health");
app.Run();
