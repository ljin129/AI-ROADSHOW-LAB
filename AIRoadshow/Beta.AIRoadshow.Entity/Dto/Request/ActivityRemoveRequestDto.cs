namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 【请求DTO】删除活动
/// </summary>
public class ActivityRemoveRequestDto : BaseRequestDto
{
    /// <summary>
    /// 活动ID
    /// </summary>
    public long ActivityId { get; set; }
}
