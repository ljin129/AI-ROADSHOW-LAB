using System;

namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 活动主表更新请求
/// </summary>
public class ActivityUpdateRequestDto : BaseRequestDto
{
    public long ActivityId { get; set; }

    public string ActivityName { get; set; }

    public string ActivityDesc { get; set; }

    public string CoverImage { get; set; }

    public string CoreGoal { get; set; }

    public string CustomerBackground { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int? RecommendedDurationMinutes { get; set; }

    public byte PublishStatus { get; set; }
}
