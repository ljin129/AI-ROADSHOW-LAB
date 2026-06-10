using Beta.AIRoadshow.Application;
using Beta.AIRoadshow.Entity.Dto.Request;
using Beta.AIRoadshow.Entity.Dto.Response;
using Beta.Framework.Entity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.H5Api.Controllers;

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
    /// 查询当前机构下已发布活动列表
    /// </summary>
    [HttpGet]
    public Task<ResponseInfo<ActivityListResponseDto>> GetPublishedActivityListAsync([FromQuery] ActivityListRequestDto request)
        => _activityService.GetPublishedActivityListAsync(request);

    /// <summary>
    /// 根据活动ID查询活动详情
    /// </summary>
    [HttpGet]
    public Task<ResponseInfo<ActivityDetailResponseDto>> GetActivityDetailAsync([FromQuery] long activityId)
        => _activityService.GetActivityDetailAsync(activityId);
}
