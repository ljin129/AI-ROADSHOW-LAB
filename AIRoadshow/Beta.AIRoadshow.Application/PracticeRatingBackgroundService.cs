using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.Application;

public class PracticeRatingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PracticeRatingQueueService _queue;
    private readonly ILogger<PracticeRatingBackgroundService> _logger;

    public PracticeRatingBackgroundService(
        IServiceScopeFactory scopeFactory,
        PracticeRatingQueueService queue,
        ILogger<PracticeRatingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("题目评分后台服务已启动");

        await foreach (var task in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var ratingService = scope.ServiceProvider.GetRequiredService<PracticeRatingService>();
                await ratingService.RateAsync(task, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "评分任务处理失败，PracticeRecordId={PracticeRecordId}, QuestionId={QuestionId}",
                    task.PracticeRecordId, task.QuestionId);
            }
        }

        _logger.LogInformation("题目评分后台服务已停止");
    }
}
