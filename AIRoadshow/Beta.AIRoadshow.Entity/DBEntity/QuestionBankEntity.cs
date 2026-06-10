namespace Beta.AIRoadshow.Entity.DBEntity;

/// <summary>
/// 【实体】题库
/// </summary>
public class QuestionBankEntity : BaseEntity
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public long QuestionId { get; set; }

    /// <summary>
    /// 活动ID
    /// </summary>
    public long ActivityId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 题干
    /// </summary>
    public string QuestionStem { get; set; }

    /// <summary>
    /// 考察要点
    /// </summary>
    public string AssessmentPoints { get; set; }

    /// <summary>
    /// 是否必答
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 所属业务环节ID
    /// </summary>
    public long StageId { get; set; }

    /// <summary>
    /// 是否删除
    /// </summary>
    public bool IsDeleted { get; set; }
}
