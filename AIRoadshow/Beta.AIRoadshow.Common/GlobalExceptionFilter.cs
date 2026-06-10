using Beta.Framework.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Beta.AIRoadshow.Common;

/// <summary>
/// 
/// </summary>
public class GlobalExceptionFilter : ExceptionFilterAttribute
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnException(ExceptionContext context)
    {
        // 如果异常没有被处理则进行处理
        if (context.ExceptionHandled)
        {
            return;
        }
        _logger.LogError($"请求路径{context.HttpContext.Request.Path}。错误：{JsonConvert.SerializeObject(context.Exception)}。请求相关参数：{JsonConvert.SerializeObject(new { context.HttpContext.Request.Headers, context.HttpContext.Request.Query })}");

        // 定义返回信息
        var response = new ResponseInfo()
        {
            State = ResultState.Failed,
            ErrorMessage = context.Exception.Message
        };

        context.Result = new ContentResult
        {
            // 返回状态码设置为200，表示成功
            StatusCode = StatusCodes.Status200OK,
            // 设置返回格式
            ContentType = "application/json;charset=utf-8",
            Content = JsonConvert.SerializeObject(response)
        };

        // 处理异常
        context.ExceptionHandled = true;
    }
}