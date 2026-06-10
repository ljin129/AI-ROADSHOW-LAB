using System;

namespace Beta.AIRoadshow.Entity.DBEntity;

/// <summary>
/// 【实体】路演活动
/// </summary>
public class ActivityEntity : BaseEntity
{
    /// <summary>
    /// 活动ID
    /// </summary>
    public long ActivityId { get; set; }

    /// <summary>
    /// 所属机构
    /// </summary>
    public int CustCompanyId { get; set; }

    /// <summary>
    /// 活动名称
    /// </summary>
    public string ActivityName { get; set; }

    /// <summary>
    /// 活动描述
    /// </summary>
    public string ActivityDesc { get; set; }

    public string CoverImage { get; set; }

    /// <summary>
    /// 核心目标
    /// </summary>
    public string CoreGoal { get; set; }

    /// <summary>
    /// 客户背景
    /// </summary>
    public string CustomerBackground { get; set; }

    /// <summary>
    /// 活动开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 活动结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 建议时长（分钟）
    /// </summary>
    public int? RecommendedDurationMinutes { get; set; }

    /// <summary>
    /// 发布状态
    /// </summary>
    public byte PublishStatus { get; set; }

    /// <summary>
    /// 是否删除
    /// </summary>
    public bool IsDeleted { get; set; }
}
