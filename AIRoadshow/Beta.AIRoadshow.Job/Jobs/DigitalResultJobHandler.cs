using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using XxlJob.Core;
using XxlJob.Core.Model;

namespace Beta.AIRoadshow.Job.Jobs;

/// <summary>
/// 【Job】视频合成结果
/// </summary>
[JobHandler("DigitalResultJobHandler")]
public class DigitalResultJobHandler : IJobHandler
{
    private readonly ILogger<DigitalResultJobHandler> _logger;

    public DigitalResultJobHandler(ILogger<DigitalResultJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ReturnT> Execute(JobExecuteContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(context.JobParameter))
            {
                _logger.LogInformation("JobParameter不能为空");
                context.JobLogger.Log("JobParameter不能为空");
                return ReturnT.FAIL;
            }

            return ReturnT.SUCCESS;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            context.JobLogger.LogError(ex);
            return ReturnT.FAIL;
        }
    }
}
