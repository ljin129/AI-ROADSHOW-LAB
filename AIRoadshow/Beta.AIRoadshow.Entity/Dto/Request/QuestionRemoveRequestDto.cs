namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 【请求DTO】删除题目
/// </summary>
public class QuestionRemoveRequestDto : BaseRequestDto
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public long QuestionId { get; set; }
}
