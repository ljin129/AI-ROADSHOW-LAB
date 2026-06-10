using XxlJob.Core;

namespace Beta.AIRoadshow.Job.Jobs;

/// <summary>
/// 自定义Service层注入
/// </summary>
public static class JobExtensio
{
    /// <summary>
    /// 注入自定义Service层
    /// </summary>
    public static IXxlJobBuilder AddJobExtension(this IXxlJobBuilder services)
    {
        services.AddJob<DigitalResultJobHandler>();
        return services;
    }
}
