using System;

namespace Beta.AIRoadshow.Entity.Dto.Response;

/// <summary>
/// 活动配置详情
/// </summary>
public class ActivityDetailActivityResponseDto : BaseResponseDto
{
    public long ActivityId { get; set; }

    public int CustCompanyId { get; set; }

    public string ActivityName { get; set; }

    public string ActivityDesc { get; set; }

    public string CoverImage { get; set; }

    public string CoreGoal { get; set; }

    public string CustomerBackground { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int? RecommendedDurationMinutes { get; set; }

    public byte PublishStatus { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime UpdateTime { get; set; }
}
