namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 题目更新请求
/// </summary>
public class QuestionUpdateRequestDto : BaseRequestDto
{
    public long QuestionId { get; set; }

    public long RoleId { get; set; }

    public long StageId { get; set; }

    public string QuestionStem { get; set; }

    public string AssessmentPoints { get; set; }

    public bool IsRequired { get; set; }
}
