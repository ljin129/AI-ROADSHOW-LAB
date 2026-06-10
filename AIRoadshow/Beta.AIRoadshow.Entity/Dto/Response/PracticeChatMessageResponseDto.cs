namespace Beta.AIRoadshow.Entity.Dto.Response;

public class PracticeChatMessageResponseDto : BaseResponseDto
{
    public long DetailId { get; set; }

    public long PracticeRecordId { get; set; }

    public long ActivityId { get; set; }

    public long? QuestionId { get; set; }

    public long? RoleId { get; set; }

    public string RoleNickname { get; set; }

    public int? CustId { get; set; }

    public string PhotoUrl { get; set; }

    public long? StageId { get; set; }

    public string StageName { get; set; }

    public string ContentType { get; set; }

    public bool IsFollowUp { get; set; }

    public string Content { get; set; }

    public string DialogVoiceFileId { get; set; }

    public string PracticeStatus { get; set; }

    public bool IsCompleted { get; set; }
}
