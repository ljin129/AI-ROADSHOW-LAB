namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 题目新增请求
/// </summary>
public class QuestionSaveRequestDto : BaseRequestDto
{
    public long ActivityId { get; set; }

    public long RoleId { get; set; }

    public long StageId { get; set; }

    public string QuestionStem { get; set; }

    public string AssessmentPoints { get; set; }

    public bool IsRequired { get; set; }
}
