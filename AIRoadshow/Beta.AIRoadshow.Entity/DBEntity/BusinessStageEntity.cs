namespace Beta.AIRoadshow.Entity.DBEntity;

/// <summary>
/// 【实体】业务环节
/// </summary>
public class BusinessStageEntity : BaseEntity
{
    /// <summary>
    /// 环节ID
    /// </summary>
    public long StageId { get; set; }

    /// <summary>
    /// 活动ID
    /// </summary>
    public long ActivityId { get; set; }

    /// <summary>
    /// 环节名称
    /// </summary>
    public string StageName { get; set; }

    /// <summary>
    /// 环节描述
    /// </summary>
    public string StageDesc { get; set; }

    /// <summary>
    /// 环节任务
    /// </summary>
    public string StageTask { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int SortNo { get; set; }

    /// <summary>
    /// 出题数量
    /// </summary>
    public int QuestionCount { get; set; }

    /// <summary>
    /// 是否删除
    /// </summary>
    public bool IsDeleted { get; set; }
}
