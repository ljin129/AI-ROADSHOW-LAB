using System.Collections.Generic;

namespace Beta.AIRoadshow.Entity.Dto;

/// <summary>
/// Apollo Prompts 命名空间配置
/// </summary>
public class PromptConfig
{
    /// <summary>
    /// Prompt 配置列表
    /// </summary>
    public List<PromptItemDto> prompt_config { get; set; }
}
