using System;

namespace Beta.AIRoadshow.Entity.Dto.Response;

/// <summary>
/// 【响应DTO】活动业务环节
/// </summary>
public class ActivityBusinessStageResponseDto : BaseResponseDto
{
    /// <summary>
    /// 业务环节ID
    /// </summary>
    public long StageId { get; set; }

    /// <summary>
    /// 活动ID
    /// </summary>
    public long ActivityId { get; set; }

    /// <summary>
    /// 业务环节名称
    /// </summary>
    public string StageName { get; set; }

    /// <summary>
    /// 业务环节描述
    /// </summary>
    public string StageDesc { get; set; }

    /// <summary>
    /// 业务环节任务
    /// </summary>
    public string StageTask { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int SortNo { get; set; }

    /// <summary>
    /// 题目数量
    /// </summary>
    public int QuestionCount { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; }
}
