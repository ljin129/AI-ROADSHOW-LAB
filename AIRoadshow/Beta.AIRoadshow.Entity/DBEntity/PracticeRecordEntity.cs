namespace Beta.AIRoadshow.Entity.DBEntity;

/// <summary>
/// 【实体】学员演练活动主表
/// </summary>
public class PracticeRecordEntity : BaseEntity
{
    /// <summary>
    /// 演练记录ID
    /// </summary>
    public long PracticeRecordId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 活动ID
    /// </summary>
    public long ActivityId { get; set; }

    /// <summary>
    /// 路演材料ID
    /// </summary>
    public long? RoadshowMaterialId { get; set; }

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

    /// <summary>
    /// 演练状态
    /// </summary>
    public string PracticeStatus { get; set; }
}
