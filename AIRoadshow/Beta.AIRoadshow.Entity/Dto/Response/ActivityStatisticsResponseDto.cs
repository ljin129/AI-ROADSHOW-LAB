namespace Beta.AIRoadshow.Entity.Dto.Response;

/// <summary>
/// 【响应DTO】活动统计
/// </summary>
public class ActivityStatisticsResponseDto : BaseResponseDto
{
    /// <summary>
    /// 活动总数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 已发布总数
    /// </summary>
    public int PublishedCount { get; set; }

    /// <summary>
    /// 未发布总数
    /// </summary>
    public int UnpublishedCount { get; set; }

    /// <summary>
    /// 本月新增总数
    /// </summary>
    public int CurrentMonthNewCount { get; set; }
}
