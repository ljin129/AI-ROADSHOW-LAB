namespace Beta.AIRoadshow.Entity.DBEntity;

/// <summary>
/// 【实体】能力维度
/// </summary>
public class AbilityDimensionEntity : BaseEntity
{
    /// <summary>
    /// 能力维度ID
    /// </summary>
    public long AbilityDimensionId { get; set; }

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

    /// <summary>
    /// 是否删除
    /// </summary>
    public bool IsDeleted { get; set; }
}
