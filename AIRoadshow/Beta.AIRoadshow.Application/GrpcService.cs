using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.Application;

/// <summary>
/// 【服务】Grpc服务
/// </summary>
[Service(ServiceLifetime.Transient)]
public class GrpcService : BaseService
{
    private readonly Serial.Grpc.SerialService.SerialServiceClient _serialServiceClient;
    private readonly Grpc.Auth.UserService.UserServiceClient _userServiceClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public GrpcService(Serial.Grpc.SerialService.SerialServiceClient serialServiceClient, Grpc.Auth.UserService.UserServiceClient userServiceClient)
    {
        _serialServiceClient = serialServiceClient;
        _userServiceClient = userServiceClient;
    }

    /// <summary>
    /// 获取雪花Id
    /// </summary>
    public async Task<long> GenSerialIdAsync()
    {
        var request = new Serial.Grpc.SerialIdRequest
        {
            SerialType = Serial.Grpc.SerialIdTypeEnum.BetaBusinessId
        };
        var response = await _serialServiceClient.GenSerialIdAsync(request);
        return response.SerialId;
    }

    /// <summary>
    /// 根据BetaUnionId获取用户信息
    /// </summary>
    /// <returns>返回用户信息</returns>
    public async Task<Grpc.Auth.Entity.UserInfoItem> GetUserInfoByUnionIDAsync(long userId)
    {
        var request = new Grpc.Auth.Request.RequestUnionID
        {
            UserId = userId
        };
        return await _userServiceClient.GetUserInfoByUnionIDAsync(request);
    }
}
