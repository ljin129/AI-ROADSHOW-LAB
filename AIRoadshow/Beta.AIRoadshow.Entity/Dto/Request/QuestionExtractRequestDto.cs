using System.Collections.Generic;

namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 题库信息提取请求
/// </summary>
public class QuestionExtractRequestDto : BaseRequestDto
{
    /// <summary>
    /// 活动ID
    /// </summary>
    public long ActivityId { get; set; }

    /// <summary>
    /// 文件地址集合
    /// </summary>
    public List<string> Files { get; set; }
}
