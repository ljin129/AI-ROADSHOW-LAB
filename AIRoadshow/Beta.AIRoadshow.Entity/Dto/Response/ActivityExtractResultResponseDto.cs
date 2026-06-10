using System;

namespace Beta.AIRoadshow.Entity.Dto.Response;

/// <summary>
/// 活动信息提取结果
/// </summary>
public class ActivityExtractResultResponseDto : BaseResponseDto
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public string TaskId { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// 提取结果
    /// </summary>
    public object Result { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; }
}
