using Beta.AIRoadshow.Application;
using Beta.AIRoadshow.Entity.Dto.Request;
using Beta.AIRoadshow.Entity.Dto.Response;
using Beta.Framework.Entity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.PCApi.Controllers;

/// <summary>
/// 【控制器】路演活动
/// </summary>
public class ActivityController : BaseController
{
    private readonly ActivityService _activityService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ActivityController(ActivityService activityService)
    {
        _activityService = activityService;
    }

    /// <summary>
    /// 查询路演活动列表
    /// </summary>
    [HttpGet]
    public Task<ResponseInfo<ActivityListResponseDto>> GetActivityListAsync([FromQuery] ActivityListRequestDto request)
        => _activityService.GetActivityListAsync(request);

    /// <summary>
    /// 查询路演活动统计
    /// </summary>
    [HttpGet]
    public Task<ResponseInfo<ActivityStatisticsResponseDto>> GetActivityStatisticsAsync()
        => _activityService.GetActivityStatisticsAsync();

    /// <summary>
    /// 查询活动详情
    /// </summary>
    [HttpGet]
    public Task<ResponseInfo<ActivityDetailResponseDto>> GetActivityDetailAsync([FromQuery] long activityId)
        => _activityService.GetActivityDetailAsync(activityId);

    /// <summary>
    /// 根据活动ID更新活动主表信息
    /// </summary>
    [HttpPost]
    public Task<ResponseInfo<bool>> UpdateActivityAsync([FromBody] ActivityUpdateRequestDto request)
        => _activityService.UpdateActivityAsync(request);

    /// <summary>
    /// 根据活动ID删除活动信息
    /// </summary>
    [HttpPost]
    public Task<ResponseInfo<bool>> RemoveActivityAsync([FromBody] ActivityRemoveRequestDto request)
        => _activityService.RemoveActivityAsync(request.ActivityId);

    /// <summary>
    /// 新增角色信息
    /// </summary>
    [HttpPost]
    public Task<ResponseInfo<string>> SaveRoleAsync([FromBody] RoleSaveRequestDto request)
        => _activityService.SaveRoleAsync(request);

    /// <summary>
    /// 根据角色ID更新角色信息
    /// </summary>
    [HttpPost]
    public Task<ResponseInfo<bool>> UpdateRoleAsync([FromBody] RoleUpdateRequestDto request)
        => _activityService.UpdateRoleAsync(request);

    /// <summary>
    /// 根据角色ID删除角色信息
    /// </summary>
    [HttpPost]
    public Task<ResponseInfo<bool>> RemoveRoleAsync([FromBody] RoleRemoveRequestDto request)
        => _activityService.RemoveRoleAsync(request.RoleId);

    /// <summary>
    /// 查询活动题库分页
    /// </summary>
    [HttpGet]
    public Task<ResponseInfo<ActivityQuestionPageResponseDto>> GetActivityQuestionPageAsync([FromQuery] ActivityQuestionPageRequestDto request)
        => _activityService.GetActivityQuestionPageAsync(request);

    /// <summary>
    /// 查询活动业务环节列表
    /// </summary>
    [HttpGet]
    public Task<ResponseInfo<IEnumerable<ActivityBusinessStageResponseDto>>> GetActivityBusinessStageListAsync([FromQuery] long activityId)
        => _activityService.GetActivityBusinessStageListAsync(activityId);

    /// <summary>
    /// 新增业务环节信息
    /// </summary>
    [HttpPost]
    public Task<ResponseInfo<string>> SaveBusinessStageAsync([FromBody] BusinessStageSaveRequestDto request)
        => _activityService.SaveBusinessStageAsync(request);

    /// <summary>
    /// 根据业务环节ID更新业务环节信息
    /// </summary>
    [HttpPost]
    public Task<ResponseInfo<bool>> UpdateBusinessStageAsync([FromBody] BusinessStageUpdateRequestDto request)
        => _activityService.UpdateBusinessStageAsync(request);

    /// <summary>
    /// 根据业务环节ID删除业务环节信息
    /// </summary>
    [HttpPost]
    public Task<ResponseInfo<bool>> RemoveBusinessStageAsync([FromBody] BusinessStageRemoveRequestDto request)
        => _activityService.RemoveBusinessStageAsync(request.StageId);

    /// <summary>
    /// 新增题目信息
    /// </summary>
    [HttpPost]
    public Task<ResponseInfo<string>> SaveQuestionAsync([FromBody] QuestionSaveRequestDto request)
        => _activityService.SaveQuestionAsync(request);

    /// <summary>
    /// 根据题目ID更新题目信息
    /// </summary>
    [HttpPost]
    public Task<ResponseInfo<bool>> UpdateQuestionAsync([FromBody] QuestionUpdateRequestDto request)
        => _activityService.UpdateQuestionAsync(request);

    /// <summary>
    /// 根据题目ID删除题目信息
    /// </summary>
    [HttpPost]
    public Task<ResponseInfo<bool>> RemoveQuestionAsync([FromBody] QuestionRemoveRequestDto request)
        => _activityService.RemoveQuestionAsync(request.QuestionId);

    /// <summary>
    /// 根据题目ID批量删除题目信息
    /// </summary>
    [HttpPost]
    public Task<ResponseInfo<bool>> BatchRemoveQuestionAsync([FromBody] QuestionBatchRemoveRequestDto request)
        => _activityService.BatchRemoveQuestionAsync(request?.QuestionIds);

    /// <summary>
    /// 提交活动信息提取任务
    /// </summary>
    [HttpPost]
    public Task<ResponseInfo<ActivityExtractSubmitResponseDto>> ExtractActivityInfoAsync([FromBody] ActivityExtractRequestDto request)
        => _activityService.ExtractActivityInfoAsync(request);

    /// <summary>
    /// 提交题库信息提取任务
    /// </summary>
    [HttpPost]
    public Task<ResponseInfo<QuestionExtractSubmitResponseDto>> ExtractQuestionInfoAsync([FromBody] QuestionExtractRequestDto request)
        => _activityService.ExtractQuestionInfoAsync(request);

    /// <summary>
    /// 查询活动信息提取结果
    /// </summary>
    [HttpGet]
    public Task<ResponseInfo<ActivityExtractResultResponseDto>> GetExtractActivityInfoResultAsync([FromQuery] string taskId)
        => _activityService.GetExtractActivityInfoResultAsync(taskId);

    /// <summary>
    /// 查询题库信息提取结果
    /// </summary>
    [HttpGet]
    public Task<ResponseInfo<QuestionExtractResultResponseDto>> GetExtractQuestionInfoResultAsync([FromQuery] string taskId, [FromQuery] long activityId)
        => _activityService.GetExtractQuestionInfoResultAsync(taskId, activityId);
}
