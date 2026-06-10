namespace Beta.AIRoadshow.Entity.DBEntity;

/// <summary>
/// 【实体】角色
/// </summary>
public class RoleEntity : BaseEntity
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 活动ID
    /// </summary>
    public long ActivityId { get; set; }

    /// <summary>
    /// 客户ID
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// 角色昵称
    /// </summary>
    public string RoleNickname { get; set; }

    /// <summary>
    /// 职务
    /// </summary>
    public string JobTitle { get; set; }

    /// <summary>
    /// 项目角色
    /// </summary>
    public string ProjectRole { get; set; }

    /// <summary>
    /// 人物性格
    /// </summary>
    public string Personality { get; set; }

    /// <summary>
    /// 沟通风格
    /// </summary>
    public string CommunicationStyle { get; set; }

    /// <summary>
    /// 项目需求
    /// </summary>
    public string ProjectRequirement { get; set; }

    /// <summary>
    /// 是否删除
    /// </summary>
    public bool IsDeleted { get; set; }
}
