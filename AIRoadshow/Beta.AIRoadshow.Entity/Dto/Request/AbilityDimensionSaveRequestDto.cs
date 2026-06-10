namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 【请求DTO】新增能力维度
/// </summary>
public class AbilityDimensionSaveRequestDto : BaseRequestDto
{
    /// <summary>
    /// 能力维度名称
    /// </summary>
    public string AbilityDimensionName { get; set; }

    /// <summary>
    /// 能力维度描述
    /// </summary>
    public string AbilityDimensionDesc { get; set; }

    /// <summary>
    /// 权重
    /// </summary>
    public decimal Weight { get; set; }

    /// <summary>
    /// 父级能力维度ID
    /// </summary>
    public long? ParentAbilityDimensionId { get; set; }
}
