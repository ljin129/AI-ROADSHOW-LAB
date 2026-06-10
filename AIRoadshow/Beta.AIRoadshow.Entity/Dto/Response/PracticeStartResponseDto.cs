namespace Beta.AIRoadshow.Entity.Dto.Response;

public class PracticeStartResponseDto : BaseResponseDto
{
    public long PracticeRecordId { get; set; }

    public string PracticeStatus { get; set; }

    public long? RoadshowMaterialId { get; set; }
}
