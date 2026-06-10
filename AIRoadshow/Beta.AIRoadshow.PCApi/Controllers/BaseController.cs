using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beta.AIRoadshow.PCApi.Controllers;

/// <summary>
/// 【控制器】基类
/// </summary>
[ApiController]
[Route("Beta.AIRoadshow.PCApi/[controller]/[action]")]
[Authorize]
public class BaseController : ControllerBase
{
}
