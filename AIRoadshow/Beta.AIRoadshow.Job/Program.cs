using Beta.AIRoadshow.Application;
using Beta.AIRoadshow.DataAccess;
using Beta.AIRoadshow.Entity.Dto;
using Beta.AIRoadshow.Job.Jobs;
using Beta.Logging;
using Beta.RedisExtensions.StackExchangeExt;
using Beta.Starter;
using Beta.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySql.Data.MySqlClient;
using OpenTelemetry.Trace;
using Serilog;
using System;
using System.Threading.Tasks;
using XxlJob.AspNetCore;
using XxlJob.Core;

namespace Beta.AIRoadshow.Job;

/// <summary>
/// 
/// </summary>
public class Program : WebStartup
{
    /// <summary>
    /// 
    /// </summary>
    public static Task Main(string[] args)
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        ServiceInspector.ForceCheckPrecondition = false;
        Environment.CurrentDirectory = AppContext.BaseDirectory;
        return new Program().Configuration(args).RunAsync();
    }

    /// <summary>
    /// 
    /// </summary>
    private Program() { }

    /// <summary>
    /// 
    /// </summary>
    public Program(IConfiguration configuration) => ConfigurationRoot = configuration;

    /// <summary>
    /// 
    /// </summary>
    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        BaseConstants.Configuration = ConfigurationRoot;
        services
            .AddHttpClient();
        services.AddStackExchangeExtensions(ConfigurationRoot);
        var connectionStrings = ConfigurationRoot.GetValue<string>("ConnectionStrings:BetaAIRoadshow");
        services
            .AddBetaDbContext(() => new AIRoadshowDbContext(() => new MySqlConnection(connectionStrings)));
        services
            .AddAutoMapper(typeof(AutoMapperProfile));
        services
            .AddGrpcServiceExtension();
        services.Configure<PromptConfig>(ConfigurationRoot.GetSection("Prompts"));
        services
            .AddXxlJob(ConfigurationRoot)
            .AddBetaConfig()
            .AddJobExtension();
    }

    /// <summary>
    /// 
    /// </summary>
    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting()
            .UseAutoMapper()
            .UseEndpoints(routes => routes.MapXxlJob());
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void ConfigureHost(IHostBuilder hostBuilder)
    {
        base.ConfigureHost(hostBuilder);
        hostBuilder.UseSerilog((hostingContext, serilogConfig) => serilogConfig.ReadFrom.Configuration(hostingContext.Configuration), writeToProviders: true);
    }

    /// <summary>
    /// 配置跟踪
    /// </summary>
    protected override void ConfigureTracing(IServiceProvider provider, TracerProviderBuilder builder)
    {
        builder.AddMySqlDataInstrumentation(options => options.SetDbStatement = options.EnableConnectionLevelAttributes = options.RecordException = true);
    }
}
