namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 【请求DTO】大模型文件上传
/// </summary>
public class LlmFileUploadRequestDto : BaseRequestDto
{
    /// <summary>
    /// 文件用途
    /// </summary>
    public string Purpose { get; set; }

    /// <summary>
    /// Provider
    /// </summary>
    public string Provider { get; set; }

    /// <summary>
    /// 目标模型名称
    /// </summary>
    public string TargetModelNames { get; set; }

    /// <summary>
    /// 目标存储
    /// </summary>
    public string TargetStorage { get; set; }

    /// <summary>
    /// 自定义模型提供商
    /// </summary>
    public string CustomLlmProvider { get; set; }

    /// <summary>
    /// 扩展元数据
    /// </summary>
    public string LitellmMetadata { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// 文件类型
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// 文件二进制
    /// </summary>
    public byte[] FileBytes { get; set; }
}
