namespace Beta.AIRoadshow.Entity.Dto.Response;

public class PracticeRoleProfileResponseDto : BaseResponseDto
{
    public long ActivityId { get; set; }

    public long PracticeRecordId { get; set; }

    public long? QuestionId { get; set; }

    public long? RoleId { get; set; }

    public string RoleNickname { get; set; }

    public int? CustId { get; set; }

    public string PhotoUrl { get; set; }

    public long? StageId { get; set; }

    public string StageName { get; set; }
}
