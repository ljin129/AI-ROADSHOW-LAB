namespace Beta.AIRoadshow.Entity.Dto.Request;

public class PracticeChatRequestDto : BaseRequestDto
{
    public long ActivityId { get; set; }

    public long PracticeRecordId { get; set; }

    public long? CurrentDetailId { get; set; }

    public string AiReplyVoiceFileId { get; set; }

    public string AnswerVoiceFileId { get; set; }

    public string AnswerContent { get; set; }
}
