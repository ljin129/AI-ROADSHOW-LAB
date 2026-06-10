using Beta.AIRoadshow.DataAccess;
using Beta.AIRoadshow.Entity.DBEntity;
using Beta.AIRoadshow.Entity.Dto;
using Beta.AIRoadshow.Entity.Dto.Request;
using Beta.Framework.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.Application;

[Service(ServiceLifetime.Transient)]
public class PracticeRatingService : BaseService
{
    private const string RatingPromptName = "roadshow_rating_info_llm";
    private const string ContentTypeAi = "ai";
    private const string ContentTypeAiFollowUp = "ai_followup";
    private const string ContentTypeStudent = "student";

    private readonly ActivityRepository _activityRepository;
    private readonly AbilityDimensionRepository _abilityDimensionRepository;
    private readonly LlmService _llmService;
    private readonly ILogger<PracticeRatingService> _logger;
    private readonly PracticeRepository _practiceRepository;
    private readonly PromptConfigService _promptConfigService;

    public PracticeRatingService(
        ActivityRepository activityRepository,
        AbilityDimensionRepository abilityDimensionRepository,
        LlmService llmService,
        ILogger<PracticeRatingService> logger,
        PracticeRepository practiceRepository,
        PromptConfigService promptConfigService)
    {
        _activityRepository = activityRepository;
        _abilityDimensionRepository = abilityDimensionRepository;
        _llmService = llmService;
        _logger = logger;
        _practiceRepository = practiceRepository;
        _promptConfigService = promptConfigService;
    }

    public async Task RateAsync(PracticeRatingTask task, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始评分，PracticeRecordId={PracticeRecordId}, QuestionId={QuestionId}",
            task.PracticeRecordId, task.QuestionId);

        var question = await _activityRepository.SelectQuestionByIdAsync(task.QuestionId);
        if (question == null)
        {
            _logger.LogWarning("评分跳过：未找到题目，QuestionId={QuestionId}", task.QuestionId);
            return;
        }

        var role = await _activityRepository.SelectRoleByIdAsync(question.RoleId);
        var activity = await _activityRepository.SelectActivityByIdAsync(task.ActivityId);
        if (role == null || activity == null)
        {
            _logger.LogWarning("评分跳过：活动上下文不完整，QuestionId={QuestionId}", task.QuestionId);
            return;
        }

        var dialogDetails = await _practiceRepository.SelectDialogDetailsByQuestionAsync(
            task.PracticeRecordId, task.QuestionId);

        var dialogList = dialogDetails?.ToList() ?? new List<PracticeRecordDetailEntity>();
        if (dialogList.Count == 0)
        {
            _logger.LogWarning("评分跳过：无对话记录，QuestionId={QuestionId}", task.QuestionId);
            return;
        }

        var askContent = dialogList
            .FirstOrDefault(x => string.Equals(x.ContentType, ContentTypeAi, StringComparison.OrdinalIgnoreCase))
            ?.DialogContent ?? string.Empty;

        var answerDetail = dialogList
            .FirstOrDefault(x => string.Equals(x.ContentType, ContentTypeStudent, StringComparison.OrdinalIgnoreCase));

        if (answerDetail == null)
        {
            _logger.LogWarning("评分跳过：无学员回答记录，QuestionId={QuestionId}", task.QuestionId);
            return;
        }

        var answerContent = answerDetail.DialogContent ?? string.Empty;

        var followUpBuilder = new StringBuilder();
        var followUpIndex = 0;
        var followUpDetails = dialogList
            .Where(x => string.Equals(x.ContentType, ContentTypeAiFollowUp, StringComparison.OrdinalIgnoreCase) ||
                         (string.Equals(x.ContentType, ContentTypeStudent, StringComparison.OrdinalIgnoreCase) &&
                          x.DetailId != answerDetail.DetailId))
            .OrderBy(x => x.CreateTime);

        foreach (var detail in followUpDetails)
        {
            if (string.Equals(detail.ContentType, ContentTypeAiFollowUp, StringComparison.OrdinalIgnoreCase))
            {
                followUpIndex++;
                followUpBuilder.AppendLine($"【追问{followUpIndex}】{detail.DialogContent}");
            }
            else
            {
                followUpBuilder.AppendLine($"【追问回答{followUpIndex}】{detail.DialogContent}");
            }
        }

        var questionAbilityRels = await _activityRepository.SelectQuestionAbilityRelsAsync(task.QuestionId);
        var relList = questionAbilityRels?.ToList() ?? new List<QuestionAbilityRelEntity>();

        var abilityDimensionInfo = await BuildAbilityDimensionInfoAsync(relList);

        var prompt = GetPromptOrThrow(RatingPromptName);
        var systemPrompt = prompt.system_prompt
            .Replace("{question_info}", BuildQuestionInfo(question))
            .Replace("{role_info}", BuildRoleInfo(role))
            .Replace("{ability_dimension_info}", abilityDimensionInfo)
            .Replace("{ask_content}", askContent)
            .Replace("{answer_content}", answerContent)
            .Replace("{followup_info}", followUpBuilder.ToString().Trim());

        var chatRequest = BuildChatRequest(prompt, systemPrompt);
        var result = await _llmService.ChatCompletionAsync(chatRequest);

        if (result.State != ResultState.Successed)
        {
            _logger.LogError("评分LLM调用失败，QuestionId={QuestionId}，错误：{Error}",
                task.QuestionId, result.ErrorMessage);
            return;
        }

        var content = ExtractMessageContent(JsonConvert.SerializeObject(result.Data));
        var normalizedContent = NormalizeMessageContent(content);

        if (string.IsNullOrWhiteSpace(normalizedContent))
        {
            _logger.LogWarning("评分LLM返回内容为空，QuestionId={QuestionId}", task.QuestionId);
            return;
        }

        RatingResult ratingResult;
        try
        {
            ratingResult = JsonConvert.DeserializeObject<RatingResult>(normalizedContent);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "评分结果JSON解析失败，QuestionId={QuestionId}，原始内容：{Content}",
                task.QuestionId, normalizedContent);
            return;
        }

        if (ratingResult == null)
        {
            _logger.LogWarning("评分结果反序列化为null，QuestionId={QuestionId}", task.QuestionId);
            return;
        }

        await _practiceRepository.UpdatePracticeRecordDetailScoreAsync(
            answerDetail.DetailId,
            ratingResult.Score,
            ratingResult.Summary,
            ratingResult.Strengths != null ? JsonConvert.SerializeObject(ratingResult.Strengths) : null,
            ratingResult.Weaknesses != null ? JsonConvert.SerializeObject(ratingResult.Weaknesses) : null,
            null);

        if (ratingResult.AbilityIndicators != null && ratingResult.AbilityIndicators.Count > 0 && relList.Count > 0)
        {
            var allDimensions = await _abilityDimensionRepository.SelectAbilityDimensionListAsync();
            var dimensionLookup = allDimensions.ToDictionary(d => d.AbilityDimensionId, d => d.AbilityDimensionName);

            var extItems = new List<PracticeRecordDetailExtEntity>();
            foreach (var indicator in ratingResult.AbilityIndicators)
            {
                var matchedRel = relList.FirstOrDefault(r =>
                    dimensionLookup.TryGetValue(r.AbilityDimensionId, out var name) &&
                    string.Equals(name, indicator.IndicatorName, StringComparison.OrdinalIgnoreCase));

                if (matchedRel == null)
                {
                    var dim = allDimensions.FirstOrDefault(d =>
                        string.Equals(d.AbilityDimensionName, indicator.IndicatorName, StringComparison.OrdinalIgnoreCase));
                    if (dim != null)
                    {
                        matchedRel = relList.FirstOrDefault(r => r.AbilityDimensionId == dim.AbilityDimensionId);
                    }
                }

                if (matchedRel != null)
                {
                    extItems.Add(new PracticeRecordDetailExtEntity
                    {
                        DetailId = answerDetail.DetailId,
                        AbilityDimensionId = matchedRel.AbilityDimensionId,
                        Score = indicator.IndicatorScore,
                        ScoreComment = indicator.Evidence
                    });
                }
                else
                {
                    _logger.LogWarning("评分指标未匹配到能力维度，indicator_name={IndicatorName}，QuestionId={QuestionId}",
                        indicator.IndicatorName, task.QuestionId);
                }
            }

            if (extItems.Count > 0)
            {
                await _practiceRepository.InsertPracticeRecordDetailExtBatchAsync(extItems);
            }
        }

        _logger.LogInformation("评分完成，PracticeRecordId={PracticeRecordId}, QuestionId={QuestionId}, Score={Score}",
            task.PracticeRecordId, task.QuestionId, ratingResult.Score);
    }

    private async Task<string> BuildAbilityDimensionInfoAsync(List<QuestionAbilityRelEntity> relList)
    {
        if (relList.Count == 0)
        {
            return "[]";
        }

        var allDimensions = await _abilityDimensionRepository.SelectAbilityDimensionListAsync();
        var items = new List<object>();

        foreach (var rel in relList)
        {
            var dimension = allDimensions.FirstOrDefault(d => d.AbilityDimensionId == rel.AbilityDimensionId);
            if (dimension == null) continue;

            var (levelName, factor) = GetDifficultyInfo(rel.DifficultyLevel);
            items.Add(new
            {
                name = dimension.AbilityDimensionName,
                difficulty_level = levelName,
                difficulty_factor = factor,
                weight = dimension.Weight
            });
        }

        return JsonConvert.SerializeObject(items);
    }

    private static (string levelName, double factor) GetDifficultyInfo(byte difficultyLevel)
    {
        return difficultyLevel switch
        {
            2 => ("中级", 1.1),
            3 => ("高级", 1.2),
            _ => ("初级", 1.0)
        };
    }

    private PromptItemDto GetPromptOrThrow(string promptName)
    {
        var prompt = _promptConfigService.GetByName(promptName);
        if (prompt == null)
        {
            throw new Exception($"未找到提示词配置：{promptName}");
        }

        if (string.IsNullOrWhiteSpace(prompt.system_prompt))
        {
            throw new Exception($"提示词模板内容为空：{promptName}");
        }

        if (string.IsNullOrWhiteSpace(prompt.llm_conf?.model))
        {
            throw new Exception($"提示词缺少模型配置：{promptName}");
        }

        return prompt;
    }

    private static ChatCompletionsRequestDto BuildChatRequest(PromptItemDto prompt, string systemPrompt)
    {
        return new ChatCompletionsRequestDto
        {
            Model = prompt.llm_conf.model,
            Stream = false,
            MaxTokens = prompt.llm_conf.max_tonkens > 0 ? prompt.llm_conf.max_tonkens : null,
            Temperature = prompt.llm_conf.temperature > 0 ? prompt.llm_conf.temperature : null,
            TopP = prompt.llm_conf.top_p > 0 ? prompt.llm_conf.top_p : null,
            Messages = new List<ChatMessageDto>
            {
                new()
                {
                    Role = "system",
                    Content = systemPrompt
                }
            }
        };
    }

    private static string BuildRoleInfo(RoleEntity role)
    {
        return JsonConvert.SerializeObject(new
        {
            roleNickname = role?.RoleNickname ?? string.Empty,
            roleProfile = JoinText(" / ", role?.JobTitle, role?.ProjectRole),
            coreGoal = role?.ProjectRequirement ?? string.Empty,
            description = JoinText(" ; ", role?.Personality, role?.CommunicationStyle)
        });
    }

    private static string BuildQuestionInfo(QuestionBankEntity question)
    {
        return JsonConvert.SerializeObject(new
        {
            questionStem = question?.QuestionStem ?? string.Empty,
            assessmentPoints = question?.AssessmentPoints ?? string.Empty
        });
    }

    private static string JoinText(string separator, params string[] values)
    {
        return string.Join(separator, values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
    }

    private static string ExtractMessageContent(string resultJson)
    {
        if (string.IsNullOrWhiteSpace(resultJson))
        {
            return null;
        }

        try
        {
            var responseToken = Newtonsoft.Json.Linq.JToken.Parse(resultJson);
            return responseToken["choices"]?.FirstOrDefault()?["message"]?["content"]?.ToString();
        }
        catch
        {
            return resultJson;
        }
    }

    private static string NormalizeMessageContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var normalizedContent = content.Trim();
        if (normalizedContent.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLineEndIndex = normalizedContent.IndexOf('\n');
            if (firstLineEndIndex >= 0)
            {
                normalizedContent = normalizedContent[(firstLineEndIndex + 1)..];
            }

            if (normalizedContent.EndsWith("```", StringComparison.Ordinal))
            {
                normalizedContent = normalizedContent[..^3];
            }

            normalizedContent = normalizedContent.Trim();
        }

        return normalizedContent;
    }

    private sealed class RatingResult
    {
        [JsonProperty("score")]
        public decimal Score { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("strengths")]
        public List<string> Strengths { get; set; }

        [JsonProperty("weaknesses")]
        public List<string> Weaknesses { get; set; }

        [JsonProperty("ability_indicators")]
        public List<AbilityIndicator> AbilityIndicators { get; set; }
    }

    private sealed class AbilityIndicator
    {
        [JsonProperty("indicator_name")]
        public string IndicatorName { get; set; }

        [JsonProperty("indicator_score")]
        public decimal IndicatorScore { get; set; }

        [JsonProperty("evidence")]
        public string Evidence { get; set; }
    }
}
