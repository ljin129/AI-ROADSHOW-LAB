using Beta.AIRoadshow.Entity.Dto.Request;
using Beta.Framework;
using Beta.Framework.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.Application;

/// <summary>
/// 【服务】大模型通用调用
/// </summary>
[Service(ServiceLifetime.Transient)]
public class LlmService : BaseService
{
    private static readonly TimeSpan DefaultHttpTimeout = TimeSpan.FromSeconds(300);

    private readonly CacheService _cacheService;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LlmService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public LlmService(
        CacheService cacheService,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<LlmService> logger)
    {
        _cacheService = cacheService;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 对话补全
    /// </summary>
    public async Task<ResponseInfo<object>> ChatCompletionAsync(ChatCompletionsRequestDto request)
    {
        if (request == null)
        {
            return Failed("请求参数不能为空");
        }

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            return Failed("模型名称不能为空");
        }

        if (request.Messages == null || !request.Messages.Any())
        {
            return Failed("消息列表不能为空");
        }

        var payload = new JObject
        {
            ["model"] = request.Model,
            ["messages"] = BuildMessages(request.Messages),
            ["stream"] = request.Stream
        };

        TrySetValue(payload, "temperature", request.Temperature);
        TrySetValue(payload, "top_p", request.TopP);
        TrySetValue(payload, "max_tokens", request.MaxTokens);

        var content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
        return await SendAsync(HttpMethod.Post, "/v1/chat/completions", content);
    }

    public async Task<ResponseInfo<string>> ChatCompletionStreamAsync(
        ChatCompletionsRequestDto request,
        Func<string, Task> onDelta,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return FailedString("璇锋眰鍙傛暟涓嶈兘涓虹┖");
        }

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            return FailedString("妯″瀷鍚嶇О涓嶈兘涓虹┖");
        }

        if (request.Messages == null || !request.Messages.Any())
        {
            return FailedString("娑堟伅鍒楄〃涓嶈兘涓虹┖");
        }

        var payload = new JObject
        {
            ["model"] = request.Model,
            ["messages"] = BuildMessages(request.Messages),
            ["stream"] = true
        };

        TrySetValue(payload, "temperature", request.Temperature);
        TrySetValue(payload, "top_p", request.TopP);
        TrySetValue(payload, "max_tokens", request.MaxTokens);

        var apiBaseUrl = _configuration.GetValue<string>("LLM:ApiBaseUrl");
        var apiKey = _configuration.GetValue<string>("LLM:ApiKey");
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            return FailedString("鏈厤缃甃LM:ApiBaseUrl");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return FailedString("鏈厤缃甃LM:ApiKey");
        }

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = DefaultHttpTimeout;
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, BuildUrl(apiBaseUrl, "/v1/chat/completions"));
        requestMessage.Headers.TryAddWithoutValidation("x-litellm-api-key", apiKey);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        requestMessage.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync(cancellationToken);
            return FailedString(BuildErrorMessage(errorBody, response.ReasonPhrase));
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(responseStream, Encoding.UTF8);
        var builder = new StringBuilder();

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var payloadLine = line[5..].Trim();
            if (string.Equals(payloadLine, "[DONE]", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            string delta;
            try
            {
                delta = ExtractStreamDelta(payloadLine);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                continue;
            }

            if (string.IsNullOrEmpty(delta))
            {
                continue;
            }

            builder.Append(delta);
            if (onDelta != null)
            {
                await onDelta(delta);
            }
        }

        return new ResponseInfo<string>
        {
            State = ResultState.Successed,
            Data = builder.ToString()
        };
    }

    /// <summary>
    /// 使用 Coze OCR 识别大附件文字内容
    /// </summary>
    public async Task<string> OcrByCoze(List<string> files, CancellationToken token = default)
    {
        if (files == null || files.Count == 0)
        {
            throw new BetaException("files不能为空");
        }

        var cacheKey = $"coze:ocr:{GetMd5Hash(new { Files = files })}";
        var cacheResult = await _cacheService.GetWithRedisAsync<string>(cacheKey);
        if (!string.IsNullOrWhiteSpace(cacheResult))
        {
            return cacheResult;
        }

        var apiHubCoze = _configuration["APIHubCoze"];
        var workflowId = _configuration["OcrWorkFlowId"];
        if (string.IsNullOrWhiteSpace(apiHubCoze) || string.IsNullOrWhiteSpace(workflowId))
        {
            ThrowOcrInvalidContent("OCR config missing: APIHubCoze or OcrWorkFlowId is empty.");
        }

        var request = new
        {
            workflow_id = workflowId,
            parameters = new
            {
                files = System.Text.Json.JsonSerializer.Serialize(files)
            },
            is_async = false
        };

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = DefaultHttpTimeout;
        using var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(apiHubCoze, content, token);
        var responseText = await response.Content.ReadAsStringAsync(token);
        if (!response.IsSuccessStatusCode)
        {
            ThrowOcrInvalidContent($"OCR request failed. StatusCode: {(int)response.StatusCode}, Response: {responseText}");
        }

        using var document = JsonDocument.Parse(responseText);
        if (!document.RootElement.TryGetProperty("data", out var dataNode))
        {
            ThrowOcrInvalidContent("OCR response missing data node.");
        }

        if (!dataNode.TryGetProperty("data", out var workflowDataNode))
        {
            ThrowOcrInvalidContent("OCR response missing workflow data node.");
        }

        var workflowData = workflowDataNode.GetString();
        if (string.IsNullOrWhiteSpace(workflowData))
        {
            ThrowOcrInvalidContent("OCR workflow data is empty.");
        }

        using var workflowDocument = JsonDocument.Parse(workflowData);
        if (!workflowDocument.RootElement.TryGetProperty("output", out var outputNode))
        {
            ThrowOcrInvalidContent("OCR output node is missing.");
        }

        string contentText = null;
        if (outputNode.ValueKind == JsonValueKind.String)
        {
            contentText = outputNode.GetString();
            if (string.IsNullOrWhiteSpace(contentText))
            {
                ThrowOcrInvalidContent("OCR output string is empty.");
            }
        }
        else if (outputNode.ValueKind == JsonValueKind.Array)
        {
            var result = new List<string>();
            var failMsg = string.Empty;
            foreach (var item in outputNode.EnumerateArray())
            {
                var code = item.TryGetProperty("code", out var codeNode) ? codeNode.GetInt32() : -1;
                var msg = item.TryGetProperty("msg", out var msgNode) ? msgNode.GetString() : string.Empty;
                if (code == 0 && item.TryGetProperty("data", out var itemDataNode) && itemDataNode.ValueKind == JsonValueKind.String)
                {
                    var text = itemDataNode.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        result.Add(text);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(msg))
                {
                    failMsg = msg;
                }
            }

            if (result.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(failMsg))
                {
                    _logger.LogWarning("Coze OCR failed message: {FailMsg}", failMsg);
                }

                ThrowOcrInvalidContent("OCR returned no valid content.");
            }

            contentText = string.Join("\n", result);
        }
        else
        {
            ThrowOcrInvalidContent("OCR output node is invalid.");
        }
        await _cacheService.SetWithRedisAsync(cacheKey, contentText, TimeSpan.FromHours(6));
        return contentText;
    }

    private async Task<ResponseInfo<object>> SendAsync(HttpMethod method, string path, HttpContent content = null)
    {
        var apiBaseUrl = _configuration.GetValue<string>("LLM:ApiBaseUrl");
        var apiKey = _configuration.GetValue<string>("LLM:ApiKey");

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            return Failed("未配置LLM:ApiBaseUrl");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Failed("未配置LLM:ApiKey");
        }

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = DefaultHttpTimeout;
        using var requestMessage = new HttpRequestMessage(method, BuildUrl(apiBaseUrl, path));
        requestMessage.Headers.TryAddWithoutValidation("x-litellm-api-key", apiKey);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (content != null)
        {
            requestMessage.Content = content;
        }

        using var response = await client.SendAsync(requestMessage);
        var responseBody = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            return new ResponseInfo<object>
            {
                State = ResultState.Failed,
                ErrorMessage = BuildErrorMessage(responseBody, response.ReasonPhrase)
            };
        }

        return new ResponseInfo<object>
        {
            State = ResultState.Successed,
            Data = DeserializeResponseBody(responseBody)
        };
    }

    private static string BuildUrl(string apiBaseUrl, string path)
    {
        var baseUrl = apiBaseUrl.TrimEnd('/');
        var requestPath = path.StartsWith("/") ? path : $"/{path}";

        if (baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) &&
            requestPath.StartsWith("/v1/", StringComparison.OrdinalIgnoreCase))
        {
            requestPath = requestPath[3..];
        }

        return $"{baseUrl}{requestPath}";
    }

    private static string BuildPath(string path, IDictionary<string, string> queryParameters)
    {
        var validQueryParameters = queryParameters?
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}")
            .ToList();

        if (validQueryParameters == null || validQueryParameters.Count == 0)
        {
            return path;
        }

        return $"{path}?{string.Join("&", validQueryParameters)}";
    }

    private static string GetMd5Hash(object value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(json));
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var item in bytes)
        {
            builder.Append(item.ToString("x2"));
        }

        return builder.ToString();
    }

    private static JArray BuildMessages(IEnumerable<ChatMessageDto> messages)
    {
        var result = new JArray();
        foreach (var item in messages)
        {
            result.Add(new JObject
            {
                ["role"] = item.Role,
                ["content"] = item.Content
            });
        }

        return result;
    }

    private static string ExtractStreamDelta(string payloadLine)
    {
        var token = JToken.Parse(payloadLine);
        var deltaToken = token["choices"]?.FirstOrDefault()?["delta"];
        if (deltaToken == null)
        {
            return null;
        }

        if (deltaToken["content"] is JValue contentValue)
        {
            return contentValue.ToString();
        }

        if (deltaToken["content"] is JArray contentArray)
        {
            var text = new StringBuilder();
            foreach (var item in contentArray)
            {
                var value = item?["text"]?.ToString()
                    ?? item?["content"]?.ToString()
                    ?? item?["value"]?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    text.Append(value);
                }
            }

            return text.ToString();
        }

        return deltaToken["text"]?.ToString();
    }

    private static void TrySetValue<T>(JObject target, string key, T value)
    {
        if (value == null)
        {
            return;
        }

        target[key] = JToken.FromObject(value);
    }

    private static object DeserializeResponseBody(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<object>(responseBody);
        }
        catch
        {
            return responseBody;
        }
    }

    private static string BuildErrorMessage(string responseBody, string fallbackMessage)
    {
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<JObject>(responseBody);
                var message = obj?["detail"]?["error"]?["message"]?.ToString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }

                return responseBody;
            }
            catch
            {
                return responseBody;
            }
        }

        return fallbackMessage ?? "调用大模型接口失败";
    }

    private static void ThrowOcrInvalidContent(string message)
    {
        throw new BetaException(message);
    }

    private static ResponseInfo<object> Failed(string message)
    {
        return new ResponseInfo<object>
        {
            State = ResultState.Failed,
            ErrorMessage = message
        };
    }

    private static ResponseInfo<string> FailedString(string message)
    {
        return new ResponseInfo<string>
        {
            State = ResultState.Failed,
            ErrorMessage = message
        };
    }
}
