namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 业务环节更新请求
/// </summary>
public class BusinessStageUpdateRequestDto : BaseRequestDto
{
    public long StageId { get; set; }

    public string StageName { get; set; }

    public string StageDesc { get; set; }

    public string StageTask { get; set; }

    public int SortNo { get; set; }

    public int QuestionCount { get; set; }
}
