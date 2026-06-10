using Beta.AIRoadshow.Application;
using Beta.AIRoadshow.Common;
using Beta.AIRoadshow.DataAccess;
using Beta.AIRoadshow.Entity.Dto;
using Beta.Framework;
using Beta.Logging;
using Beta.RedisExtensions.StackExchangeExt;
using Beta.Utils;
using Beta.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySql.Data.MySqlClient;
using MongoDB.Driver;
using OpenTelemetry.Trace;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.PCApi;

/// <summary>
/// 
/// </summary>
public class Program : Starter.WebStartup
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

        services
                 .Configure<SwaggerConfig>(options =>
                 {
                 });

        services
            .AddControllers(x => x.Filters.Add(typeof(GlobalExceptionFilter)))
            .AddNewtonsoftJson(x =>
            {
                x.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                x.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
                x.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                x.SerializerSettings.Converters.Add(new LongToStringConverter());
            });

        services
            .AddBetaAuthJwtBearer(ConfigurationRoot);
        services
            .AddCors(options => options.AddPolicy("Beta.AIRoadshow.PCApi.Cors", x => x.AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("bjwt").SetIsOriginAllowed(_ => true).AllowCredentials()));

        services.AddHttpContextAccessor();
        services.AddStackExchangeExtensions(ConfigurationRoot);
        services.AddSingleton<IMongoClient>(_ => new MongoClient(ConfigurationRoot.GetConnectionString("MongoServer")));

        var connectionStrings = ConfigurationRoot.GetValue<string>("ConnectionStrings:BetaAIRoadshow");
        services
            .AddBetaDbContext(() => new AIRoadshowDbContext(() => new MySqlConnection(connectionStrings)));
        services
            .AddAutoMapper(typeof(AutoMapperProfile));
        services
            .AddGrpcServiceExtension();

        services.Configure<PromptConfig>(ConfigurationRoot.GetSection("Prompts"));
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void ConfigureBetaServices(IBetaBuilder builder)
    {
        base.ConfigureBetaServices(builder);

        builder
            .AddSwagger();
    }

    /// <summary>
    /// 
    /// </summary>
    public void Configure(IApplicationBuilder app, IHostEnvironment environment)
    {
        app
            .UseCors("Beta.AIRoadshow.PCApi.Cors");
        app
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization()
            .UseAutoMapper()
            .UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());
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
