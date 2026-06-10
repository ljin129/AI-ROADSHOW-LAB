namespace Beta.AIRoadshow.Entity.DBEntity;

/// <summary>
/// 【实体】学员演练记录环节评价
/// </summary>
public class PracticeStageEvaluationEntity : BaseEntity
{
    /// <summary>
    /// 环节评价ID
    /// </summary>
    public long StageEvaluationId { get; set; }

    /// <summary>
    /// 演练记录ID
    /// </summary>
    public long PracticeRecordId { get; set; }

    /// <summary>
    /// 业务环节ID
    /// </summary>
    public long StageId { get; set; }

    /// <summary>
    /// 得分
    /// </summary>
    public decimal? Score { get; set; }

    /// <summary>
    /// 得分评价
    /// </summary>
    public string ScoreComment { get; set; }

    /// <summary>
    /// 优势
    /// </summary>
    public string Strengths { get; set; }

    /// <summary>
    /// 不足
    /// </summary>
    public string Weaknesses { get; set; }

    /// <summary>
    /// 建议
    /// </summary>
    public string Suggestions { get; set; }
}
