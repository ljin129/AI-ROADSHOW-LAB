namespace Beta.AIRoadshow.Entity.DBEntity;

/// <summary>
/// 【实体】学员演练记录明细扩展
/// </summary>
public class PracticeRecordDetailExtEntity : BaseEntity
{
    /// <summary>
    /// 明细ID
    /// </summary>
    public long DetailId { get; set; }

    /// <summary>
    /// 能力维度ID
    /// </summary>
    public long AbilityDimensionId { get; set; }

    /// <summary>
    /// 得分
    /// </summary>
    public decimal? Score { get; set; }

    /// <summary>
    /// 得分评价
    /// </summary>
    public string ScoreComment { get; set; }
}
