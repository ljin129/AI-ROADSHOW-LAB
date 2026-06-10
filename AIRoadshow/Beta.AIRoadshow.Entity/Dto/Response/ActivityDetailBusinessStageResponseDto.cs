using System;

namespace Beta.AIRoadshow.Entity.Dto.Response;

/// <summary>
/// 业务环节配置详情
/// </summary>
public class ActivityDetailBusinessStageResponseDto : BaseResponseDto
{
    public long StageId { get; set; }

    public long ActivityId { get; set; }

    public string StageName { get; set; }

    public string StageDesc { get; set; }

    public string StageTask { get; set; }

    public int SortNo { get; set; }

    public int QuestionCount { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime UpdateTime { get; set; }
}
