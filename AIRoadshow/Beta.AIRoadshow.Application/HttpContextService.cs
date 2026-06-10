using Beta.Framework;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Beta.AIRoadshow.Application;

/// <summary>
/// 【服务】请求上下文
/// </summary>
[Service(ServiceLifetime.Transient)]
public class HttpContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly GrpcService _grpcService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public HttpContextService(IHttpContextAccessor httpContextAccessor, GrpcService grpcService)
    {
        _httpContextAccessor = httpContextAccessor;
        _grpcService = grpcService;
    }

    public int GetCustCompanyId()
    {
        // 非JWT 即Uparm
        var uparms = GetUparms();
        if (!string.IsNullOrWhiteSpace(uparms))
        {
            var dict = BetaUnionAuthHelper.DecryptParmsForUnion(BetaUnionAuthHelper.CryptType.AESDecryptHexString, uparms);
            if (dict?.TryGetValue("ccid", out var ccidObj) == true && ccidObj != null)
            {
                if (int.TryParse(ccidObj?.ToString(), out var custCompanyId))
                {
                    return custCompanyId;
                }
            }
        }

        //JWT
        var jwtBetaUnionId = GetJWTBetaUnionId();
        if (jwtBetaUnionId > 0)
        {
            var userInfo = _grpcService.GetUserInfoByUnionIDAsync(jwtBetaUnionId).ConfigureAwait(false).GetAwaiter().GetResult();
            if (userInfo != null && userInfo.InternalUserID > 0)
            {
                return userInfo.CustCompanyID;
            }
        }

        

        return 0;
    }

    /// <summary>
    /// 获取BetaUnionId
    /// </summary>
    public long GetBetaUnionId()
    {
        // 非JWT 即Uparm
        var uparms = GetUparms();
        if (!string.IsNullOrWhiteSpace(uparms))
        {
            var dict = BetaUnionAuthHelper.DecryptParmsForUnion(BetaUnionAuthHelper.CryptType.AESDecryptHexString, uparms);
            if (dict?.TryGetValue("BetaUnionID", out var betaUnionIdObj) == true && betaUnionIdObj != null)
            {
                if (long.TryParse(betaUnionIdObj?.ToString(), out var betaUnionId))
                {
                    return betaUnionId;
                }
            }
        }

        //JWT
        var jwtBetaUnionId = GetJWTBetaUnionId();
        if (jwtBetaUnionId > 0)
        {
            return jwtBetaUnionId;
        }

        

        return 0;
    }

    /// <summary>
    ///  获取JWT BetaUnionId
    /// </summary>
    private long GetJWTBetaUnionId()
    {
        var context = _httpContextAccessor.HttpContext;

        if (context?.Items?.TryGetValue("ActorId", out var actorIdObj) == true && actorIdObj != null)
        {
            if (long.TryParse(actorIdObj?.ToString(), out var betaUnionId))
            {
                return betaUnionId;
            }
        }
        return 0;
    }

    /// <summary>
    /// 获取Uparms
    /// </summary>
    public string GetUparms()
    => GetParam("Uparms");

    /// <summary>
    /// 获取Refer参数
    /// </summary>
    public string GetParam(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return string.Empty;
        }
        if (httpContext.Request.Query.TryGetValue(name, out var queryValue) && !string.IsNullOrWhiteSpace(queryValue))
        {
            return queryValue.ToString();
        }
        if (httpContext.Request.Headers.TryGetValue("Referer", out var refererValue) && !string.IsNullOrWhiteSpace(refererValue))
        {
            return PageAuthHelper.ParseParamFromQuery(refererValue.ToString(), name);
        }
        return string.Empty;
    }
}