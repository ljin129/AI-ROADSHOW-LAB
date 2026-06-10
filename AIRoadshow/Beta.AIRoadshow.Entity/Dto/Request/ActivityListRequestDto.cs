using System;

namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 【请求DTO】活动列表
/// </summary>
public class ActivityListRequestDto : BaseRequestDto
{
    /// <summary>
    /// 页码
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 每页条数
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// 活动开始日期
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 活动结束日期
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 发布状态
    /// </summary>
    public byte? PublishStatus { get; set; }
}
