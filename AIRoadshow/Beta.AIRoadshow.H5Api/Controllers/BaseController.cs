using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beta.AIRoadshow.H5Api.Controllers;

/// <summary>
/// 【控制器】基类
/// </summary>
[ApiController]
[Route("Beta.AIRoadshow.H5Api/[controller]/[action]")]
public class BaseController : ControllerBase
{
}
