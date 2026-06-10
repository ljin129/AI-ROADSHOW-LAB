using Beta.AIRoadshow.DataAccess;
using Beta.AIRoadshow.Entity.DBEntity;
using Beta.AIRoadshow.Entity.Dto.Request;
using Beta.AIRoadshow.Entity.Dto.Response;
using Beta.Framework.Entity;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.Application;

/// <summary>
/// 【服务】能力维度
/// </summary>
[Service(ServiceLifetime.Transient)]
public class AbilityDimensionService : BaseService
{
    private readonly GrpcService _grpcService;
    private readonly AbilityDimensionRepository _abilityDimensionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AbilityDimensionService(GrpcService grpcService, AbilityDimensionRepository abilityDimensionRepository)
    {
        _grpcService = grpcService;
        _abilityDimensionRepository = abilityDimensionRepository;
    }

    /// <summary>
    /// 新增能力维度
    /// </summary>
    public async Task<ResponseInfo<string>> SaveAbilityDimensionAsync(AbilityDimensionSaveRequestDto request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.AbilityDimensionName))
        {
            return FailedString("能力维度名称不能为空");
        }

        if (request.ParentAbilityDimensionId.HasValue)
        {
            var parent = await _abilityDimensionRepository.SelectAbilityDimensionByIdAsync(request.ParentAbilityDimensionId.Value);
            if (parent == null)
            {
                return FailedString("父级能力维度不存在");
            }
        }

        var entity = request.MapTo<AbilityDimensionEntity>();
        entity.AbilityDimensionId = await _grpcService.GenSerialIdAsync();
        var count = await _abilityDimensionRepository.InsertAbilityDimensionAsync(entity);
        var result = count > 0;
        return new ResponseInfo<string>
        {
            State = result ? ResultState.Successed : ResultState.Failed,
            Data = result ? entity.AbilityDimensionId.ToString() : string.Empty,
            ErrorMessage = result ? string.Empty : "新增能力维度失败"
        };
    }

    /// <summary>
    /// 查询能力维度树
    /// </summary>
    public async Task<ResponseInfo<IEnumerable<AbilityDimensionTreeResponseDto>>> GetAbilityDimensionTreeAsync()
    {
        var abilityDimensions = (await _abilityDimensionRepository.SelectAbilityDimensionListAsync()).ToList();
        var tree = BuildTree(abilityDimensions, null);
        return new ResponseInfo<IEnumerable<AbilityDimensionTreeResponseDto>>
        {
            State = ResultState.Successed,
            Data = tree
        };
    }

    /// <summary>
    /// 根据机构ID查询能力维度列表
    /// </summary>
    public async Task<ResponseInfo<IEnumerable<AbilityDimensionTreeResponseDto>>> GetAbilityDimensionListByCustCompanyIdAsync(int custCompanyId)
    {
        if (custCompanyId <= 0)
        {
            return new ResponseInfo<IEnumerable<AbilityDimensionTreeResponseDto>>
            {
                State = ResultState.Failed,
                Data = new List<AbilityDimensionTreeResponseDto>(),
                ErrorMessage = "机构ID不能为空"
            };
        }

        var abilityDimensions = (await _abilityDimensionRepository.SelectAbilityDimensionListAsync()).ToList();
        var tree = BuildTree(abilityDimensions, null).ToList();
        return new ResponseInfo<IEnumerable<AbilityDimensionTreeResponseDto>>
        {
            State = ResultState.Successed,
            Data = tree
        };
    }

    /// <summary>
    /// 删除能力维度
    /// </summary>
    public async Task<ResponseInfo<bool>> RemoveAbilityDimensionAsync(long abilityDimensionId)
    {
        var abilityDimensions = (await _abilityDimensionRepository.SelectAbilityDimensionListAsync()).ToList();
        var current = abilityDimensions.FirstOrDefault(x => x.AbilityDimensionId == abilityDimensionId);
        if (current == null)
        {
            return FailedBool("能力维度不存在");
        }

        var removeIds = new List<long> { abilityDimensionId };
        CollectChildIds(abilityDimensions, abilityDimensionId, removeIds);
        var count = await _abilityDimensionRepository.DeleteAbilityDimensionsAsync(removeIds);
        return new ResponseInfo<bool>
        {
            State = count > 0 ? ResultState.Successed : ResultState.Failed,
            Data = count > 0,
            ErrorMessage = count > 0 ? string.Empty : "删除能力维度失败"
        };
    }

    /// <summary>
    /// 更新能力维度
    /// </summary>
    public async Task<ResponseInfo<bool>> UpdateAbilityDimensionAsync(AbilityDimensionUpdateRequestDto request)
    {
        if (request == null || request.AbilityDimensionId <= 0)
        {
            return FailedBool("能力维度ID不能为空");
        }

        if (string.IsNullOrWhiteSpace(request.AbilityDimensionName))
        {
            return FailedBool("能力维度名称不能为空");
        }

        var abilityDimensions = (await _abilityDimensionRepository.SelectAbilityDimensionListAsync()).ToList();
        var current = abilityDimensions.FirstOrDefault(x => x.AbilityDimensionId == request.AbilityDimensionId);
        if (current == null)
        {
            return FailedBool("能力维度不存在");
        }

        if (request.ParentAbilityDimensionId == request.AbilityDimensionId)
        {
            return FailedBool("父级能力维度不能指向自己");
        }

        if (request.ParentAbilityDimensionId.HasValue)
        {
            var parent = abilityDimensions.FirstOrDefault(x => x.AbilityDimensionId == request.ParentAbilityDimensionId.Value);
            if (parent == null)
            {
                return FailedBool("父级能力维度不存在");
            }

            var childIds = new List<long>();
            CollectChildIds(abilityDimensions, request.AbilityDimensionId, childIds);
            if (childIds.Contains(request.ParentAbilityDimensionId.Value))
            {
                return FailedBool("父级能力维度不能挂载到自己的子级节点下");
            }
        }

        current.AbilityDimensionName = request.AbilityDimensionName;
        current.AbilityDimensionDesc = request.AbilityDimensionDesc;
        current.Weight = request.Weight;
        current.ParentAbilityDimensionId = request.ParentAbilityDimensionId;
        var count = await _abilityDimensionRepository.UpdateAbilityDimensionAsync(current);
        return new ResponseInfo<bool>
        {
            State = count > 0 ? ResultState.Successed : ResultState.Failed,
            Data = count > 0,
            ErrorMessage = count > 0 ? string.Empty : "更新能力维度失败"
        };
    }

    private static IEnumerable<AbilityDimensionTreeResponseDto> BuildTree(IEnumerable<AbilityDimensionEntity> source, long? parentAbilityDimensionId)
    {
        var currentLevel = source
            .Where(x => x.ParentAbilityDimensionId == parentAbilityDimensionId)
            .OrderBy(x => x.CreateTime)
            .ThenBy(x => x.AbilityDimensionId)
            .ToList();

        return currentLevel.Select(x => new AbilityDimensionTreeResponseDto
        {
            AbilityDimensionId = x.AbilityDimensionId,
            AbilityDimensionName = x.AbilityDimensionName,
            AbilityDimensionDesc = x.AbilityDimensionDesc,
            Weight = x.Weight,
            ParentAbilityDimensionId = x.ParentAbilityDimensionId,
            Children = BuildTree(source, x.AbilityDimensionId).ToList()
        }).ToList();
    }

    private static void CollectChildIds(IEnumerable<AbilityDimensionEntity> source, long parentId, ICollection<long> result)
    {
        var childIds = source
            .Where(x => x.ParentAbilityDimensionId == parentId)
            .Select(x => x.AbilityDimensionId)
            .ToList();

        foreach (var childId in childIds)
        {
            if (!result.Contains(childId))
            {
                result.Add(childId);
            }
            CollectChildIds(source, childId, result);
        }
    }

    private static ResponseInfo<string> FailedString(string message)
    {
        return new ResponseInfo<string>
        {
            State = ResultState.Failed,
            Data = string.Empty,
            ErrorMessage = message
        };
    }

    private static ResponseInfo<bool> FailedBool(string message)
    {
        return new ResponseInfo<bool>
        {
            State = ResultState.Failed,
            Data = false,
            ErrorMessage = message
        };
    }
}
