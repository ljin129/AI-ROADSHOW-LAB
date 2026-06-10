using AutoMapper;
using Beta.AIRoadshow.Entity.DBEntity;
using Beta.AIRoadshow.Entity.Dto.Request;
using Beta.AIRoadshow.Entity.Dto.Response;

namespace Beta.AIRoadshow.Application;

/// <summary>
/// AutoMapper 映射配置
/// </summary>
public class AutoMapperProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public AutoMapperProfile()
    {
        #region DTO转实体

        CreateMap<AbilityDimensionSaveRequestDto, AbilityDimensionEntity>();
        CreateMap<AbilityDimensionUpdateRequestDto, AbilityDimensionEntity>();

        #endregion

        #region 实体转DTO

        CreateMap<AbilityDimensionEntity, AbilityDimensionTreeResponseDto>();
        CreateMap<ActivityEntity, ActivityListItemResponseDto>();
        CreateMap<ActivityEntity, ActivityDetailActivityResponseDto>();
        CreateMap<RoleEntity, ActivityDetailRoleResponseDto>();
        CreateMap<BusinessStageEntity, ActivityDetailBusinessStageResponseDto>();
        CreateMap<BusinessStageEntity, ActivityBusinessStageResponseDto>();
        CreateMap<CustomerMongoDocument, CustomerResponseDto>();
        CreateMap<CustomerEmojiMongoDocument, CustomerEmojiResponseDto>();

        #endregion
    }
}
