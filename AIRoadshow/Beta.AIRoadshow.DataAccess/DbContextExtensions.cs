using Microsoft.Extensions.DependencyInjection;
using System;

namespace Beta.AIRoadshow.DataAccess;

/// <summary>
/// 
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// 
    /// </summary>
    public static IServiceCollection AddBetaDbContext<T>(this IServiceCollection services, Func<T> func) where T : AIRoadshowDbContext
    {
        services.AddSingleton((IServiceProvider r) => func());
        return services;
    }
}