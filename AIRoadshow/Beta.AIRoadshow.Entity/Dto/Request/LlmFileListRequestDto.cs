namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 【请求DTO】大模型文件列表
/// </summary>
public class LlmFileListRequestDto : BaseRequestDto
{
    /// <summary>
    /// Provider
    /// </summary>
    public string Provider { get; set; }

    /// <summary>
    /// 目标模型名称
    /// </summary>
    public string TargetModelNames { get; set; }

    /// <summary>
    /// 文件用途
    /// </summary>
    public string Purpose { get; set; }
}
