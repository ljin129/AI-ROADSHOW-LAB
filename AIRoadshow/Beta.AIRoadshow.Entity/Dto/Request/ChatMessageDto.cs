namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 单条消息
/// </summary>
public class ChatMessageDto : BaseRequestDto
{
    /// <summary>
    /// 角色：user / assistant / system / developer
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; }
}
