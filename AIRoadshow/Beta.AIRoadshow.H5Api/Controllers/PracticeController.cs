using Beta.AIRoadshow.Application;
using Beta.AIRoadshow.Entity.Dto.Request;
using Beta.AIRoadshow.Entity.Dto.Response;
using Beta.Framework.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.H5Api.Controllers;

/// <summary>
/// H5端路演演练接口。
/// </summary>
public class PracticeController : BaseController
{
    private readonly PracticeService _practiceService;

    /// <summary>
    /// 初始化路演演练控制器。
    /// </summary>
    public PracticeController(PracticeService practiceService)
    {
        _practiceService = practiceService;
    }

    /// <summary>
    /// 创建一条新的演练记录。
    /// </summary>
    [HttpPost]
    public Task<ResponseInfo<PracticeStartResponseDto>> StartPracticeAsync([FromBody] PracticeStartRequestDto request)
        => _practiceService.StartPracticeAsync(request);

    /// <summary>
    /// 根据演练记录ID获取测评明细。
    /// </summary>
    [HttpGet]
    public Task<ResponseInfo<PracticeDetailResponseDto>> GetPracticeDetailAsync([FromQuery] long practiceRecordId)
        => _practiceService.GetPracticeDetailAsync(practiceRecordId);

    /// <summary>
    /// 以SSE方式流式返回演练对话事件。
    /// </summary>
    [HttpPost]
    public async Task PracticeChatStreamAsync([FromBody] PracticeChatRequestDto request)
    {
        Response.StatusCode = 200;
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers.Append("X-Accel-Buffering", "no");

        await _practiceService.PracticeChatStreamAsync(request, async (eventName, data) =>
        {
            await WriteSseEventAsync(eventName, data);
        }, HttpContext.RequestAborted);
    }

    private async Task WriteSseEventAsync(string eventName, object data)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            eventName = "message";
        }

        var payload = data == null ? "{}" : JsonConvert.SerializeObject(data);
        await Response.WriteAsync($"event: {eventName}\n");
        foreach (var line in payload.Split('\n', StringSplitOptions.None))
        {
            await Response.WriteAsync($"data: {line}\n");
        }

        await Response.WriteAsync("\n");
        await Response.Body.FlushAsync(HttpContext.RequestAborted);
    }
}
