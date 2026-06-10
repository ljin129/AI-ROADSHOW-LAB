using System.Collections.Generic;

namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 【请求DTO】批量删除题目
/// </summary>
public class QuestionBatchRemoveRequestDto : BaseRequestDto
{
    /// <summary>
    /// 题目ID集合
    /// </summary>
    public IEnumerable<long> QuestionIds { get; set; } = new List<long>();
}
