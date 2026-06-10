namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 角色新增请求
/// </summary>
public class RoleSaveRequestDto : BaseRequestDto
{
    public long ActivityId { get; set; }

    public int? CustomerId { get; set; }

    public string RoleNickname { get; set; }

    public string JobTitle { get; set; }

    public string ProjectRole { get; set; }

    public string Personality { get; set; }

    public string CommunicationStyle { get; set; }

    public string ProjectRequirement { get; set; }
}
