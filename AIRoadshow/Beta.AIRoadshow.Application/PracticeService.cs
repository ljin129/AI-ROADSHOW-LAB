using Beta.AIRoadshow.DataAccess;
using Beta.AIRoadshow.Entity.DBEntity;
using Beta.AIRoadshow.Entity.Dto;
using Beta.AIRoadshow.Entity.Dto.Request;
using Beta.AIRoadshow.Entity.Dto.Response;
using Beta.Framework.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.Application;

[Service(ServiceLifetime.Transient)]
public class PracticeService : BaseService
{
    private const string AskPromptName = "roadshow_ask_info_llm";
    private const string DeciderPromptName = "roadshow_decider_info_llm";
    private const string FollowUpPromptName = "roadshow_followup_info_llm";

    private const string ContentTypeAi = "ai";
    private const string ContentTypeAiFollowUp = "ai_followup";
    private const string ContentTypeStudent = "student";

    private const string PracticeStatusInProgress = "in_progress";
    private const string PracticeStatusCompleted = "completed";
    private const int MaxFollowUpCount = 1;

    private readonly ActivityRepository _activityRepository;
    private readonly GrpcService _grpcService;
    private readonly HttpContextService _httpContextService;
    private readonly LlmService _llmService;
    private readonly ILogger<PracticeService> _logger;
    private readonly PracticeRepository _practiceRepository;
    private readonly PromptConfigService _promptConfigService;
    private readonly PracticeRatingQueueService _ratingQueue;
    private readonly CustomerService _customerService;

    public PracticeService(
        ActivityRepository activityRepository,
        CustomerService customerService,
        GrpcService grpcService,
        HttpContextService httpContextService,
        LlmService llmService,
        ILogger<PracticeService> logger,
        PracticeRepository practiceRepository,
        PromptConfigService promptConfigService,
        PracticeRatingQueueService ratingQueue)
    {
        _activityRepository = activityRepository;
        _customerService = customerService;
        _grpcService = grpcService;
        _httpContextService = httpContextService;
        _llmService = llmService;
        _logger = logger;
        _practiceRepository = practiceRepository;
        _promptConfigService = promptConfigService;
        _ratingQueue = ratingQueue;
    }

    public async Task<ResponseInfo<PracticeStartResponseDto>> StartPracticeAsync(PracticeStartRequestDto request)
    {
        if (request == null || request.ActivityId <= 0)
        {
            return Failed<PracticeStartResponseDto>("活动ID不能为空。");
        }

        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            return Failed<PracticeStartResponseDto>("当前用户信息无效。");
        }

        var activity = await _activityRepository.SelectActivityAsync(request.ActivityId, currentUser.CustCompanyId);
        if (activity == null)
        {
            return Failed<PracticeStartResponseDto>("未找到对应的活动信息。");
        }

        var existingRecord = await _practiceRepository.SelectInProgressRecordAsync(currentUser.UserId, request.ActivityId);
        if (existingRecord != null)
        {
            return new ResponseInfo<PracticeStartResponseDto>
            {
                State = ResultState.Successed,
                Data = new PracticeStartResponseDto
                {
                    PracticeRecordId = existingRecord.PracticeRecordId,
                    PracticeStatus = existingRecord.PracticeStatus,
                    RoadshowMaterialId = existingRecord.RoadshowMaterialId
                }
            };
        }

        var now = DateTime.Now;
        var record = new PracticeRecordEntity
        {
            PracticeRecordId = await _grpcService.GenSerialIdAsync(),
            UserId = currentUser.UserId,
            ActivityId = request.ActivityId,
            RoadshowMaterialId = request.RoadshowMaterialId,
            PracticeStatus = PracticeStatusInProgress,
            CreateTime = now,
            UpdateTime = now
        };

        var count = await _practiceRepository.InsertPracticeRecordAsync(record);
        if (count <= 0)
        {
            return Failed<PracticeStartResponseDto>("创建演练记录失败。");
        }

        return new ResponseInfo<PracticeStartResponseDto>
        {
            State = ResultState.Successed,
            Data = new PracticeStartResponseDto
            {
                PracticeRecordId = record.PracticeRecordId,
                PracticeStatus = record.PracticeStatus,
                RoadshowMaterialId = record.RoadshowMaterialId
            }
        };
    }

    public async Task<ResponseInfo<PracticeDetailResponseDto>> GetPracticeDetailAsync(long practiceRecordId)
    {
        if (practiceRecordId <= 0)
        {
            return Failed<PracticeDetailResponseDto>("演练记录ID不能为空。");
        }

        var record = await _practiceRepository.SelectPracticeRecordByIdAsync(practiceRecordId);
        if (record == null)
        {
            return Failed<PracticeDetailResponseDto>("未找到对应的演练记录。");
        }

        var details = await _practiceRepository.SelectPracticeRecordDetailsAsync(practiceRecordId);
        var detailList = details?.ToList() ?? new List<PracticeRecordDetailEntity>();
        var roleContextMap = await BuildDetailRoleContextMapAsync(record.ActivityId, detailList);

        var result = new PracticeDetailResponseDto
        {
            PracticeRecordId = record.PracticeRecordId,
            ActivityId = record.ActivityId,
            PracticeStatus = record.PracticeStatus,
            Score = record.Score,
            ScoreComment = record.ScoreComment,
            Details = detailList.Select(d => new PracticeDetailItemDto
            {
                DetailId = d.DetailId,
                ContentType = d.ContentType,
                QuestionId = d.QuestionId,
                RoleId = roleContextMap.TryGetValue(d.DetailId, out var roleContext) ? roleContext.RoleId : null,
                RoleNickname = roleContext?.RoleNickname,
                CustId = roleContext?.CustId,
                PhotoUrl = roleContext?.PhotoUrl,
                Content = d.DialogContent,
                DialogVoiceFileId = d.DialogVoiceFileId,
                IsFollowUp = string.Equals(d.ContentType, ContentTypeAiFollowUp, StringComparison.OrdinalIgnoreCase),
                Score = d.Score,
                ScoreComment = d.ScoreComment,
                Strengths = d.Strengths,
                Weaknesses = d.Weaknesses,
                CreateTime = d.CreateTime
            }).ToList()
        };

        return new ResponseInfo<PracticeDetailResponseDto>
        {
            State = ResultState.Successed,
            Data = result
        };
    }

    public async Task PracticeChatStreamAsync(
        PracticeChatRequestDto request,
        Func<string, object, Task> emitAsync,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (emitAsync == null)
            {
                throw new ArgumentNullException(nameof(emitAsync));
            }

            if (request == null || request.ActivityId <= 0)
            {
                await emitAsync("error", new { message = "活动ID不能为空。" });
                return;
            }

            if (request.PracticeRecordId <= 0)
            {
                await emitAsync("error", new { message = "演练记录ID不能为空。" });
                return;
            }

            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                await emitAsync("error", new { message = "当前用户信息无效。" });
                return;
            }

            var practiceRecord = await _practiceRepository.SelectPracticeRecordByIdAsync(request.PracticeRecordId);
            if (!IsValidPracticeRecord(practiceRecord, request.ActivityId, currentUser.UserId))
            {
                await emitAsync("error", new { message = "未找到对应的演练记录，或当前用户无权访问。" });
                return;
            }

            if (string.Equals(practiceRecord.PracticeStatus, PracticeStatusCompleted, StringComparison.OrdinalIgnoreCase))
            {
                await emitAsync("message", BuildCompletedMessage(practiceRecord));
                await emitAsync("done", new { practiceStatus = PracticeStatusCompleted });
                return;
            }

            if (string.IsNullOrWhiteSpace(request.AnswerContent))
            {
                if (await ProcessAskAsync(practiceRecord, null, null, request.AiReplyVoiceFileId, currentUser, emitAsync, cancellationToken))
                {
                    await emitAsync("done", new { practiceStatus = practiceRecord.PracticeStatus });
                }

                return;
            }

            if (!request.CurrentDetailId.HasValue || request.CurrentDetailId.Value <= 0)
            {
                await emitAsync("error", new { message = "回答问题时，当前提问明细ID不能为空。" });
                return;
            }

            if (await ProcessAnswerAsync(practiceRecord, request, currentUser, emitAsync, cancellationToken))
            {
                await emitAsync("done", new { practiceStatus = practiceRecord.PracticeStatus });
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("实时路演对话流已取消，practiceRecordId={PracticeRecordId}", request?.PracticeRecordId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "实时路演对话流处理失败，practiceRecordId={PracticeRecordId}", request?.PracticeRecordId);
            if (emitAsync != null)
            {
                await emitAsync("error", new { message = ex.Message });
            }
        }
    }

    private async Task<bool> ProcessAnswerAsync(
        PracticeRecordEntity practiceRecord,
        PracticeChatRequestDto request,
        CurrentUserContext currentUser,
        Func<string, object, Task> emitAsync,
        CancellationToken cancellationToken)
    {
        var askDetail = await _practiceRepository.SelectPracticeRecordDetailByIdAsync(request.CurrentDetailId!.Value);
        if (askDetail == null ||
            askDetail.PracticeRecordId != practiceRecord.PracticeRecordId ||
            !IsAiAskContentType(askDetail.ContentType))
        {
            await emitAsync("error", new { message = "未找到当前对应的AI提问记录。" });
            return false;
        }

        if (!askDetail.QuestionId.HasValue || askDetail.QuestionId.Value <= 0)
        {
            await emitAsync("error", new { message = "当前提问记录缺少题目ID。" });
            return false;
        }

        var question = await _activityRepository.SelectQuestionByIdAsync(askDetail.QuestionId.Value);
        if (question == null || question.ActivityId != practiceRecord.ActivityId)
        {
            await emitAsync("error", new { message = "未找到对应的题目信息。" });
            return false;
        }

        var role = await _activityRepository.SelectRoleByIdAsync(question.RoleId);
        var stage = await _activityRepository.SelectBusinessStageByIdAsync(question.StageId);
        var activity = await _activityRepository.SelectActivityAsync(practiceRecord.ActivityId, currentUser.CustCompanyId);
        if (role == null || stage == null || activity == null)
        {
            await emitAsync("error", new { message = "活动上下文信息不完整。" });
            return false;
        }

        var existingAiReply = await _practiceRepository.SelectNextAiReplyAfterDetailAsync(
            practiceRecord.PracticeRecordId, askDetail.DetailId);
        if (existingAiReply != null)
        {
            var roleProfile = await BuildRoleProfileAsync(practiceRecord, question, role, stage);
            await emitAsync("role_profile", roleProfile);
            var isFollowUp = string.Equals(existingAiReply.ContentType, ContentTypeAiFollowUp, StringComparison.OrdinalIgnoreCase);
            await emitAsync("message", new PracticeChatMessageResponseDto
            {
                DetailId = existingAiReply.DetailId,
                PracticeRecordId = practiceRecord.PracticeRecordId,
                ActivityId = practiceRecord.ActivityId,
                QuestionId = existingAiReply.QuestionId,
                RoleId = role.RoleId,
                RoleNickname = role.RoleNickname,
                CustId = roleProfile.CustId,
                PhotoUrl = roleProfile.PhotoUrl,
                StageId = stage.StageId,
                StageName = stage.StageName,
                ContentType = existingAiReply.ContentType,
                IsFollowUp = isFollowUp,
                Content = existingAiReply.DialogContent,
                DialogVoiceFileId = existingAiReply.DialogVoiceFileId,
                PracticeStatus = practiceRecord.PracticeStatus,
                IsCompleted = false
            });
            return true;
        }

        var now = DateTime.Now;
        var answerDetail = new PracticeRecordDetailEntity
        {
            DetailId = await _grpcService.GenSerialIdAsync(),
            PracticeRecordId = practiceRecord.PracticeRecordId,
            UserId = currentUser.UserId,
            ActivityId = practiceRecord.ActivityId,
            DialogContent = request.AnswerContent?.Trim(),
            DialogVoiceFileId = request.AnswerVoiceFileId,
            ContentType = ContentTypeStudent,
            QuestionId = question.QuestionId,
            CreateTime = now,
            UpdateTime = now
        };

        await _practiceRepository.InsertPracticeRecordDetailAsync(answerDetail);

        var followUpCount = await _practiceRepository.CountFollowUpAsksAsync(practiceRecord.PracticeRecordId, question.QuestionId);
        await emitAsync("status", new
        {
            step = "decide_followup",
            questionId = question.QuestionId,
            followUpCount
        });

        var decision = await DecideFollowUpAsync(activity, role, question, askDetail.DialogContent, answerDetail.DialogContent, followUpCount);
        if (decision == null)
        {
            throw new Exception("解析追问决策结果失败。");
        }

        if (decision.NeedFollowUp && followUpCount < MaxFollowUpCount)
        {
            await emitAsync("status", new
            {
                step = "generate_followup",
                questionId = question.QuestionId
            });

            await GenerateAndStoreAskAsync(
                practiceRecord,
                currentUser,
                activity,
                question,
                role,
                stage,
                await BuildRoleProfileAsync(practiceRecord, question, role, stage),
                true,
                BuildFollowUpPrompt(
                    prompt: GetPromptOrThrow(FollowUpPromptName),
                    activity,
                    role,
                    question,
                    askDetail.DialogContent,
                    answerDetail.DialogContent,
                    decision,
                    followUpCount),
                request.AiReplyVoiceFileId,
                emitAsync,
                cancellationToken);

            return true;
        }

        return await ProcessAskAsync(practiceRecord, stage.StageId, question.QuestionId, request.AiReplyVoiceFileId, currentUser, emitAsync, cancellationToken);
    }

    private async Task<bool> ProcessAskAsync(
        PracticeRecordEntity practiceRecord,
        long? previousStageId,
        long? previousQuestionId,
        string aiReplyVoiceFileId,
        CurrentUserContext currentUser,
        Func<string, object, Task> emitAsync,
        CancellationToken cancellationToken)
    {
        if (previousQuestionId.HasValue && previousQuestionId.Value > 0)
        {
            _ = _ratingQueue.EnqueueAsync(new PracticeRatingTask
            {
                PracticeRecordId = practiceRecord.PracticeRecordId,
                ActivityId = practiceRecord.ActivityId,
                QuestionId = previousQuestionId.Value,
                UserId = currentUser.UserId
            });
        }

        var activity = await _activityRepository.SelectActivityAsync(practiceRecord.ActivityId, currentUser.CustCompanyId);
        if (activity == null)
        {
            await emitAsync("error", new { message = "未找到对应的活动信息。" });
            return false;
        }

        var selection = await SelectNextQuestionAsync(practiceRecord.PracticeRecordId, practiceRecord.ActivityId);
        if (selection == null)
        {
            practiceRecord.PracticeStatus = PracticeStatusCompleted;
            await _practiceRepository.UpdatePracticeRecordStatusAsync(practiceRecord.PracticeRecordId, PracticeStatusCompleted, DateTime.Now);
            await emitAsync("status", new
            {
                step = "completed",
                practiceRecordId = practiceRecord.PracticeRecordId
            });
            await emitAsync("message", BuildCompletedMessage(practiceRecord));
            return true;
        }

        if (previousStageId.HasValue && previousStageId.Value != selection.Stage.StageId)
        {
            await emitAsync("status", new
            {
                step = "switch_stage",
                stageId = selection.Stage.StageId,
                stageName = selection.Stage.StageName
            });
        }

        await emitAsync("status", new
        {
            step = "select_question",
            questionId = selection.Question.QuestionId,
            stageId = selection.Stage.StageId,
            stageName = selection.Stage.StageName
        });

        var roleProfile = await BuildRoleProfileAsync(practiceRecord, selection.Question, selection.Role, selection.Stage);
        await emitAsync("role_profile", roleProfile);

        var prompt = GetPromptOrThrow(AskPromptName);
        var systemPrompt = BuildAskPrompt(prompt, activity, selection.Role, selection.Question);
        await GenerateAndStoreAskAsync(
            practiceRecord,
            currentUser,
            activity,
            selection.Question,
            selection.Role,
            selection.Stage,
            roleProfile,
            false,
            systemPrompt,
            aiReplyVoiceFileId,
            emitAsync,
            cancellationToken);

        return true;
    }

    private async Task GenerateAndStoreAskAsync(
        PracticeRecordEntity practiceRecord,
        CurrentUserContext currentUser,
        ActivityEntity activity,
        QuestionBankEntity question,
        RoleEntity role,
        BusinessStageEntity stage,
        PracticeRoleProfileResponseDto roleProfile,
        bool isFollowUp,
        string systemPrompt,
        string aiReplyVoiceFileId,
        Func<string, object, Task> emitAsync,
        CancellationToken cancellationToken)
    {
        var prompt = isFollowUp ? GetPromptOrThrow(FollowUpPromptName) : GetPromptOrThrow(AskPromptName);
        var chatRequest = BuildChatRequest(prompt, systemPrompt, true);
        var builder = new StringBuilder();
        var result = await _llmService.ChatCompletionStreamAsync(chatRequest, async delta =>
        {
            if (string.IsNullOrEmpty(delta))
            {
                return;
            }

            builder.Append(delta);
            await emitAsync("delta", new
            {
                content = delta,
                isFollowUp
            });
        }, cancellationToken);

        if (result.State != ResultState.Successed)
        {
            throw new Exception(result.ErrorMessage ?? "调用大模型生成内容失败。");
        }

        var content = NormalizeMessageContent(result.Data) ?? builder.ToString().Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new Exception("大模型返回内容为空。");
        }

        var now = DateTime.Now;
        var detail = new PracticeRecordDetailEntity
        {
            DetailId = await _grpcService.GenSerialIdAsync(),
            PracticeRecordId = practiceRecord.PracticeRecordId,
            UserId = currentUser.UserId,
            ActivityId = practiceRecord.ActivityId,
            DialogContent = content,
            DialogVoiceFileId = aiReplyVoiceFileId,
            ContentType = isFollowUp ? ContentTypeAiFollowUp : ContentTypeAi,
            QuestionId = question.QuestionId,
            CreateTime = now,
            UpdateTime = now
        };

        await _practiceRepository.InsertPracticeRecordDetailAsync(detail);
        practiceRecord.PracticeStatus = PracticeStatusInProgress;

        await emitAsync("message", new PracticeChatMessageResponseDto
        {
            DetailId = detail.DetailId,
            PracticeRecordId = practiceRecord.PracticeRecordId,
            ActivityId = practiceRecord.ActivityId,
            QuestionId = question.QuestionId,
            RoleId = role.RoleId,
            RoleNickname = role.RoleNickname,
            CustId = roleProfile?.CustId,
            PhotoUrl = roleProfile?.PhotoUrl,
            StageId = stage.StageId,
            StageName = stage.StageName,
            ContentType = detail.ContentType,
            IsFollowUp = isFollowUp,
            Content = content,
            DialogVoiceFileId = detail.DialogVoiceFileId,
            PracticeStatus = practiceRecord.PracticeStatus,
            IsCompleted = false
        });
    }

    private async Task<QuestionSelectionContext> SelectNextQuestionAsync(long practiceRecordId, long activityId)
    {
        var stages = (await _activityRepository.SelectActivityBusinessStagesAsync(activityId))
            .OrderBy(x => x.SortNo)
            .ThenBy(x => x.CreateTime)
            .ThenBy(x => x.StageId)
            .ToList();

        if (stages.Count == 0)
        {
            return null;
        }

        var askedQuestionIds = (await _practiceRepository.SelectPracticeQuestionIdsAsync(practiceRecordId))
            .Distinct()
            .ToList();

        foreach (var stage in stages)
        {
            if (stage.QuestionCount <= 0)
            {
                continue;
            }

            var askedCount = await _practiceRepository.CountPracticeQuestionsByStageAsync(practiceRecordId, stage.StageId);
            if (askedCount >= stage.QuestionCount)
            {
                continue;
            }

            var candidates = (await _practiceRepository.SelectAvailableQuestionsByStageAsync(activityId, stage.StageId, askedQuestionIds)).ToList();
            if (candidates.Count == 0)
            {
                continue;
            }

            var selectedQuestion = candidates.FirstOrDefault(x => x.IsRequired);
            if (selectedQuestion == null)
            {
                var optionalQuestions = candidates.Where(x => !x.IsRequired).ToList();
                if (optionalQuestions.Count == 0)
                {
                    continue;
                }

                selectedQuestion = optionalQuestions[Random.Shared.Next(optionalQuestions.Count)];
            }

            var role = await _activityRepository.SelectRoleByIdAsync(selectedQuestion.RoleId);
            if (role == null)
            {
                continue;
            }

            return new QuestionSelectionContext
            {
                Stage = stage,
                Question = selectedQuestion,
                Role = role
            };
        }

        return null;
    }

    private async Task<FollowUpDecision> DecideFollowUpAsync(
        ActivityEntity activity,
        RoleEntity role,
        QuestionBankEntity question,
        string askContent,
        string answerContent,
        int followUpCount)
    {
        var prompt = GetPromptOrThrow(DeciderPromptName);
        var systemPrompt = prompt.system_prompt
            .Replace("{role_info}", BuildRoleInfo(role))
            .Replace("{activity_info}", BuildActivityInfo(activity))
            .Replace("{question_info}", BuildQuestionInfo(question))
            .Replace("{ask_info}", askContent ?? string.Empty)
            .Replace("{answer_info}", answerContent ?? string.Empty)
            .Replace("{ask_count}", followUpCount.ToString())
            .Replace("{max_ask_count}", MaxFollowUpCount.ToString());

        var result = await _llmService.ChatCompletionAsync(BuildChatRequest(prompt, systemPrompt, false));
        if (result.State != ResultState.Successed)
        {
            throw new Exception(result.ErrorMessage ?? "调用大模型进行追问决策失败。");
        }

        var content = ExtractMessageContent(JsonConvert.SerializeObject(result.Data));
        var normalizedContent = NormalizeMessageContent(content);
        if (string.IsNullOrWhiteSpace(normalizedContent))
        {
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<FollowUpDecision>(normalizedContent);
        }
        catch (JsonException)
        {
            return null;
        }
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

    private static ChatCompletionsRequestDto BuildChatRequest(PromptItemDto prompt, string systemPrompt, bool stream)
    {
        return new ChatCompletionsRequestDto
        {
            Model = prompt.llm_conf.model,
            Stream = stream,
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

    private static string BuildAskPrompt(PromptItemDto prompt, ActivityEntity activity, RoleEntity role, QuestionBankEntity question)
    {
        return prompt.system_prompt
            .Replace("{role_info}", BuildRoleInfo(role))
            .Replace("{activity_info}", BuildActivityInfo(activity))
            .Replace("{question_info}", BuildQuestionInfo(question));
    }

    private static string BuildFollowUpPrompt(
        PromptItemDto prompt,
        ActivityEntity activity,
        RoleEntity role,
        QuestionBankEntity question,
        string askContent,
        string answerContent,
        FollowUpDecision decision,
        int followUpCount)
    {
        return prompt.system_prompt
            .Replace("{role_info}", BuildRoleInfo(role))
            .Replace("{activity_info}", BuildActivityInfo(activity))
            .Replace("{question_info}", BuildQuestionInfo(question))
            .Replace("{ask_info}", askContent ?? string.Empty)
            .Replace("{answer_info}", answerContent ?? string.Empty)
            .Replace("{followiup_info}", decision?.FollowUpDirection ?? string.Empty)
            .Replace("{followup_info}", decision?.FollowUpDirection ?? string.Empty)
            .Replace("{reason_info}", decision?.Reason ?? string.Empty)
            .Replace("{pressurelevel_info}", decision?.PressureLevel ?? string.Empty)
            .Replace("{pressure_level_info}", decision?.PressureLevel ?? string.Empty)
            .Replace("{ask_count}", followUpCount.ToString())
            .Replace("{max_ask_count}", MaxFollowUpCount.ToString());
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

    private static string BuildActivityInfo(ActivityEntity activity)
    {
        return JsonConvert.SerializeObject(new
        {
            activityDesc = activity?.ActivityDesc ?? string.Empty,
            customerBackground = activity?.CustomerBackground ?? string.Empty
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

        var responseToken = JToken.Parse(resultJson);
        return responseToken["choices"]?.FirstOrDefault()?["message"]?["content"]?.ToString();
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

    private static bool IsAiAskContentType(string contentType)
    {
        return string.Equals(contentType, ContentTypeAi, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(contentType, ContentTypeAiFollowUp, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<long, DetailRoleContext>> BuildDetailRoleContextMapAsync(
        long activityId,
        IEnumerable<PracticeRecordDetailEntity> details)
    {
        var detailList = details?.ToList() ?? new List<PracticeRecordDetailEntity>();
        var questionIds = detailList
            .Where(x => x.QuestionId.HasValue && x.QuestionId.Value > 0)
            .Select(x => x.QuestionId!.Value)
            .Distinct()
            .ToArray();
        if (questionIds.Length == 0)
        {
            return new Dictionary<long, DetailRoleContext>();
        }

        var questionList = (await _activityRepository.SelectQuestionsByIdsAsync(questionIds)).ToList();
        var questionMap = questionList.ToDictionary(x => x.QuestionId, x => x);
        var roleMap = (await _activityRepository.SelectActivityRolesAsync(activityId))
            .GroupBy(x => x.RoleId)
            .ToDictionary(x => x.Key, x => x.First());
        var customerMap = await _customerService.GetCustomerMapByCustIdsAsync(
            roleMap.Values
                .Where(x => x.CustomerId.HasValue && x.CustomerId.Value > 0)
                .Select(x => x.CustomerId!.Value));

        var result = new Dictionary<long, DetailRoleContext>();
        foreach (var detail in detailList)
        {
            if (!detail.QuestionId.HasValue ||
                !questionMap.TryGetValue(detail.QuestionId.Value, out var question) ||
                !roleMap.TryGetValue(question.RoleId, out var role))
            {
                continue;
            }

            customerMap.TryGetValue(role.CustomerId ?? 0, out var customer);
            result[detail.DetailId] = new DetailRoleContext
            {
                RoleId = role.RoleId,
                RoleNickname = role.RoleNickname,
                CustId = customer?.CustId ?? role.CustomerId,
                PhotoUrl = customer?.PhotoUrl ?? string.Empty
            };
        }

        return result;
    }

    private async Task<PracticeRoleProfileResponseDto> BuildRoleProfileAsync(
        PracticeRecordEntity practiceRecord,
        QuestionBankEntity question,
        RoleEntity role,
        BusinessStageEntity stage)
    {
        CustomerResponseDto customer = null;
        if (role?.CustomerId > 0)
        {
            customer = await _customerService.GetCustomerByCustIdAsync(role.CustomerId.Value);
        }

        return new PracticeRoleProfileResponseDto
        {
            ActivityId = practiceRecord?.ActivityId ?? 0,
            PracticeRecordId = practiceRecord?.PracticeRecordId ?? 0,
            QuestionId = question?.QuestionId,
            RoleId = role?.RoleId,
            RoleNickname = role?.RoleNickname,
            CustId = customer?.CustId ?? role?.CustomerId,
            PhotoUrl = customer?.PhotoUrl ?? string.Empty,
            StageId = stage?.StageId,
            StageName = stage?.StageName
        };
    }

    private static PracticeChatMessageResponseDto BuildCompletedMessage(PracticeRecordEntity practiceRecord)
    {
        return new PracticeChatMessageResponseDto
        {
            PracticeRecordId = practiceRecord.PracticeRecordId,
            ActivityId = practiceRecord.ActivityId,
            ContentType = ContentTypeAi,
            Content = "本次路演演练已完成。",
            DialogVoiceFileId = null,
            PracticeStatus = PracticeStatusCompleted,
            IsCompleted = true
        };
    }

    private static bool IsValidPracticeRecord(PracticeRecordEntity practiceRecord, long activityId, long userId)
    {
        return practiceRecord != null &&
               practiceRecord.ActivityId == activityId &&
               practiceRecord.UserId == userId;
    }

    private async Task<CurrentUserContext> GetCurrentUserAsync()
    {
        var betaUnionId = _httpContextService.GetBetaUnionId();
        if (betaUnionId <= 0)
        {
            return null;
        }

        var userInfo = await _grpcService.GetUserInfoByUnionIDAsync(betaUnionId);
        if (userInfo == null || userInfo.InternalUserID <= 0 || userInfo.CustCompanyID <= 0)
        {
            return null;
        }

        return new CurrentUserContext
        {
            UserId = userInfo.InternalUserID,
            CustCompanyId = userInfo.CustCompanyID
        };
    }

    private static ResponseInfo<T> Failed<T>(string message)
    {
        return new ResponseInfo<T>
        {
            State = ResultState.Failed,
            ErrorMessage = message
        };
    }

    private sealed class CurrentUserContext
    {
        public long UserId { get; set; }

        public int CustCompanyId { get; set; }
    }

    private sealed class QuestionSelectionContext
    {
        public QuestionBankEntity Question { get; set; }

        public RoleEntity Role { get; set; }

        public BusinessStageEntity Stage { get; set; }
    }

    private sealed class FollowUpDecision
    {
        [JsonProperty("needFollowUp")]
        public bool NeedFollowUp { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("followUpDirection")]
        public string FollowUpDirection { get; set; }

        [JsonProperty("pressureLevel")]
        public string PressureLevel { get; set; }
    }

    private sealed class DetailRoleContext
    {
        public long RoleId { get; set; }

        public string RoleNickname { get; set; }

        public int? CustId { get; set; }

        public string PhotoUrl { get; set; }
    }
}
