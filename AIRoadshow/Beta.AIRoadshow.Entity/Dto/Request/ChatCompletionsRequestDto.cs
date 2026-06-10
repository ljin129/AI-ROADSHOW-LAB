using System.Collections.Generic;

namespace Beta.AIRoadshow.Entity.Dto.Request;

/// <summary>
/// 大模型对话补全请求
/// </summary>
public class ChatCompletionsRequestDto : BaseRequestDto
{
    /// <summary>
    /// 模型名称，如 my-custom-model、VAR_chat_model_id
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// 消息列表
    /// </summary>
    public List<ChatMessageDto> Messages { get; set; }

    /// <summary>
    /// 是否流式返回，流式接口使用
    /// </summary>
    public bool Stream { get; set; }

    /// <summary>
    /// 最大生成 token 数
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// 温度，控制随机性
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Top-P 采样
    /// </summary>
    public double? TopP { get; set; }
}
