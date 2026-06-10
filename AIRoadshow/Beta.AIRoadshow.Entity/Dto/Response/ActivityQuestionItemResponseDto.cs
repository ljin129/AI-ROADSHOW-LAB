using System;

namespace Beta.AIRoadshow.Entity.Dto.Response;

/// <summary>
/// 【响应DTO】活动题库项
/// </summary>
public class ActivityQuestionItemResponseDto : BaseResponseDto
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
    /// 角色昵称
    /// </summary>
    public string RoleNickname { get; set; }

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
    /// 业务环节ID
    /// </summary>
    public long StageId { get; set; }

    /// <summary>
    /// 业务环节名称
    /// </summary>
    public string StageName { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; }
}
