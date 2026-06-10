namespace Beta.AIRoadshow.Entity.Dto;

/// <summary>
/// Prompt 对应的 LLM 调用配置
/// </summary>
public class PromptLlmConfDto
{
    public int provider { get; set; }
    public string model { get; set; }
    public int max_tonkens { get; set; }
    public double temperature { get; set; }
    public double top_p { get; set; }
    public int est_time { get; set; }
    public int alt_provider { get; set; }
    public string alt_model { get; set; }
    public int alt_max_tokens { get; set; }
    public double alt_temperature { get; set; }
    public double alt_top_p { get; set; }
}
