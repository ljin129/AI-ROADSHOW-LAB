using System.Collections.Generic;

namespace Beta.AIRoadshow.Entity.Dto.Response;

/// <summary>
/// 活动详情
/// </summary>
public class ActivityDetailResponseDto : BaseResponseDto
{
    /// <summary>
    /// 活动配置信息
    /// </summary>
    public ActivityDetailActivityResponseDto Activity { get; set; }

    /// <summary>
    /// 角色配置信息
    /// </summary>
    public IEnumerable<ActivityDetailRoleResponseDto> Roles { get; set; } = new List<ActivityDetailRoleResponseDto>();

    /// <summary>
    /// 业务环节配置信息
    /// </summary>
    public IEnumerable<ActivityDetailBusinessStageResponseDto> BusinessStages { get; set; } = new List<ActivityDetailBusinessStageResponseDto>();
}
