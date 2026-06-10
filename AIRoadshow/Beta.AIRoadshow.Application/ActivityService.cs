using Beta.AIRoadshow.DataAccess;
using Beta.AIRoadshow.Entity.DBEntity;
using Beta.AIRoadshow.Entity.Dto;
using Beta.AIRoadshow.Entity.Dto.Request;
using Beta.AIRoadshow.Entity.Dto.Response;
using Beta.Framework.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.Application;

/// <summary>
/// 【服务】路演活动
/// </summary>
[Service(ServiceLifetime.Transient)]
public class ActivityService : BaseService
{
    private const string ActivityExtractTaskCachePrefix = "roadshow:activity_extract:";
    private const string QuestionExtractTaskCachePrefix = "roadshow:question_extract:";
    private const string QuestionExtractPromptName = "roadshow_question_list_llm";

    private readonly HttpContextService _httpContextService;
    private readonly ActivityRepository _activityRepository;
    private readonly AbilityDimensionRepository _abilityDimensionRepository;
    private readonly CacheService _cacheService;
    private readonly GrpcService _grpcService;
    private readonly LlmService _llmService;
    private readonly ILogger<ActivityService> _logger;
    private readonly PromptConfigService _promptConfigService;
    private readonly IConfiguration _configuration;
    private readonly CustomerService _customerService;

    public ActivityService(
        HttpContextService httpContextService,
        ActivityRepository activityRepository,
        AbilityDimensionRepository abilityDimensionRepository,
        CacheService cacheService,
        GrpcService grpcService,
        LlmService llmService,
        ILogger<ActivityService> logger,
        PromptConfigService promptConfigService,
        IConfiguration configuration,
        CustomerService customerService)
    {
        _httpContextService = httpContextService;
        _activityRepository = activityRepository;
        _abilityDimensionRepository = abilityDimensionRepository;
        _cacheService = cacheService;
        _grpcService = grpcService;
        _llmService = llmService;
        _logger = logger;
        _promptConfigService = promptConfigService;
        _configuration = configuration;
        _customerService = customerService;
    }

    /// <summary>
    /// 查询活动分页列表
    /// </summary>
    public async Task<ResponseInfo<ActivityListResponseDto>> GetActivityListAsync(ActivityListRequestDto request)
    {
        request ??= new ActivityListRequestDto();
        var (pageIndex, pageSize) = NormalizePaging(request.PageIndex, request.PageSize);
        request.PageIndex = pageIndex;
        request.PageSize = pageSize;

        var custCompanyId = GetCustCompanyIdOrNull();
        var (totalCount, items) = await _activityRepository.SelectActivityPageAsync(request, custCompanyId);
        var listItems = items
            .MapToList<ActivityListItemResponseDto>()
            .Select(x =>
            {
                x.CoverImage = BuildActivityExtractFileUrl(x.CoverImage);
                return x;
            })
            .ToList();

        return new ResponseInfo<ActivityListResponseDto>
        {
            State = ResultState.Successed,
            Data = new ActivityListResponseDto
            {
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                Items = listItems
            }
        };
    }

    /// <summary>
    /// 查询当前机构下已发布活动分页列表
    /// </summary>
    public async Task<ResponseInfo<ActivityListResponseDto>> GetPublishedActivityListAsync(ActivityListRequestDto request)
    {
        var custCompanyId = GetCustCompanyIdOrNull();
        if (!custCompanyId.HasValue || custCompanyId.Value <= 0)
        {
            return Failed<ActivityListResponseDto>("所属机构不能为空");
        }

        request ??= new ActivityListRequestDto();
        var (pageIndex, pageSize) = NormalizePaging(request.PageIndex, request.PageSize);
        request.PageIndex = pageIndex;
        request.PageSize = pageSize;
        request.PublishStatus = 1;

        var (totalCount, items) = await _activityRepository.SelectPublishedActivityPageAsync(request, custCompanyId.Value);
        var listItems = items
            .MapToList<ActivityListItemResponseDto>()
            .Select(x =>
            {
                x.CoverImage = BuildActivityExtractFileUrl(x.CoverImage);
                return x;
            })
            .ToList();

        return new ResponseInfo<ActivityListResponseDto>
        {
            State = ResultState.Successed,
            Data = new ActivityListResponseDto
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = listItems
            }
        };
    }

    /// <summary>
    /// 查询活动统计
    /// </summary>
    public async Task<ResponseInfo<ActivityStatisticsResponseDto>> GetActivityStatisticsAsync()
    {
        var custCompanyId = GetCustCompanyIdOrNull();
        var statistics = await _activityRepository.SelectActivityStatisticsAsync(custCompanyId) ?? new ActivityStatisticsResponseDto();
        return new ResponseInfo<ActivityStatisticsResponseDto>
        {
            State = ResultState.Successed,
            Data = statistics
        };
    }

    /// <summary>
    /// 查询活动详情
    /// </summary>
    public async Task<ResponseInfo<ActivityDetailResponseDto>> GetActivityDetailAsync(long activityId)
    {
        if (activityId <= 0)
        {
            return new ResponseInfo<ActivityDetailResponseDto>
            {
                State = ResultState.Failed,
                ErrorMessage = "活动ID不能为空"
            };
        }

        var custCompanyId = GetCustCompanyIdOrNull();
        if (!custCompanyId.HasValue || custCompanyId.Value <= 0)
        {
            return new ResponseInfo<ActivityDetailResponseDto>
            {
                State = ResultState.Failed,
                ErrorMessage = "所属机构不能为空"
            };
        }

        var detail = await _activityRepository.SelectActivityDetailAsync(activityId, custCompanyId.Value);
        if (detail?.Activity == null)
        {
            return new ResponseInfo<ActivityDetailResponseDto>
            {
                State = ResultState.Failed,
                ErrorMessage = "活动不存在"
            };
        }

        var roleDtos = detail.Roles.MapToList<ActivityDetailRoleResponseDto>().ToList();

        var customerIds = roleDtos
            .Where(x => x.CustomerId.HasValue && x.CustomerId.Value > 0)
            .Select(x => x.CustomerId.Value)
            .Distinct()
            .ToArray();

        if (customerIds.Length > 0)
        {
            var customerMap = await _customerService.GetCustomerMapByCustIdsAsync(customerIds);
            foreach (var role in roleDtos)
            {
                if (role.CustomerId.HasValue && customerMap.TryGetValue(role.CustomerId.Value, out var customer))
                {
                    role.PhotoUrl = customer.PhotoUrl ?? string.Empty;
                }
            }
        }

        var response = new ActivityDetailResponseDto
        {
            Activity = detail.Activity.MapTo<ActivityDetailActivityResponseDto>(),
            Roles = roleDtos,
            BusinessStages = detail.BusinessStages.MapToList<ActivityDetailBusinessStageResponseDto>().ToList()
        };

        return new ResponseInfo<ActivityDetailResponseDto>
        {
            State = ResultState.Successed,
            Data = response
        };
    }

    /// <summary>
    /// 查询活动题库分页
    /// </summary>
    public async Task<ResponseInfo<ActivityQuestionPageResponseDto>> GetActivityQuestionPageAsync(ActivityQuestionPageRequestDto request)
    {
        if (request == null || request.ActivityId <= 0)
        {
            return Failed<ActivityQuestionPageResponseDto>("活动ID不能为空");
        }

        if (request.StageId.HasValue && request.StageId.Value <= 0)
        {
            return Failed<ActivityQuestionPageResponseDto>("业务环节ID无效");
        }

        return await WithOwnedActivityAsync(request.ActivityId, async _ =>
        {
            var (pageIndex, pageSize) = NormalizePaging(request.PageIndex, request.PageSize);
            request.PageIndex = pageIndex;
            request.PageSize = pageSize;

            var (totalCount, items) = await _activityRepository.SelectActivityQuestionPageAsync(request);
            return new ResponseInfo<ActivityQuestionPageResponseDto>
            {
                State = ResultState.Successed,
                Data = new ActivityQuestionPageResponseDto
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    Items = items?.ToList() ?? new List<ActivityQuestionItemResponseDto>()
                }
            };
        });
    }

    /// <summary>
    /// 查询活动业务环节列表
    /// </summary>
    public async Task<ResponseInfo<IEnumerable<ActivityBusinessStageResponseDto>>> GetActivityBusinessStageListAsync(long activityId)
    {
        if (activityId <= 0)
        {
            return Failed<IEnumerable<ActivityBusinessStageResponseDto>>("活动ID不能为空");
        }

        return await WithOwnedActivityAsync(activityId, async _ =>
        {
            var stageList = (await _activityRepository.SelectActivityBusinessStagesAsync(activityId))
                .MapToList<ActivityBusinessStageResponseDto>()
                .ToList();

            return new ResponseInfo<IEnumerable<ActivityBusinessStageResponseDto>>
            {
                State = ResultState.Successed,
                Data = stageList
            };
        });
    }

    /// <summary>
    /// 更新活动主表信息
    /// </summary>
    public async Task<ResponseInfo<bool>> UpdateActivityAsync(ActivityUpdateRequestDto request)
    {
        if (request == null || request.ActivityId <= 0)
        {
            return Failed<bool>("活动ID不能为空", false);
        }

        if (string.IsNullOrWhiteSpace(request.ActivityName))
        {
            return Failed<bool>("活动名称不能为空", false);
        }

        if (request.EndTime <= request.StartTime)
        {
            return Failed<bool>("活动结束时间必须晚于开始时间", false);
        }

        return await WithOwnedActivityAsync(request.ActivityId, async current =>
        {
            current.ActivityName = request.ActivityName?.Trim();
            current.ActivityDesc = request.ActivityDesc?.Trim();
            current.CoverImage = string.IsNullOrWhiteSpace(request.CoverImage) ? null : request.CoverImage.Trim();
            current.CoreGoal = request.CoreGoal?.Trim();
            current.CustomerBackground = request.CustomerBackground?.Trim();
            current.StartTime = request.StartTime;
            current.EndTime = request.EndTime;
            current.RecommendedDurationMinutes = request.RecommendedDurationMinutes;
            current.PublishStatus = request.PublishStatus;
            current.UpdateTime = DateTime.Now;

            var count = await _activityRepository.UpdateActivityAsync(current);
            return BuildMutationBoolResponse(count, "更新活动失败");
        });
    }

    /// <summary>
    /// 根据活动ID删除活动信息
    /// </summary>
    public async Task<ResponseInfo<bool>> RemoveActivityAsync(long activityId)
    {
        if (activityId <= 0)
        {
            return Failed<bool>("活动ID不能为空", false);
        }

        return await WithOwnedActivityAsync(activityId, async _ =>
        {
            var count = await _activityRepository.DeleteActivityAsync(activityId, DateTime.Now);
            return BuildMutationBoolResponse(count, "删除活动失败");
        });
    }

    /// <summary>
    /// 新增角色信息
    /// </summary>
    public async Task<ResponseInfo<string>> SaveRoleAsync(RoleSaveRequestDto request)
    {
        if (request == null || request.ActivityId <= 0)
        {
            return Failed("活动ID不能为空", string.Empty);
        }

        if (string.IsNullOrWhiteSpace(request.RoleNickname))
        {
            return Failed("角色昵称不能为空", string.Empty);
        }

        return await WithOwnedActivityAsync(request.ActivityId, async _ =>
        {
            var now = DateTime.Now;
            var role = new RoleEntity
            {
                RoleId = await _grpcService.GenSerialIdAsync(),
                ActivityId = request.ActivityId,
                CustomerId = request.CustomerId,
                RoleNickname = request.RoleNickname?.Trim(),
                JobTitle = request.JobTitle?.Trim(),
                ProjectRole = request.ProjectRole?.Trim(),
                Personality = request.Personality?.Trim(),
                CommunicationStyle = request.CommunicationStyle?.Trim(),
                ProjectRequirement = request.ProjectRequirement?.Trim(),
                IsDeleted = false,
                CreateTime = now,
                UpdateTime = now
            };

            var count = await _activityRepository.InsertRoleAsync(role);
            return BuildMutationStringResponse(count, role.RoleId.ToString(), "新增角色失败");
        }, failedData: string.Empty);
    }

    /// <summary>
    /// 根据角色ID更新角色信息
    /// </summary>
    public async Task<ResponseInfo<bool>> UpdateRoleAsync(RoleUpdateRequestDto request)
    {
        if (request == null || request.RoleId <= 0)
        {
            return Failed<bool>("角色ID不能为空", false);
        }

        if (string.IsNullOrWhiteSpace(request.RoleNickname))
        {
            return Failed<bool>("角色昵称不能为空", false);
        }

        var current = await _activityRepository.SelectRoleByIdAsync(request.RoleId);
        if (current == null)
        {
            return Failed<bool>("角色不存在", false);
        }

        return await WithOwnedActivityAsync(current.ActivityId, async _ =>
        {
            current.CustomerId = request.CustomerId;
            current.RoleNickname = request.RoleNickname?.Trim();
            current.JobTitle = request.JobTitle?.Trim();
            current.ProjectRole = request.ProjectRole?.Trim();
            current.Personality = request.Personality?.Trim();
            current.CommunicationStyle = request.CommunicationStyle?.Trim();
            current.ProjectRequirement = request.ProjectRequirement?.Trim();
            current.UpdateTime = DateTime.Now;

            var count = await _activityRepository.UpdateRoleAsync(current);
            return BuildMutationBoolResponse(count, "更新角色失败");
        }, "活动ID不能为空", "角色所属活动不存在");
    }

    /// <summary>
    /// 根据角色ID删除角色信息
    /// </summary>
    public async Task<ResponseInfo<bool>> RemoveRoleAsync(long roleId)
    {
        if (roleId <= 0)
        {
            return Failed<bool>("角色ID不能为空", false);
        }

        var current = await _activityRepository.SelectRoleByIdAsync(roleId);
        if (current == null)
        {
            return Failed<bool>("角色不存在", false);
        }

        return await WithOwnedActivityAsync(current.ActivityId, async _ =>
        {
            if (await _activityRepository.ExistsQuestionByRoleAsync(roleId))
            {
                return Failed<bool>("当前角色下存在题目，无法删除", false);
            }

            var count = await _activityRepository.DeleteRoleAsync(roleId, DateTime.Now);
            return BuildMutationBoolResponse(count, "删除角色失败");
        }, "活动ID不能为空", "角色所属活动不存在");
    }

    /// <summary>
    /// 新增业务环节信息
    /// </summary>
    public async Task<ResponseInfo<string>> SaveBusinessStageAsync(BusinessStageSaveRequestDto request)
    {
        if (request == null || request.ActivityId <= 0)
        {
            return Failed("活动ID不能为空", string.Empty);
        }

        if (string.IsNullOrWhiteSpace(request.StageName))
        {
            return Failed("业务环节名称不能为空", string.Empty);
        }

        if (request.QuestionCount < 0)
        {
            return Failed("最大出题数量不能小于0", string.Empty);
        }

        return await WithOwnedActivityAsync(request.ActivityId, async _ =>
        {
            var now = DateTime.Now;
            var stage = new BusinessStageEntity
            {
                StageId = await _grpcService.GenSerialIdAsync(),
                ActivityId = request.ActivityId,
                StageName = request.StageName?.Trim(),
                StageDesc = request.StageDesc?.Trim(),
                StageTask = request.StageTask?.Trim(),
                SortNo = request.SortNo,
                QuestionCount = request.QuestionCount,
                IsDeleted = false,
                CreateTime = now,
                UpdateTime = now
            };

            var count = await _activityRepository.InsertBusinessStageAsync(stage);
            return BuildMutationStringResponse(count, stage.StageId.ToString(), "新增业务环节失败");
        }, failedData: string.Empty);
    }

    /// <summary>
    /// 根据业务环节ID更新业务环节信息
    /// </summary>
    public async Task<ResponseInfo<bool>> UpdateBusinessStageAsync(BusinessStageUpdateRequestDto request)
    {
        if (request == null || request.StageId <= 0)
        {
            return Failed<bool>("业务环节ID不能为空", false);
        }

        if (string.IsNullOrWhiteSpace(request.StageName))
        {
            return Failed<bool>("业务环节名称不能为空", false);
        }

        if (request.QuestionCount < 0)
        {
            return Failed<bool>("最大出题数量不能小于0", false);
        }

        var current = await _activityRepository.SelectBusinessStageByIdAsync(request.StageId);
        if (current == null)
        {
            return Failed<bool>("业务环节不存在", false);
        }

        return await WithOwnedActivityAsync(current.ActivityId, async _ =>
        {
            current.StageName = request.StageName?.Trim();
            current.StageDesc = request.StageDesc?.Trim();
            current.StageTask = request.StageTask?.Trim();
            current.SortNo = request.SortNo;
            current.QuestionCount = request.QuestionCount;
            current.UpdateTime = DateTime.Now;

            var count = await _activityRepository.UpdateBusinessStageAsync(current);
            return BuildMutationBoolResponse(count, "更新业务环节失败");
        }, "活动ID不能为空", "业务环节所属活动不存在");
    }

    /// <summary>
    /// 根据业务环节ID删除业务环节信息
    /// </summary>
    public async Task<ResponseInfo<bool>> RemoveBusinessStageAsync(long stageId)
    {
        if (stageId <= 0)
        {
            return Failed<bool>("业务环节ID不能为空", false);
        }

        var current = await _activityRepository.SelectBusinessStageByIdAsync(stageId);
        if (current == null)
        {
            return Failed<bool>("业务环节不存在", false);
        }

        return await WithOwnedActivityAsync(current.ActivityId, async _ =>
        {
            if (await _activityRepository.ExistsQuestionByStageAsync(stageId))
            {
                return Failed<bool>("当前业务环节下存在题目，无法删除", false);
            }

            var count = await _activityRepository.DeleteBusinessStageAsync(stageId, DateTime.Now);
            return BuildMutationBoolResponse(count, "删除业务环节失败");
        }, "活动ID不能为空", "业务环节所属活动不存在");
    }

    /// <summary>
    /// 新增题目信息
    /// </summary>
    public async Task<ResponseInfo<string>> SaveQuestionAsync(QuestionSaveRequestDto request)
    {
        if (request == null || request.ActivityId <= 0)
        {
            return Failed("活动ID不能为空", string.Empty);
        }

        if (request.RoleId <= 0)
        {
            return Failed("角色ID不能为空", string.Empty);
        }

        if (request.StageId <= 0)
        {
            return Failed("业务环节ID不能为空", string.Empty);
        }

        if (string.IsNullOrWhiteSpace(request.QuestionStem))
        {
            return Failed("题干不能为空", string.Empty);
        }

        return await WithOwnedActivityAsync(request.ActivityId, async _ =>
        {
            var role = await _activityRepository.SelectRoleByIdAsync(request.RoleId);
            if (role == null || role.ActivityId != request.ActivityId)
            {
                return Failed("角色不存在或不属于当前活动", string.Empty);
            }

            var stage = await _activityRepository.SelectBusinessStageByIdAsync(request.StageId);
            if (stage == null || stage.ActivityId != request.ActivityId)
            {
                return Failed("业务环节不存在或不属于当前活动", string.Empty);
            }

            var now = DateTime.Now;
            var question = new QuestionBankEntity
            {
                QuestionId = await _grpcService.GenSerialIdAsync(),
                ActivityId = request.ActivityId,
                RoleId = request.RoleId,
                QuestionStem = request.QuestionStem?.Trim(),
                AssessmentPoints = request.AssessmentPoints?.Trim(),
                IsRequired = request.IsRequired,
                StageId = request.StageId,
                IsDeleted = false,
                CreateTime = now,
                UpdateTime = now
            };

            var count = await _activityRepository.InsertQuestionAsync(question);
            return BuildMutationStringResponse(count, question.QuestionId.ToString(), "新增题目失败");
        }, failedData: string.Empty);
    }

    /// <summary>
    /// 根据题目ID更新题目信息
    /// </summary>
    public async Task<ResponseInfo<bool>> UpdateQuestionAsync(QuestionUpdateRequestDto request)
    {
        if (request == null || request.QuestionId <= 0)
        {
            return Failed<bool>("题目ID不能为空", false);
        }

        if (request.RoleId <= 0)
        {
            return Failed<bool>("角色ID不能为空", false);
        }

        if (request.StageId <= 0)
        {
            return Failed<bool>("业务环节ID不能为空", false);
        }

        if (string.IsNullOrWhiteSpace(request.QuestionStem))
        {
            return Failed<bool>("题干不能为空", false);
        }

        var current = await _activityRepository.SelectQuestionByIdAsync(request.QuestionId);
        if (current == null)
        {
            return Failed<bool>("题目不存在", false);
        }

        return await WithOwnedActivityAsync(current.ActivityId, async _ =>
        {
            var role = await _activityRepository.SelectRoleByIdAsync(request.RoleId);
            if (role == null || role.ActivityId != current.ActivityId)
            {
                return Failed<bool>("角色不存在或不属于当前活动", false);
            }

            var stage = await _activityRepository.SelectBusinessStageByIdAsync(request.StageId);
            if (stage == null || stage.ActivityId != current.ActivityId)
            {
                return Failed<bool>("业务环节不存在或不属于当前活动", false);
            }

            current.RoleId = request.RoleId;
            current.QuestionStem = request.QuestionStem?.Trim();
            current.AssessmentPoints = request.AssessmentPoints?.Trim();
            current.IsRequired = request.IsRequired;
            current.StageId = request.StageId;
            current.UpdateTime = DateTime.Now;

            var count = await _activityRepository.UpdateQuestionAsync(current);
            return BuildMutationBoolResponse(count, "更新题目失败");
        }, "活动ID不能为空", "题目所属活动不存在");
    }

    /// <summary>
    /// 根据题目ID删除题目信息
    /// </summary>
    public async Task<ResponseInfo<bool>> RemoveQuestionAsync(long questionId)
    {
        if (questionId <= 0)
        {
            return Failed<bool>("题目ID不能为空", false);
        }

        var current = await _activityRepository.SelectQuestionByIdAsync(questionId);
        if (current == null)
        {
            return Failed<bool>("题目不存在", false);
        }

        return await WithOwnedActivityAsync(current.ActivityId, async _ =>
        {
            var count = await _activityRepository.DeleteQuestionAsync(questionId, current.ActivityId, DateTime.Now);
            return BuildMutationBoolResponse(count, "删除题目失败");
        }, "活动ID不能为空", "题目所属活动不存在");
    }

    /// <summary>
    /// 根据题目ID批量删除题目信息
    /// </summary>
    public async Task<ResponseInfo<bool>> BatchRemoveQuestionAsync(IEnumerable<long> questionIds)
    {
        var idList = questionIds?
            .Where(x => x > 0)
            .Distinct()
            .ToList() ?? new List<long>();

        if (idList.Count == 0)
        {
            return Failed<bool>("题目ID集合不能为空", false);
        }

        var questions = (await _activityRepository.SelectQuestionsByIdsAsync(idList)).ToList();
        if (questions.Count != idList.Count)
        {
            return Failed<bool>("存在无效或已删除的题目ID", false);
        }

        var custCompanyId = GetCustCompanyIdOrNull();
        if (!custCompanyId.HasValue || custCompanyId.Value <= 0)
        {
            return Failed<bool>("所属机构不能为空", false);
        }

        var activityIds = questions
            .Select(x => x.ActivityId)
            .Distinct()
            .ToList();

        var ownedActivityCount = await _activityRepository.CountActivitiesAsync(activityIds, custCompanyId.Value);
        if (ownedActivityCount != activityIds.Count)
        {
            return Failed<bool>("存在题目所属活动不存在或无权限操作", false);
        }

        var count = await _activityRepository.DeleteQuestionsAsync(idList, activityIds, DateTime.Now);
        return BuildMutationBoolResponse(count, "批量删除题目失败");
    }

    /// <summary>
    /// 提交活动信息提取任务
    /// </summary>
    public async Task<ResponseInfo<ActivityExtractSubmitResponseDto>> ExtractActivityInfoAsync(ActivityExtractRequestDto request)
    {
        _logger.LogInformation("【活动提取】开始提交活动信息提取任务。文件数量={FileCount}", request?.Files?.Count ?? 0);

        var normalizedFiles = NormalizeActivityExtractFiles(request?.Files);
        if (normalizedFiles.Count == 0)
        {
            _logger.LogWarning("【活动提取】文件地址集合为空，任务提交失败");
            return Failed<ActivityExtractSubmitResponseDto>("文件地址集合不能为空");
        }

        var taskId = Guid.NewGuid().ToString("N");
        var cacheKey = GetActivityExtractTaskCacheKey(taskId);
        var custCompanyId = GetCustCompanyIdOrNull();
        _logger.LogInformation("【活动提取】任务已创建。taskId={TaskId}, custCompanyId={CustCompanyId}, 文件数量={FileCount}", taskId, custCompanyId, normalizedFiles.Count);

        var taskState = new ActivityExtractTaskCacheDto
        {
            TaskId = taskId,
            CustCompanyId = custCompanyId,
            Status = ActivityExtractTaskStatus.Pending,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };
        await _cacheService.SetWithRedisAsync(cacheKey, taskState, TimeSpan.FromHours(24));

        _ = Task.Run(() => ProcessExtractActivityInfoAsync(cacheKey, normalizedFiles));

        _logger.LogInformation("【活动提取】任务已提交后台处理。taskId={TaskId}", taskId);

        return new ResponseInfo<ActivityExtractSubmitResponseDto>
        {
            State = ResultState.Successed,
            Data = new ActivityExtractSubmitResponseDto
            {
                TaskId = taskId
            }
        };
    }

    /// <summary>
    /// 提交题库信息提取任务
    /// </summary>
    public async Task<ResponseInfo<QuestionExtractSubmitResponseDto>> ExtractQuestionInfoAsync(QuestionExtractRequestDto request)
    {
        _logger.LogInformation("【题库提取】开始提交题库信息提取任务。activityId={ActivityId}, 文件数量={FileCount}", request?.ActivityId, request?.Files?.Count ?? 0);

        if (request == null || request.ActivityId <= 0)
        {
            _logger.LogWarning("【题库提取】活动ID不能为空");
            return Failed<QuestionExtractSubmitResponseDto>("活动ID不能为空");
        }

        var normalizedFiles = NormalizeActivityExtractFiles(request.Files);
        if (normalizedFiles.Count == 0)
        {
            _logger.LogWarning("【题库提取】文件地址集合为空，任务提交失败");
            return Failed<QuestionExtractSubmitResponseDto>("文件地址集合不能为空");
        }

        var taskId = Guid.NewGuid().ToString("N");
        var cacheKey = GetQuestionExtractTaskCacheKey(taskId);
        var activityResult = await WithOwnedActivityAsync(request.ActivityId, _ => Task.FromResult<ResponseInfo<QuestionExtractSubmitResponseDto>>(null));
        if (activityResult != null)
        {
            _logger.LogWarning("【题库提取】活动权限校验失败。activityId={ActivityId}", request.ActivityId);
            return activityResult;
        }

        var custCompanyId = GetCustCompanyIdOrNull();
        _logger.LogInformation("【题库提取】任务已创建。taskId={TaskId}, activityId={ActivityId}, custCompanyId={CustCompanyId}, 文件数量={FileCount}", taskId, request.ActivityId, custCompanyId, normalizedFiles.Count);

        var taskState = new QuestionExtractTaskCacheDto
        {
            TaskId = taskId,
            ActivityId = request.ActivityId,
            CustCompanyId = custCompanyId,
            Status = ActivityExtractTaskStatus.Pending,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };
        await _cacheService.SetWithRedisAsync(cacheKey, taskState, TimeSpan.FromHours(24));

        _ = Task.Run(() => ProcessExtractQuestionInfoAsync(cacheKey, normalizedFiles));

        _logger.LogInformation("【题库提取】任务已提交后台处理。taskId={TaskId}", taskId);

        return new ResponseInfo<QuestionExtractSubmitResponseDto>
        {
            State = ResultState.Successed,
            Data = new QuestionExtractSubmitResponseDto
            {
                TaskId = taskId
            }
        };
    }

    /// <summary>
    /// 查询活动信息提取结果
    /// </summary>
    public async Task<ResponseInfo<ActivityExtractResultResponseDto>> GetExtractActivityInfoResultAsync(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
        {
            return new ResponseInfo<ActivityExtractResultResponseDto>
            {
                State = ResultState.Failed,
                ErrorMessage = "任务ID不能为空"
            };
        }

        var task = await _cacheService.GetWithRedisAsync<ActivityExtractTaskCacheDto>(GetActivityExtractTaskCacheKey(taskId));
        if (task == null)
        {
            return new ResponseInfo<ActivityExtractResultResponseDto>
            {
                State = ResultState.Failed,
                ErrorMessage = "任务不存在或已过期"
            };
        }

        if (task.Status == ActivityExtractTaskStatus.Success && !task.IsImported)
        {
            try
            {
                await ImportExtractedActivityAsync(GetActivityExtractTaskCacheKey(taskId), task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入活动提取结果失败。taskId={TaskId}", taskId);
                await UpdateTaskFailedAsync(GetActivityExtractTaskCacheKey(taskId), ex.Message);
            }

            task = await _cacheService.GetWithRedisAsync<ActivityExtractTaskCacheDto>(GetActivityExtractTaskCacheKey(taskId));
            if (task == null)
            {
                return new ResponseInfo<ActivityExtractResultResponseDto>
                {
                    State = ResultState.Failed,
                    ErrorMessage = "任务不存在或已过期"
                };
            }
        }

        return new ResponseInfo<ActivityExtractResultResponseDto>
        {
            State = ResultState.Successed,
            Data = new ActivityExtractResultResponseDto
            {
                TaskId = task.TaskId,
                Status = task.Status,
                Result = DeserializeTaskResult(task),
                ErrorMessage = task.ErrorMessage,
                CreateTime = task.CreateTime,
                UpdateTime = task.UpdateTime
            }
        };
    }

    /// <summary>
    /// 查询题库信息提取结果
    /// </summary>
    public async Task<ResponseInfo<QuestionExtractResultResponseDto>> GetExtractQuestionInfoResultAsync(string taskId, long activityId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
        {
            return new ResponseInfo<QuestionExtractResultResponseDto>
            {
                State = ResultState.Failed,
                ErrorMessage = "任务ID不能为空"
            };
        }

        if (activityId <= 0)
        {
            return new ResponseInfo<QuestionExtractResultResponseDto>
            {
                State = ResultState.Failed,
                ErrorMessage = "活动ID不能为空"
            };
        }

        var task = await _cacheService.GetWithRedisAsync<QuestionExtractTaskCacheDto>(GetQuestionExtractTaskCacheKey(taskId));
        if (task == null)
        {
            return new ResponseInfo<QuestionExtractResultResponseDto>
            {
                State = ResultState.Failed,
                ErrorMessage = "任务不存在或已过期"
            };
        }

        if (task.ActivityId > 0 && task.ActivityId != activityId)
        {
            return new ResponseInfo<QuestionExtractResultResponseDto>
            {
                State = ResultState.Failed,
                ErrorMessage = "任务所属活动与传入活动不一致"
            };
        }

        if (task.Status == ActivityExtractTaskStatus.Success && !task.IsImported)
        {
            try
            {
                await ImportExtractedQuestionsAsync(GetQuestionExtractTaskCacheKey(taskId), task, activityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入题库提取结果失败。taskId={TaskId}, activityId={ActivityId}", taskId, activityId);
                await UpdateQuestionTaskFailedAsync(GetQuestionExtractTaskCacheKey(taskId), ex.Message);
            }

            task = await _cacheService.GetWithRedisAsync<QuestionExtractTaskCacheDto>(GetQuestionExtractTaskCacheKey(taskId));
            if (task == null)
            {
                return new ResponseInfo<QuestionExtractResultResponseDto>
                {
                    State = ResultState.Failed,
                    ErrorMessage = "任务不存在或已过期"
                };
            }
        }

        return new ResponseInfo<QuestionExtractResultResponseDto>
        {
            State = ResultState.Successed,
            Data = new QuestionExtractResultResponseDto
            {
                TaskId = task.TaskId,
                Status = task.Status,
                Result = DeserializeQuestionTaskResult(task),
                ErrorMessage = task.ErrorMessage,
                CreateTime = task.CreateTime,
                UpdateTime = task.UpdateTime
            }
        };
    }

    private async Task ProcessExtractActivityInfoAsync(string cacheKey, List<string> files)
    {
        try
        {
            _logger.LogInformation("【活动提取】后台任务开始执行。cacheKey={CacheKey}, 文件数量={FileCount}", cacheKey, files?.Count ?? 0);

            await UpdateTaskStatusAsync(cacheKey, ActivityExtractTaskStatus.Processing);
            _logger.LogInformation("【活动提取】任务状态已更新为Processing。cacheKey={CacheKey}", cacheKey);

            var prompt = _promptConfigService.GetByName("roadshow_bridge_info_llm");
            if (prompt == null)
            {
                _logger.LogWarning("【活动提取】未找到提示词配置：roadshow_bridge_info_llm");
                await UpdateTaskFailedAsync(cacheKey, "未找到提示词配置：roadshow_bridge_info_llm");
                return;
            }

            if (string.IsNullOrWhiteSpace(prompt.system_prompt))
            {
                _logger.LogWarning("【活动提取】提示词模板内容为空");
                await UpdateTaskFailedAsync(cacheKey, "提示词模板内容为空");
                return;
            }

            if (string.IsNullOrWhiteSpace(prompt.llm_conf?.model))
            {
                _logger.LogWarning("【活动提取】提示词未配置模型信息");
                await UpdateTaskFailedAsync(cacheKey, "提示词未配置模型信息");
                return;
            }

            _logger.LogInformation("【活动提取】提示词配置校验通过。model={Model}, system_prompt长度={PromptLength}", prompt.llm_conf.model, prompt.system_prompt.Length);

            _logger.LogInformation("【活动提取】开始OCR识别。文件数量={FileCount}", files?.Count ?? 0);
            var originalContent = await _llmService.OcrByCoze(files);
            _logger.LogInformation("【活动提取】OCR识别完成。内容长度={ContentLength}", originalContent?.Length ?? 0);

            _logger.LogInformation("【活动提取】开始获取客户信息");
            var customerInfoJson = await BuildCustomerInfoPromptJsonAsync();
            _logger.LogInformation("【活动提取】客户信息获取完成。JSON长度={JsonLength}", customerInfoJson?.Length ?? 0);

            var systemPrompt = prompt.system_prompt
                .Replace("{original_content}", originalContent ?? string.Empty)
                .Replace("{customer_info}", customerInfoJson);
            _logger.LogInformation("【活动提取】system_prompt占位符替换完成。最终长度={PromptLength}", systemPrompt.Length);

            var chatRequest = new ChatCompletionsRequestDto
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

            _logger.LogInformation("【活动提取】开始调用大模型提取。model={Model}, maxTokens={MaxTokens}", chatRequest.Model, chatRequest.MaxTokens);
            var result = await _llmService.ChatCompletionAsync(chatRequest);
            _logger.LogInformation("【活动提取】大模型调用完成。state={State}", result.State);

            if (result.State != ResultState.Successed)
            {
                _logger.LogWarning("【活动提取】大模型提取失败。errorMessage={ErrorMessage}", result.ErrorMessage);
                await UpdateTaskFailedAsync(cacheKey, result.ErrorMessage ?? "大模型提取失败");
                return;
            }

            await UpdateTaskSuccessAsync(cacheKey, result.Data);
            _logger.LogInformation("【活动提取】任务处理成功。cacheKey={CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理活动提取任务失败。cacheKey={CacheKey}", cacheKey);
            await UpdateTaskFailedAsync(cacheKey, ex.Message);
        }
    }

    private async Task ProcessExtractQuestionInfoAsync(string cacheKey, List<string> files)
    {
        try
        {
            _logger.LogInformation("【题库提取】后台任务开始执行。cacheKey={CacheKey}, 文件数量={FileCount}", cacheKey, files?.Count ?? 0);

            await UpdateQuestionTaskStatusAsync(cacheKey, ActivityExtractTaskStatus.Processing);
            _logger.LogInformation("【题库提取】任务状态已更新为Processing。cacheKey={CacheKey}", cacheKey);

            var prompt = _promptConfigService.GetByName(QuestionExtractPromptName);
            if (prompt == null)
            {
                _logger.LogWarning("【题库提取】未找到提示词配置：{PromptName}", QuestionExtractPromptName);
                await UpdateQuestionTaskFailedAsync(cacheKey, $"未找到提示词配置：{QuestionExtractPromptName}");
                return;
            }

            if (string.IsNullOrWhiteSpace(prompt.system_prompt))
            {
                _logger.LogWarning("【题库提取】提示词模板内容为空");
                await UpdateQuestionTaskFailedAsync(cacheKey, "提示词模板内容为空");
                return;
            }

            if (string.IsNullOrWhiteSpace(prompt.llm_conf?.model))
            {
                _logger.LogWarning("【题库提取】提示词未配置模型信息");
                await UpdateQuestionTaskFailedAsync(cacheKey, "提示词未配置模型信息");
                return;
            }

            _logger.LogInformation("【题库提取】提示词配置校验通过。model={Model}, system_prompt长度={PromptLength}", prompt.llm_conf.model, prompt.system_prompt.Length);

            _logger.LogInformation("【题库提取】开始OCR识别。文件数量={FileCount}", files?.Count ?? 0);
            var originalContent = await _llmService.OcrByCoze(files);
            _logger.LogInformation("【题库提取】OCR识别完成。内容长度={ContentLength}", originalContent?.Length ?? 0);

            var task = await _cacheService.GetWithRedisAsync<QuestionExtractTaskCacheDto>(cacheKey);
            if (task == null || task.ActivityId <= 0)
            {
                _logger.LogWarning("【题库提取】题库提取任务缺少活动信息。task={Task}, activityId={ActivityId}", task != null ? "exists" : "null", task?.ActivityId);
                await UpdateQuestionTaskFailedAsync(cacheKey, "题库提取任务缺少活动信息");
                return;
            }

            _logger.LogInformation("【题库提取】开始获取能力维度和角色信息。activityId={ActivityId}", task.ActivityId);
            var abilityDimensionJson = await BuildAbilityDimensionPromptJsonAsync();
            var roleJson = await BuildRolePromptJsonAsync(task.ActivityId);
            _logger.LogInformation("【题库提取】能力维度和角色信息获取完成。abilityDimension长度={AbilityDimensionLength}, roleJson长度={RoleJsonLength}", abilityDimensionJson?.Length ?? 0, roleJson?.Length ?? 0);

            if (string.Equals(roleJson, "[]", StringComparison.Ordinal))
            {
                _logger.LogWarning("【题库提取】当前活动未配置角色，无法提取题库信息。activityId={ActivityId}", task.ActivityId);
                await UpdateQuestionTaskFailedAsync(cacheKey, "当前活动未配置角色，无法提取题库信息");
                return;
            }

            var systemPrompt = prompt.system_prompt
                .Replace("{original_content}", originalContent ?? string.Empty)
                .Replace("{ability_dimension_list}", abilityDimensionJson)
                .Replace("{role_list}", roleJson);
            _logger.LogInformation("【题库提取】system_prompt占位符替换完成。最终长度={PromptLength}", systemPrompt.Length);

            var chatRequest = new ChatCompletionsRequestDto
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

            _logger.LogInformation("【题库提取】开始调用大模型提取。model={Model}, maxTokens={MaxTokens}", chatRequest.Model, chatRequest.MaxTokens);
            var result = await _llmService.ChatCompletionAsync(chatRequest);
            _logger.LogInformation("【题库提取】大模型调用完成。state={State}", result.State);

            if (result.State != ResultState.Successed)
            {
                _logger.LogWarning("【题库提取】大模型提取失败。errorMessage={ErrorMessage}", result.ErrorMessage);
                await UpdateQuestionTaskFailedAsync(cacheKey, result.ErrorMessage ?? "大模型提取失败");
                return;
            }

            await UpdateQuestionTaskSuccessAsync(cacheKey, result.Data);
            _logger.LogInformation("【题库提取】任务处理成功。cacheKey={CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理题库提取任务失败。cacheKey={CacheKey}", cacheKey);
            await UpdateQuestionTaskFailedAsync(cacheKey, ex.Message);
        }
    }

    private async Task UpdateTaskStatusAsync(string cacheKey, string status)
    {
        var task = await _cacheService.GetWithRedisAsync<ActivityExtractTaskCacheDto>(cacheKey);
        if (task == null)
        {
            return;
        }

        task.Status = status;
        task.UpdateTime = DateTime.Now;
        await _cacheService.SetWithRedisAsync(cacheKey, task, TimeSpan.FromHours(24));
    }

    private async Task UpdateQuestionTaskStatusAsync(string cacheKey, string status)
    {
        var task = await _cacheService.GetWithRedisAsync<QuestionExtractTaskCacheDto>(cacheKey);
        if (task == null)
        {
            return;
        }

        task.Status = status;
        task.UpdateTime = DateTime.Now;
        await _cacheService.SetWithRedisAsync(cacheKey, task, TimeSpan.FromHours(24));
    }

    private async Task UpdateTaskSuccessAsync(string cacheKey, object result)
    {
        var task = await _cacheService.GetWithRedisAsync<ActivityExtractTaskCacheDto>(cacheKey);
        if (task == null)
        {
            return;
        }

        task.Status = ActivityExtractTaskStatus.Success;
        task.ResultJson = result == null ? null : JsonConvert.SerializeObject(result);
        task.IsImported = false;
        task.ImportResultJson = null;
        task.ErrorMessage = null;
        task.UpdateTime = DateTime.Now;
        await _cacheService.SetWithRedisAsync(cacheKey, task, TimeSpan.FromHours(24));
    }

    private async Task UpdateTaskImportedAsync(string cacheKey, ActivityExtractImportResult importResult)
    {
        var task = await _cacheService.GetWithRedisAsync<ActivityExtractTaskCacheDto>(cacheKey);
        if (task == null)
        {
            return;
        }

        task.IsImported = true;
        task.ImportResultJson = JsonConvert.SerializeObject(importResult);
        task.ErrorMessage = null;
        task.UpdateTime = DateTime.Now;
        await _cacheService.SetWithRedisAsync(cacheKey, task, TimeSpan.FromHours(24));
    }

    private async Task UpdateTaskFailedAsync(string cacheKey, string errorMessage)
    {
        var task = await _cacheService.GetWithRedisAsync<ActivityExtractTaskCacheDto>(cacheKey);
        if (task == null)
        {
            return;
        }

        task.Status = ActivityExtractTaskStatus.Failed;
        task.ErrorMessage = errorMessage;
        task.UpdateTime = DateTime.Now;
        await _cacheService.SetWithRedisAsync(cacheKey, task, TimeSpan.FromHours(24));
    }

    private async Task UpdateQuestionTaskSuccessAsync(string cacheKey, object result)
    {
        var task = await _cacheService.GetWithRedisAsync<QuestionExtractTaskCacheDto>(cacheKey);
        if (task == null)
        {
            return;
        }

        task.Status = ActivityExtractTaskStatus.Success;
        task.ResultJson = result == null ? null : JsonConvert.SerializeObject(result);
        task.IsImported = false;
        task.ImportResultJson = null;
        task.ErrorMessage = null;
        task.UpdateTime = DateTime.Now;
        await _cacheService.SetWithRedisAsync(cacheKey, task, TimeSpan.FromHours(24));
    }

    private async Task UpdateQuestionTaskImportedAsync(string cacheKey, QuestionExtractImportResult importResult)
    {
        var task = await _cacheService.GetWithRedisAsync<QuestionExtractTaskCacheDto>(cacheKey);
        if (task == null)
        {
            return;
        }

        task.IsImported = true;
        task.ImportResultJson = JsonConvert.SerializeObject(importResult);
        task.ErrorMessage = null;
        task.UpdateTime = DateTime.Now;
        await _cacheService.SetWithRedisAsync(cacheKey, task, TimeSpan.FromHours(24));
    }

    private async Task UpdateQuestionTaskFailedAsync(string cacheKey, string errorMessage)
    {
        var task = await _cacheService.GetWithRedisAsync<QuestionExtractTaskCacheDto>(cacheKey);
        if (task == null)
        {
            return;
        }

        task.Status = ActivityExtractTaskStatus.Failed;
        task.ErrorMessage = errorMessage;
        task.UpdateTime = DateTime.Now;
        await _cacheService.SetWithRedisAsync(cacheKey, task, TimeSpan.FromHours(24));
    }

    private static string GetActivityExtractTaskCacheKey(string taskId)
        => $"{ActivityExtractTaskCachePrefix}{taskId}";

    private static string GetQuestionExtractTaskCacheKey(string taskId)
        => $"{QuestionExtractTaskCachePrefix}{taskId}";

    private async Task<string> BuildAbilityDimensionPromptJsonAsync()
    {
        var abilityDimensions = await _abilityDimensionRepository.SelectAbilityDimensionListAsync();
        var data = abilityDimensions.Select(x => new AbilityDimensionPromptItem
        {
            Id = x.AbilityDimensionId,
            Name = x.AbilityDimensionName,
            ParentId = x.ParentAbilityDimensionId
        }).ToList();
        return JsonConvert.SerializeObject(data);
    }

    private async Task<string> BuildRolePromptJsonAsync(long activityId)
    {
        if (activityId <= 0)
        {
            return "[]";
        }

        var roles = await _activityRepository.SelectActivityRolesAsync(activityId);
        var data = roles.Select(x => new RolePromptItem
        {
            Id = x.RoleId,
            RoleNickname = x.RoleNickname
        }).ToList();
        return JsonConvert.SerializeObject(data);
    }

    private async Task<string> BuildCustomerInfoPromptJsonAsync()
    {
        var customerResult = await _customerService.GetCustomerListAsync();
        if (customerResult?.State != ResultState.Successed || customerResult.Data == null)
        {
            return "[]";
        }

        var data = customerResult.Data.Select(x => new
        {
            x.CustId,
            x.Age,
            x.Name,
            x.CustType,
            x.Occupation,
            x.Gender,
            x.InvestmentExperience
        }).ToList();
        return JsonConvert.SerializeObject(data);
    }

    private async Task ImportExtractedActivityAsync(string cacheKey, ActivityExtractTaskCacheDto task)
    {
        var content = DeserializeActivityImportContent(task?.ResultJson);
        if (content?.Activity == null)
        {
            throw new Exception("活动提取结果格式无效，缺少活动主体信息。");
        }

        if (!task.CustCompanyId.HasValue || task.CustCompanyId.Value <= 0)
        {
            throw new Exception("当前用户所属机构不能为空。");
        }

        var now = DateTime.Now;
        var durationMinutes = content.Activity.RecommendedDurationMinutes.GetValueOrDefault() > 0
            ? content.Activity.RecommendedDurationMinutes.Value
            : 45;

        var activityEntity = new ActivityEntity
        {
            ActivityId = await _grpcService.GenSerialIdAsync(),
            CustCompanyId = task.CustCompanyId.Value,
            ActivityName = content.Activity.ActivityName?.Trim(),
            ActivityDesc = content.Activity.ActivityDesc?.Trim(),
            CoverImage = null,
            CoreGoal = content.Activity.CoreGoal?.Trim(),
            CustomerBackground = content.Activity.CustomerBackground?.Trim(),
            StartTime = now,
            EndTime = now.AddMinutes(durationMinutes),
            RecommendedDurationMinutes = durationMinutes,
            PublishStatus = 0,
            IsDeleted = false,
            CreateTime = now,
            UpdateTime = now
        };

        var roleList = new List<RoleEntity>();
        foreach (var role in content.Roles ?? Enumerable.Empty<ActivityExtractRoleContent>())
        {
            var nickname = role.RoleNickname?.Trim();
            if (string.IsNullOrWhiteSpace(nickname))
            {
                continue;
            }

            var roleEntity = new RoleEntity
            {
                RoleId = await _grpcService.GenSerialIdAsync(),
                ActivityId = activityEntity.ActivityId,
                CustomerId = role.CustomerId,
                RoleNickname = nickname,
                JobTitle = role.JobTitle?.Trim(),
                ProjectRole = role.ProjectRole?.Trim(),
                Personality = role.Personality?.Trim(),
                CommunicationStyle = role.CommunicationStyle?.Trim(),
                ProjectRequirement = role.ProjectRequirement?.Trim(),
                IsDeleted = false,
                CreateTime = now,
                UpdateTime = now
            };

            roleList.Add(roleEntity);
        }

        await _activityRepository.InsertExtractedActivityAsync(activityEntity, roleList);
        await UpdateTaskImportedAsync(cacheKey, new ActivityExtractImportResult
        {
            ActivityId = activityEntity.ActivityId,
            RoleCount = roleList.Count
        });
    }

    private async Task ImportExtractedQuestionsAsync(string cacheKey, QuestionExtractTaskCacheDto task, long activityId)
    {
        var content = DeserializeQuestionImportContent(task?.ResultJson);
        if (content?.Questions == null || content.Questions.Count == 0)
        {
            throw new Exception("题库提取结果格式无效，缺少题库信息。");
        }

        if (!task.CustCompanyId.HasValue || task.CustCompanyId.Value <= 0)
        {
            throw new Exception("当前用户所属机构不能为空。");
        }

        var activity = await _activityRepository.SelectActivityAsync(activityId, task.CustCompanyId.Value);
        if (activity == null)
        {
            throw new Exception("活动不存在或无权访问。");
        }

        if (await _activityRepository.ExistsQuestionByActivityAsync(activityId))
        {
            throw new Exception("当前活动已存在题库，不支持重复导入。");
        }

        var existingStages = (await _activityRepository.SelectActivityBusinessStagesAsync(activityId)).ToList();
        var extractedStages = BuildQuestionStageImportContents(content);
        if (extractedStages.Count == 0)
        {
            throw new Exception("题库提取结果格式无效，缺少业务环节信息。");
        }

        var abilityDimensions = (await _abilityDimensionRepository.SelectAbilityDimensionListAsync()).ToList();
        var abilityIdSet = abilityDimensions.Select(x => x.AbilityDimensionId).ToHashSet();
        var uniqueAbilityNameMap = BuildUniqueKeyMap(abilityDimensions, x => x.AbilityDimensionName, x => x.AbilityDimensionId);
        var roles = (await _activityRepository.SelectActivityRolesAsync(activityId)).ToList();
        if (roles.Count == 0)
        {
            throw new Exception("当前活动未配置角色，无法导入题库。");
        }

        var roleIdSet = roles.Select(x => x.RoleId).ToHashSet();
        var uniqueRoleNameMap = BuildUniqueKeyMap(roles, x => x.RoleNickname, x => x.RoleId);

        var now = DateTime.Now;
        var stagesToInsert = new List<BusinessStageEntity>();
        List<BusinessStageEntity> finalStages;
        IReadOnlyDictionary<long, BusinessStageEntity> stageMap;
        IReadOnlyDictionary<string, long> uniqueStageNameMap;

        if (existingStages.Count > 0)
        {
            stageMap = existingStages.ToDictionary(x => x.StageId);
            uniqueStageNameMap = BuildUniqueKeyMap(existingStages, x => x.StageName, x => x.StageId);

            foreach (var stage in extractedStages)
            {
                ResolveStageId(stage, stageMap, uniqueStageNameMap);
            }

            finalStages = existingStages;
        }
        else
        {
            var sortNo = 1;
            foreach (var stage in extractedStages)
            {
                stagesToInsert.Add(new BusinessStageEntity
                {
                    StageId = await _grpcService.GenSerialIdAsync(),
                    ActivityId = activityId,
                    StageName = stage.StageName?.Trim(),
                    StageDesc = stage.StageDesc?.Trim(),
                    StageTask = stage.StageTask?.Trim(),
                    SortNo = stage.SortNo > 0 ? stage.SortNo : sortNo,
                    QuestionCount = stage.QuestionCount,
                    IsDeleted = false,
                    CreateTime = now,
                    UpdateTime = now
                });
                sortNo++;
            }

            finalStages = stagesToInsert;
            stageMap = finalStages.ToDictionary(x => x.StageId);
            uniqueStageNameMap = BuildUniqueKeyMap(finalStages, x => x.StageName, x => x.StageId);
        }

        var questionList = new List<QuestionBankEntity>();
        var questionAbilityRelationList = new List<QuestionAbilityRelEntity>();
        foreach (var question in content.Questions)
        {
            var questionStem = question.QuestionStem?.Trim();
            if (string.IsNullOrWhiteSpace(questionStem))
            {
                continue;
            }

            var stageId = ResolveStageId(question, stageMap, uniqueStageNameMap);
            var questionId = await _grpcService.GenSerialIdAsync();
            var roleId = ResolveRoleId(question, roleIdSet, uniqueRoleNameMap);
            questionList.Add(new QuestionBankEntity
            {
                QuestionId = questionId,
                ActivityId = activityId,
                RoleId = roleId,
                QuestionStem = questionStem,
                AssessmentPoints = question.AssessmentPoints?.Trim(),
                IsRequired = question.IsRequired,
                StageId = stageId,
                IsDeleted = false,
                CreateTime = now,
                UpdateTime = now
            });

            var seenAbilityIds = new HashSet<long>();
            foreach (var relation in question.AbilityRelations ?? Enumerable.Empty<QuestionExtractAbilityRelationContent>())
            {
                var abilityDimensionId = ResolveAbilityDimensionId(relation, abilityIdSet, uniqueAbilityNameMap, questionStem);
                if (!abilityDimensionId.HasValue || !seenAbilityIds.Add(abilityDimensionId.Value))
                {
                    continue;
                }

                questionAbilityRelationList.Add(new QuestionAbilityRelEntity
                {
                    QuestionId = questionId,
                    AbilityDimensionId = abilityDimensionId.Value,
                    DifficultyLevel = NormalizeDifficultyLevel(relation.DifficultyLevel),
                    CreateTime = now
                });
            }
        }

        if (questionList.Count == 0)
        {
            throw new Exception("未从提取结果中解析出有效题目。");
        }

        await _activityRepository.InsertExtractedQuestionsAsync(stagesToInsert, questionList, questionAbilityRelationList);
        await UpdateQuestionTaskImportedAsync(cacheKey, new QuestionExtractImportResult
        {
            ActivityId = activityId,
            QuestionCount = questionList.Count,
            AbilityRelationCount = questionAbilityRelationList.Count,
            BusinessStageCount = finalStages.Count
        });
    }

    private List<string> NormalizeActivityExtractFiles(IEnumerable<string> files)
    {
        var normalizedFiles = files?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(BuildActivityExtractFileUrl)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalizedFiles ?? new List<string>();
    }

    private string BuildActivityExtractFileUrl(string filePath)
    {
        var trimmedPath = filePath?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedPath))
        {
            return null;
        }

        if (Uri.TryCreate(trimmedPath, UriKind.Absolute, out _))
        {
            return trimmedPath;
        }

        var defaultEnvUrl = _configuration["BaseUrl:DefaultEnvUrl"]?.Trim();
        if (string.IsNullOrWhiteSpace(defaultEnvUrl))
        {
            return trimmedPath;
        }

        return $"{defaultEnvUrl.TrimEnd('/')}/Beta.FileService/f/{trimmedPath.TrimStart('/')}";
    }

    private static object DeserializeTaskResult(ActivityExtractTaskCacheDto task)
    {
        if (!string.IsNullOrWhiteSpace(task?.ImportResultJson))
        {
            return JsonConvert.DeserializeObject<object>(task.ImportResultJson);
        }

        return ExtractActivityInfoResult(task?.ResultJson);
    }

    private static object DeserializeQuestionTaskResult(QuestionExtractTaskCacheDto task)
    {
        if (!string.IsNullOrWhiteSpace(task?.ImportResultJson))
        {
            return JsonConvert.DeserializeObject<object>(task.ImportResultJson);
        }

        return ExtractActivityInfoResult(task?.ResultJson);
    }

    private static object ExtractActivityInfoResult(string resultJson)
    {
        try
        {
            var content = ExtractMessageContent(resultJson);
            return DeserializeContentJson(content);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ActivityExtractContent DeserializeActivityImportContent(string resultJson)
    {
        var content = NormalizeMessageContent(ExtractMessageContent(resultJson));
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<ActivityExtractContent>(content);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static QuestionExtractContent DeserializeQuestionImportContent(string resultJson)
    {
        var content = NormalizeMessageContent(ExtractMessageContent(resultJson));
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            return ParseQuestionExtractContent(JToken.Parse(content));
        }
        catch (JsonException)
        {
            return null;
        }
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

    private static object DeserializeContentJson(string content)
    {
        var normalizedContent = NormalizeMessageContent(content);
        if (string.IsNullOrWhiteSpace(normalizedContent))
        {
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<object>(normalizedContent);
        }
        catch (JsonException)
        {
            return normalizedContent;
        }
    }

    private static QuestionExtractContent ParseQuestionExtractContent(JToken token)
    {
        var businessStages = GetQuestionStageTokens(token)
            .Select(ParseQuestionExtractStageContent)
            .Where(x => x != null)
            .ToList();

        var questions = GetQuestionItemTokens(token)
            .Select(ParseQuestionExtractQuestionContent)
            .Where(x => x != null)
            .ToList();

        return questions.Count == 0 && businessStages.Count == 0
            ? null
            : new QuestionExtractContent
            {
                BusinessStages = businessStages,
                Questions = questions
            };
    }

    private static IEnumerable<JToken> GetQuestionStageTokens(JToken token)
    {
        if (token?.Type != JTokenType.Object)
        {
            return Enumerable.Empty<JToken>();
        }

        foreach (var propertyName in new[] { "business_stages", "businessStages", "stage_list", "stageList", "stages", "business_stage_list", "businessStageList" })
        {
            var nestedToken = GetPropertyValue(token, propertyName);
            if (nestedToken?.Type != JTokenType.Array)
            {
                continue;
            }

            var items = nestedToken.Children().Where(x => x.Type == JTokenType.Object).ToList();
            if (items.Count > 0)
            {
                return items;
            }
        }

        return Enumerable.Empty<JToken>();
    }

    private static IEnumerable<JToken> GetQuestionItemTokens(JToken token)
    {
        if (token == null)
        {
            return Enumerable.Empty<JToken>();
        }

        if (token.Type == JTokenType.Array)
        {
            return token.Children().Where(x => x.Type == JTokenType.Object);
        }

        if (token.Type != JTokenType.Object)
        {
            return Enumerable.Empty<JToken>();
        }

        if (LooksLikeQuestionToken(token))
        {
            return new[] { token };
        }

        foreach (var propertyName in new[] { "QuestionBank", "questions", "question_list", "questionList", "items", "data", "result", "list" })
        {
            var nestedToken = GetPropertyValue(token, propertyName);
            if (nestedToken == null)
            {
                continue;
            }

            var items = GetQuestionItemTokens(nestedToken).ToList();
            if (items.Count > 0)
            {
                return items;
            }
        }

        return Enumerable.Empty<JToken>();
    }

    private static bool LooksLikeQuestionToken(JToken token)
        => !string.IsNullOrWhiteSpace(GetStringValue(token, "question_stem", "questionStem", "stem", "title"));

    private static QuestionExtractQuestionContent ParseQuestionExtractQuestionContent(JToken token)
    {
        if (token == null || token.Type != JTokenType.Object)
        {
            return null;
        }

        var questionStem = GetStringValue(token, "question_stem", "questionStem", "stem", "title");
        if (string.IsNullOrWhiteSpace(questionStem))
        {
            return null;
        }

        return new QuestionExtractQuestionContent
        {
            QuestionStem = questionStem,
            AssessmentPoints = GetStringValue(token, "assessment_points", "assessmentPoints", "key_points", "keyPoints"),
            IsRequired = GetBooleanValue(token, "is_required", "isRequired", "required", "must_answer", "mustAnswer") ?? false,
            RoleId = GetLongValue(token, "role_id", "roleId"),
            RoleNickname = GetStringValue(token, "role_nickname", "roleNickname", "role_name", "roleName", "role", "character_name", "characterName"),
            StageId = GetLongValue(token, "stage_id", "stageId", "business_stage_id", "businessStageId"),
            StageName = GetStringValue(token, "stage_name", "stageName", "business_stage_name", "businessStageName"),
            AbilityRelations = ParseQuestionAbilityRelations(token)
        };
    }

    private static QuestionExtractStageContent ParseQuestionExtractStageContent(JToken token)
    {
        if (token == null || token.Type != JTokenType.Object)
        {
            return null;
        }

        var stageName = GetStringValue(token, "stage_name", "stageName", "business_stage_name", "businessStageName", "name");
        var stageId = GetLongValue(token, "stage_id", "stageId", "business_stage_id", "businessStageId");
        if (!stageId.HasValue && string.IsNullOrWhiteSpace(stageName))
        {
            return null;
        }

        return new QuestionExtractStageContent
        {
            StageId = stageId,
            StageName = stageName,
            StageDesc = GetStringValue(token, "stage_desc", "stageDesc", "business_stage_desc", "businessStageDesc", "description"),
            StageTask = GetStringValue(token, "stage_task", "stageTask", "business_stage_task", "businessStageTask", "task"),
            SortNo = GetIntValue(token, "sort_no", "sortNo", "sort", "order") ?? 0,
            QuestionCount = GetIntValue(token, "question_count", "questionCount", "count") ?? 0
        };
    }

    private static List<QuestionExtractAbilityRelationContent> ParseQuestionAbilityRelations(JToken token)
    {
        var defaultDifficulty = GetIntValue(token, "difficulty_level", "difficultyLevel", "difficulty");
        var relations = new List<QuestionExtractAbilityRelationContent>();

        foreach (var propertyName in new[] { "ability_relations", "abilityRelations", "ability_dimensions", "abilityDimensions", "ability_dimension_list", "abilityDimensionList" })
        {
            var relationToken = GetPropertyValue(token, propertyName);
            if (relationToken == null)
            {
                continue;
            }

            if (relationToken.Type == JTokenType.Array)
            {
                foreach (var item in relationToken.Children())
                {
                    var relation = ParseSingleAbilityRelation(item, defaultDifficulty);
                    if (relation != null)
                    {
                        relations.Add(relation);
                    }
                }
            }
            else
            {
                var relation = ParseSingleAbilityRelation(relationToken, defaultDifficulty);
                if (relation != null)
                {
                    relations.Add(relation);
                }
            }
        }

        foreach (var propertyName in new[] { "ability_dimension_ids", "abilityDimensionIds" })
        {
            var idsToken = GetPropertyValue(token, propertyName);
            if (idsToken?.Type != JTokenType.Array)
            {
                continue;
            }

            foreach (var item in idsToken.Children())
            {
                var relation = ParseSingleAbilityRelation(item, defaultDifficulty);
                if (relation != null)
                {
                    relations.Add(relation);
                }
            }
        }

        if (relations.Count == 0)
        {
            var directRelation = ParseSingleAbilityRelation(token, defaultDifficulty);
            if (directRelation != null)
            {
                relations.Add(directRelation);
            }
        }

        return relations;
    }

    private static QuestionExtractAbilityRelationContent ParseSingleAbilityRelation(JToken token, int? defaultDifficulty)
    {
        if (token == null)
        {
            return null;
        }

        if (token.Type == JTokenType.Integer)
        {
            return new QuestionExtractAbilityRelationContent
            {
                AbilityDimensionId = token.Value<long>(),
                DifficultyLevel = defaultDifficulty
            };
        }

        if (token.Type == JTokenType.String)
        {
            var text = token.ToString().Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return long.TryParse(text, out var id)
                ? new QuestionExtractAbilityRelationContent
                {
                    AbilityDimensionId = id,
                    DifficultyLevel = defaultDifficulty
                }
                : new QuestionExtractAbilityRelationContent
                {
                    AbilityDimensionName = text,
                    DifficultyLevel = defaultDifficulty
                };
        }

        if (token.Type != JTokenType.Object)
        {
            return null;
        }

        var abilityDimensionId = GetLongValue(token, "ability_dimension_id", "abilityDimensionId", "dimension_id", "dimensionId");
        var abilityDimensionName = GetStringValue(token, "ability_dimension_name", "abilityDimensionName", "dimension_name", "dimensionName", "name");
        var difficultyLevel = GetIntValue(token, "difficulty_level", "difficultyLevel", "difficulty") ?? defaultDifficulty;

        if (!abilityDimensionId.HasValue && string.IsNullOrWhiteSpace(abilityDimensionName))
        {
            return null;
        }

        return new QuestionExtractAbilityRelationContent
        {
            AbilityDimensionId = abilityDimensionId,
            AbilityDimensionName = abilityDimensionName,
            DifficultyLevel = difficultyLevel
        };
    }

    private static List<QuestionExtractStageContent> BuildQuestionStageImportContents(QuestionExtractContent content)
    {
        var stageList = new List<QuestionExtractStageContent>();
        var stageKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var stage in content?.BusinessStages ?? Enumerable.Empty<QuestionExtractStageContent>())
        {
            var normalizedStage = NormalizeQuestionStageContent(stage);
            if (normalizedStage == null)
            {
                continue;
            }

            var stageKey = BuildQuestionStageKey(normalizedStage.StageId, normalizedStage.StageName);
            if (stageKeys.Add(stageKey))
            {
                stageList.Add(normalizedStage);
            }
        }

        foreach (var question in content?.Questions ?? Enumerable.Empty<QuestionExtractQuestionContent>())
        {
            var normalizedStage = NormalizeQuestionStageContent(new QuestionExtractStageContent
            {
                StageId = question.StageId,
                StageName = question.StageName
            });
            if (normalizedStage == null)
            {
                continue;
            }

            var stageKey = BuildQuestionStageKey(normalizedStage.StageId, normalizedStage.StageName);
            if (stageKeys.Add(stageKey))
            {
                normalizedStage.SortNo = stageList.Count + 1;
                stageList.Add(normalizedStage);
            }
        }

        for (var i = 0; i < stageList.Count; i++)
        {
            if (stageList[i].SortNo <= 0)
            {
                stageList[i].SortNo = i + 1;
            }
        }

        return stageList;
    }

    private static QuestionExtractStageContent NormalizeQuestionStageContent(QuestionExtractStageContent stage)
    {
        if (stage == null)
        {
            return null;
        }

        var stageName = stage.StageName?.Trim();
        if (!stage.StageId.HasValue && string.IsNullOrWhiteSpace(stageName))
        {
            return null;
        }

        return new QuestionExtractStageContent
        {
            StageId = stage.StageId,
            StageName = stageName,
            StageDesc = stage.StageDesc?.Trim(),
            StageTask = stage.StageTask?.Trim(),
            SortNo = stage.SortNo,
            QuestionCount = stage.QuestionCount
        };
    }

    private static string BuildQuestionStageKey(long? stageId, string stageName)
    {
        if (stageId.HasValue && stageId.Value > 0)
        {
            return $"id:{stageId.Value}";
        }

        return $"name:{stageName?.Trim()}";
    }

    private static long ResolveStageId(
        QuestionExtractQuestionContent question,
        IReadOnlyDictionary<long, BusinessStageEntity> stageMap,
        IReadOnlyDictionary<string, long> uniqueStageNameMap)
    {
        if (question.StageId.HasValue && stageMap.ContainsKey(question.StageId.Value))
        {
            return question.StageId.Value;
        }

        var normalizedStageName = question.StageName?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedStageName) && uniqueStageNameMap.TryGetValue(normalizedStageName, out var stageId))
        {
            return stageId;
        }

        throw new Exception($"题目“{question.QuestionStem}”未匹配到有效业务环节。");
    }

    private static long ResolveStageId(
        QuestionExtractStageContent stage,
        IReadOnlyDictionary<long, BusinessStageEntity> stageMap,
        IReadOnlyDictionary<string, long> uniqueStageNameMap)
    {
        if (stage.StageId.HasValue && stageMap.ContainsKey(stage.StageId.Value))
        {
            return stage.StageId.Value;
        }

        var normalizedStageName = stage.StageName?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedStageName) && uniqueStageNameMap.TryGetValue(normalizedStageName, out var stageId))
        {
            return stageId;
        }

        throw new Exception($"业务环节“{normalizedStageName ?? stage.StageId?.ToString() ?? "未命名环节"}”未匹配到当前活动已有环节。");
    }

    private static long ResolveRoleId(
        QuestionExtractQuestionContent question,
        ISet<long> roleIdSet,
        IReadOnlyDictionary<string, long> uniqueRoleNameMap)
    {
        if (question.RoleId.HasValue)
        {
            if (!roleIdSet.Contains(question.RoleId.Value))
            {
                throw new Exception($"题目“{question.QuestionStem}”关联的角色ID不存在。");
            }

            return question.RoleId.Value;
        }

        var normalizedRoleName = question.RoleNickname?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedRoleName))
        {
            throw new Exception($"题目“{question.QuestionStem}”缺少角色信息。");
        }

        if (!uniqueRoleNameMap.TryGetValue(normalizedRoleName, out var roleId))
        {
            throw new Exception($"题目“{question.QuestionStem}”关联的角色“{normalizedRoleName}”不存在。");
        }

        return roleId;
    }

    private static long? ResolveAbilityDimensionId(
        QuestionExtractAbilityRelationContent relation,
        ISet<long> abilityIdSet,
        IReadOnlyDictionary<string, long> uniqueAbilityNameMap,
        string questionStem)
    {
        if (relation == null)
        {
            return null;
        }

        if (relation.AbilityDimensionId.HasValue)
        {
            if (!abilityIdSet.Contains(relation.AbilityDimensionId.Value))
            {
                throw new Exception($"题目“{questionStem}”关联的能力维度ID不存在。");
            }

            return relation.AbilityDimensionId.Value;
        }

        var normalizedAbilityName = relation.AbilityDimensionName?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedAbilityName))
        {
            return null;
        }

        if (!uniqueAbilityNameMap.TryGetValue(normalizedAbilityName, out var abilityDimensionId))
        {
            throw new Exception($"题目“{questionStem}”关联的能力维度“{normalizedAbilityName}”不存在。");
        }

        return abilityDimensionId;
    }

    private static Dictionary<string, long> BuildUniqueKeyMap<T>(IEnumerable<T> source, Func<T, string> keySelector, Func<T, long> valueSelector)
    {
        return source
            .Where(x => !string.IsNullOrWhiteSpace(keySelector(x)))
            .GroupBy(x => keySelector(x).Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() == 1)
            .ToDictionary(x => x.Key, x => valueSelector(x.First()), StringComparer.OrdinalIgnoreCase);
    }

    private static JToken GetPropertyValue(JToken token, params string[] propertyNames)
    {
        if (token?.Type != JTokenType.Object || propertyNames == null)
        {
            return null;
        }

        var obj = (JObject)token;
        foreach (var propertyName in propertyNames)
        {
            var property = obj.Properties().FirstOrDefault(x => string.Equals(x.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            if (property != null)
            {
                return property.Value;
            }
        }

        return null;
    }

    private static string GetStringValue(JToken token, params string[] propertyNames)
    {
        var valueToken = GetPropertyValue(token, propertyNames);
        if (valueToken == null)
        {
            return null;
        }

        if (valueToken.Type == JTokenType.Array)
        {
            var values = valueToken
                .Children()
                .Select(x => x?.ToString()?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x));
            return string.Join("; ", values);
        }

        var value = valueToken.ToString()?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static long? GetLongValue(JToken token, params string[] propertyNames)
    {
        var valueToken = GetPropertyValue(token, propertyNames);
        if (valueToken == null)
        {
            return null;
        }

        if (valueToken.Type == JTokenType.Integer)
        {
            return valueToken.Value<long>();
        }

        return valueToken.Type == JTokenType.String && long.TryParse(valueToken.ToString().Trim(), out var value)
            ? value
            : null;
    }

    private static ResponseInfo<T> Failed<T>(string message, T data = default)
    {
        return new ResponseInfo<T>
        {
            State = ResultState.Failed,
            Data = data,
            ErrorMessage = message
        };
    }

    private static ResponseInfo<bool> BuildMutationBoolResponse(int count, string errorMessage)
    {
        return new ResponseInfo<bool>
        {
            State = count > 0 ? ResultState.Successed : ResultState.Failed,
            Data = count > 0,
            ErrorMessage = count > 0 ? string.Empty : errorMessage
        };
    }

    private static ResponseInfo<string> BuildMutationStringResponse(int count, string data, string errorMessage)
    {
        return new ResponseInfo<string>
        {
            State = count > 0 ? ResultState.Successed : ResultState.Failed,
            Data = count > 0 ? data : string.Empty,
            ErrorMessage = count > 0 ? string.Empty : errorMessage
        };
    }

    private static (int pageIndex, int pageSize) NormalizePaging(int pageIndex, int pageSize, int defaultPageSize = 10)
    {
        return (pageIndex > 0 ? pageIndex : 1, pageSize > 0 ? pageSize : defaultPageSize);
    }

    private static int? GetIntValue(JToken token, params string[] propertyNames)
    {
        var valueToken = GetPropertyValue(token, propertyNames);
        if (valueToken == null)
        {
            return null;
        }

        if (valueToken.Type == JTokenType.Integer)
        {
            return valueToken.Value<int>();
        }

        return valueToken.Type == JTokenType.String && int.TryParse(valueToken.ToString().Trim(), out var value)
            ? value
            : null;
    }

    private static bool? GetBooleanValue(JToken token, params string[] propertyNames)
    {
        var valueToken = GetPropertyValue(token, propertyNames);
        if (valueToken == null)
        {
            return null;
        }

        return valueToken.Type switch
        {
            JTokenType.Boolean => valueToken.Value<bool>(),
            JTokenType.Integer => valueToken.Value<int>() > 0,
            JTokenType.String => ParseBooleanText(valueToken.ToString()),
            _ => null
        };
    }

    private static bool? ParseBooleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalizedText = text.Trim();
        if (bool.TryParse(normalizedText, out var result))
        {
            return result;
        }

        return normalizedText switch
        {
            "1" => true,
            "0" => false,
            "是" => true,
            "否" => false,
            "必答" => true,
            "非必答" => false,
            "必做" => true,
            "非必做" => false,
            _ => null
        };
    }

    private static byte NormalizeDifficultyLevel(int? difficultyLevel)
    {
        if (!difficultyLevel.HasValue || difficultyLevel.Value <= 0)
        {
            return 1;
        }

        return difficultyLevel.Value >= byte.MaxValue
            ? byte.MaxValue
            : (byte)difficultyLevel.Value;
    }

    private async Task<ResponseInfo<T>> WithOwnedActivityAsync<T>(
        long activityId,
        Func<ActivityEntity, Task<ResponseInfo<T>>> next,
        string emptyActivityIdMessage = "活动ID不能为空",
        string activityNotFoundMessage = "活动不存在",
        T failedData = default)
    {
        if (activityId <= 0)
        {
            return Failed(emptyActivityIdMessage, failedData);
        }

        var custCompanyId = GetCustCompanyIdOrNull();
        if (!custCompanyId.HasValue || custCompanyId.Value <= 0)
        {
            return Failed("所属机构不能为空", failedData);
        }

        var activity = await _activityRepository.SelectActivityAsync(activityId, custCompanyId.Value);
        if (activity == null)
        {
            return Failed(activityNotFoundMessage, failedData);
        }

        return await next(activity);
    }

    private int? GetCustCompanyIdOrNull()
    {
        var custCompanyId = _httpContextService.GetCustCompanyId();
        return custCompanyId > 0 ? custCompanyId : null;
    }

    private static class ActivityExtractTaskStatus
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Success = "Success";
        public const string Failed = "Failed";
    }

    private sealed class ActivityExtractTaskCacheDto
    {
        public string TaskId { get; set; }

        public int? CustCompanyId { get; set; }

        public string Status { get; set; }

        public string ResultJson { get; set; }

        public bool IsImported { get; set; }

        public string ImportResultJson { get; set; }

        public string ErrorMessage { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }
    }

    private sealed class QuestionExtractTaskCacheDto
    {
        public string TaskId { get; set; }

        public long ActivityId { get; set; }

        public int? CustCompanyId { get; set; }

        public string Status { get; set; }

        public string ResultJson { get; set; }

        public bool IsImported { get; set; }

        public string ImportResultJson { get; set; }

        public string ErrorMessage { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }
    }

    private sealed class ActivityExtractContent
    {
        public ActivityExtractActivityContent Activity { get; set; }

        public List<ActivityExtractRoleContent> Roles { get; set; }
    }

    private sealed class ActivityExtractActivityContent
    {
        public string ActivityName { get; set; }

        public string ActivityDesc { get; set; }

        public string CoreGoal { get; set; }

        public string CustomerBackground { get; set; }

        public int? RecommendedDurationMinutes { get; set; }
    }

    private sealed class ActivityExtractRoleContent
    {
        public string RoleNickname { get; set; }

        public string JobTitle { get; set; }

        public string ProjectRole { get; set; }

        public string Personality { get; set; }

        public string CommunicationStyle { get; set; }

        public string ProjectRequirement { get; set; }
        public int CustomerId { get; set; }
    }

    private sealed class ActivityExtractImportResult
    {
        public long ActivityId { get; set; }

        public int RoleCount { get; set; }
    }

    private sealed class QuestionExtractImportResult
    {
        public long ActivityId { get; set; }

        public int QuestionCount { get; set; }

        public int AbilityRelationCount { get; set; }

        public int BusinessStageCount { get; set; }
    }

    private sealed class QuestionExtractContent
    {
        public List<QuestionExtractStageContent> BusinessStages { get; set; }

        public List<QuestionExtractQuestionContent> Questions { get; set; }
    }

    private sealed class QuestionExtractStageContent
    {
        public long? StageId { get; set; }

        public string StageName { get; set; }

        public string StageDesc { get; set; }

        public string StageTask { get; set; }

        public int SortNo { get; set; }

        public int QuestionCount { get; set; }
    }

    private sealed class QuestionExtractQuestionContent
    {
        public string QuestionStem { get; set; }

        public string AssessmentPoints { get; set; }

        public bool IsRequired { get; set; }

        public long? RoleId { get; set; }

        public string RoleNickname { get; set; }

        public long? StageId { get; set; }

        public string StageName { get; set; }

        public List<QuestionExtractAbilityRelationContent> AbilityRelations { get; set; }
    }

    private sealed class QuestionExtractAbilityRelationContent
    {
        public long? AbilityDimensionId { get; set; }

        public string AbilityDimensionName { get; set; }

        public int? DifficultyLevel { get; set; }
    }

    private sealed class AbilityDimensionPromptItem
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public long? ParentId { get; set; }
    }

    private sealed class RolePromptItem
    {
        public long Id { get; set; }

        public string RoleNickname { get; set; }
    }
}
