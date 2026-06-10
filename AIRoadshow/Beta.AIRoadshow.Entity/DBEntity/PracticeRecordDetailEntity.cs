namespace Beta.AIRoadshow.Entity.DBEntity;

/// <summary>
/// 【实体】学员演练记录明细
/// </summary>
public class PracticeRecordDetailEntity : BaseEntity
{
    /// <summary>
    /// 明细ID
    /// </summary>
    public long DetailId { get; set; }

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
    /// 对话文字内容
    /// </summary>
    public string DialogContent { get; set; }

    /// <summary>
    /// 对话语音文件ID
    /// </summary>
    public string DialogVoiceFileId { get; set; }

    /// <summary>
    /// 内容类型
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// 所属题目ID
    /// </summary>
    public long? QuestionId { get; set; }

    /// <summary>
    /// 鏄惁杩介棶
    /// </summary>
    public bool IsFollowUp { get; set; }

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
