namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 角色更新请求
/// </summary>
public class RoleUpdateRequestDto : BaseRequestDto
{
    public long RoleId { get; set; }

    public int? CustomerId { get; set; }

    public string RoleNickname { get; set; }

    public string JobTitle { get; set; }

    public string ProjectRole { get; set; }

    public string Personality { get; set; }

    public string CommunicationStyle { get; set; }

    public string ProjectRequirement { get; set; }
}
