using Microsoft.Extensions.DependencyInjection;

namespace Beta.AIRoadshow.Application;

/// <summary>
/// 
/// </summary>
public static class GrpcServiceExtension
{
    /// <summary>
    /// 注入自定义Service层
    /// </summary>
    public static IServiceCollection AddGrpcServiceExtension(this IServiceCollection services)
    {
        #region GRPC服务

        services.AddGrpcClient<Grpc.Auth.UserService.UserServiceClient>(opt =>
        {
            opt.ChannelOptionsActions.Add(cnl =>
            {
                cnl.MaxReceiveMessageSize = int.MaxValue;
                cnl.MaxSendMessageSize = int.MaxValue;
            });
        });

        services.AddGrpcClient<Serial.Grpc.SerialService.SerialServiceClient>(opt =>
        {
            opt.ChannelOptionsActions.Add(cnl =>
            {
                cnl.MaxReceiveMessageSize = int.MaxValue;
                cnl.MaxSendMessageSize = int.MaxValue;
            });
        });

        #endregion

        return services;
    }
}
