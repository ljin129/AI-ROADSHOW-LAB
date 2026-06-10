using Beta.AIRoadshow.Entity.Dto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beta.AIRoadshow.Application;

/// <summary>
/// 从 Apollo Prompts 命名空间读取 Prompt 配置
/// </summary>
[Service(ServiceLifetime.Singleton)]
public class PromptConfigService
{
    private readonly IOptionsMonitor<PromptConfig> _promptConfig;

    public PromptConfigService(IOptionsMonitor<PromptConfig> promptConfig)
    {
        _promptConfig = promptConfig;
    }

    public IReadOnlyList<PromptItemDto> GetAll()
    {
        var list = _promptConfig.CurrentValue?.prompt_config;
        return list ?? new List<PromptItemDto>();
    }

    public PromptItemDto GetByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return GetAll().FirstOrDefault(x => string.Equals(x.name, name, StringComparison.OrdinalIgnoreCase));
    }
}
