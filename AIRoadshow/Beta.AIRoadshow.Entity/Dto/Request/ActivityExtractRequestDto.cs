using System.Collections.Generic;

namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 活动信息提取请求
/// </summary>
public class ActivityExtractRequestDto : BaseRequestDto
{
    /// <summary>
    /// 文件地址集合
    /// </summary>
    public List<string> Files { get; set; }
}
