using System;

namespace Beta.AIRoadshow.Entity.Dto.Response;

/// <summary>
/// 角色配置详情
/// </summary>
public class ActivityDetailRoleResponseDto : BaseResponseDto
{
    public long RoleId { get; set; }

    public long ActivityId { get; set; }

    public int? CustomerId { get; set; }

    public string PhotoUrl { get; set; }

    public string RoleNickname { get; set; }

    public string JobTitle { get; set; }

    public string ProjectRole { get; set; }

    public string Personality { get; set; }

    public string CommunicationStyle { get; set; }

    public string ProjectRequirement { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime UpdateTime { get; set; }
}
