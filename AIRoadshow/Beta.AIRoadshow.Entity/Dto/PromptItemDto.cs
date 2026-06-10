namespace Beta.AIRoadshow.Entity.Dto;

/// <summary>
/// Apollo Prompts 命名空间中的单条 Prompt 配置
/// </summary>
public class PromptItemDto
{
    /// <summary>
    /// 唯一标识
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 中文名称
    /// </summary>
    public string c_name { get; set; }

    /// <summary>
    /// 系统 Prompt 模板
    /// </summary>
    public string system_prompt { get; set; }

    /// <summary>
    /// LLM 调用配置
    /// </summary>
    public PromptLlmConfDto llm_conf { get; set; }
}
