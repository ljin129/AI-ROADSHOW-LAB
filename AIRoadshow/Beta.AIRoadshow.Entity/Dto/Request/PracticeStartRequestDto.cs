namespace Beta.AIRoadshow.Entity.Dto.Request;

public class PracticeStartRequestDto : BaseRequestDto
{
    public long ActivityId { get; set; }

    public long? RoadshowMaterialId { get; set; }
}
