using System.Collections.Generic;

namespace Beta.AIRoadshow.Entity.Dto.Response;

/// <summary>
/// 【响应DTO】活动题库分页结果
/// </summary>
public class ActivityQuestionPageResponseDto : BaseResponseDto
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
    public IEnumerable<ActivityQuestionItemResponseDto> Items { get; set; } = new List<ActivityQuestionItemResponseDto>();
}
