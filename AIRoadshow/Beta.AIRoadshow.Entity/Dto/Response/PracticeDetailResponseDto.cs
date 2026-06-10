using System;
using System.Collections.Generic;

namespace Beta.AIRoadshow.Entity.Dto.Response;

public class PracticeDetailResponseDto : BaseResponseDto
{
    public long PracticeRecordId { get; set; }

    public long ActivityId { get; set; }

    public string PracticeStatus { get; set; }

    public decimal? Score { get; set; }

    public string ScoreComment { get; set; }

    public List<PracticeDetailItemDto> Details { get; set; } = new();
}

public class PracticeDetailItemDto : BaseResponseDto
{
    public long DetailId { get; set; }

    public string ContentType { get; set; }

    public long? QuestionId { get; set; }

    public long? RoleId { get; set; }

    public string RoleNickname { get; set; }

    public int? CustId { get; set; }

    public string PhotoUrl { get; set; }

    public string Content { get; set; }

    public string DialogVoiceFileId { get; set; }

    public bool IsFollowUp { get; set; }

    public decimal? Score { get; set; }

    public string ScoreComment { get; set; }

    public string Strengths { get; set; }

    public string Weaknesses { get; set; }

    public DateTime? CreateTime { get; set; }
}
