namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 【请求DTO】删除业务环节
/// </summary>
public class BusinessStageRemoveRequestDto : BaseRequestDto
{
    /// <summary>
    /// 业务环节ID
    /// </summary>
    public long StageId { get; set; }
}
