using System;

namespace Beta.AIRoadshow.Entity.DBEntity;

/// <summary>
/// 【实体】基础
/// </summary>
public class BaseEntity
{
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; }

    /// <summary>
    /// 是否删除
    /// </summary>
    public bool IsDel { get; set; }

    /// <summary>
    /// 删除时间
    /// </summary>
    public DateTime? DelTime { get; set; }
}
