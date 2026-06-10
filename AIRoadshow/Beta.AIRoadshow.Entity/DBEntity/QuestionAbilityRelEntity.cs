namespace Beta.AIRoadshow.Entity.DBEntity;

/// <summary>
/// 【实体】题库与能力维度关系
/// </summary>
public class QuestionAbilityRelEntity : BaseEntity
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public long QuestionId { get; set; }

    /// <summary>
    /// 能力维度ID
    /// </summary>
    public long AbilityDimensionId { get; set; }

    /// <summary>
    /// 难度等级
    /// </summary>
    public byte DifficultyLevel { get; set; }
}
