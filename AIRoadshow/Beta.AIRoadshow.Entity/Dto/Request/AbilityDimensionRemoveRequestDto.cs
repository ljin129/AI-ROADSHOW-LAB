namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 【请求DTO】删除能力维度
/// </summary>
public class AbilityDimensionRemoveRequestDto : BaseRequestDto
{
    /// <summary>
    /// 能力维度ID
    /// </summary>
    public long AbilityDimensionId { get; set; }
}
