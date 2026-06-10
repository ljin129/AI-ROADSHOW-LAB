namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 【请求DTO】删除角色
/// </summary>
public class RoleRemoveRequestDto : BaseRequestDto
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }
}
