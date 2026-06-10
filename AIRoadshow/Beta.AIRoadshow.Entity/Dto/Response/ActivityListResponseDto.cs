using System.Collections.Generic;

namespace Beta.AIRoadshow.Entity.Dto.Response;

/// <summary>
/// 【响应DTO】活动分页列表
/// </summary>
public class ActivityListResponseDto : BaseResponseDto
{
    /// <summary>
    /// 页码
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 每页条数
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总条数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 数据列表
    /// </summary>
    public IEnumerable<ActivityListItemResponseDto> Items { get; set; } = new List<ActivityListItemResponseDto>();
}
