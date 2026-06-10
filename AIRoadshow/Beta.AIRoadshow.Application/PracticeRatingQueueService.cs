using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.Application;

[Service(ServiceLifetime.Singleton)]
public class PracticeRatingQueueService
{
    private readonly Channel<PracticeRatingTask> _channel =
        Channel.CreateBounded<PracticeRatingTask>(new BoundedChannelOptions(200)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    public ValueTask EnqueueAsync(PracticeRatingTask task, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(task, ct);

    public IAsyncEnumerable<PracticeRatingTask> DequeueAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}

public sealed class PracticeRatingTask
{
    public long PracticeRecordId { get; set; }

    public long ActivityId { get; set; }

    public long QuestionId { get; set; }

    public long UserId { get; set; }
}
