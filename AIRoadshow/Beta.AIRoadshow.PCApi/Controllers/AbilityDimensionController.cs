using Beta.AIRoadshow.Application;
using Beta.AIRoadshow.Entity.Dto.Request;
using Beta.AIRoadshow.Entity.Dto.Response;
using Beta.Framework.Entity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.PCApi.Controllers;

/// <summary>
/// 【控制器】能力维度
/// </summary>
public class AbilityDimensionController : BaseController
{
    private readonly AbilityDimensionService _abilityDimensionService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AbilityDimensionController(AbilityDimensionService abilityDimensionService)
    {
        _abilityDimensionService = abilityDimensionService;
    }

    /// <summary>
    /// 新增能力维度信息
    /// </summary>
    [HttpPost]
    public async Task<ResponseInfo<string>> SaveAbilityDimensionAsync([FromBody] AbilityDimensionSaveRequestDto request)
        => await _abilityDimensionService.SaveAbilityDimensionAsync(request);

    /// <summary>
    /// 查询能力维度树状结构的全量数据
    /// </summary>
    [HttpGet]
    public async Task<ResponseInfo<IEnumerable<AbilityDimensionTreeResponseDto>>> GetAbilityDimensionTreeAsync()
        => await _abilityDimensionService.GetAbilityDimensionTreeAsync();

    /// <summary>
    /// 根据能力维度ID删除数据，同时删除下属子级
    /// </summary>
    [HttpPost]
    public async Task<ResponseInfo<bool>> RemoveAbilityDimensionAsync([FromBody] AbilityDimensionRemoveRequestDto request)
        => await _abilityDimensionService.RemoveAbilityDimensionAsync(request.AbilityDimensionId);

    /// <summary>
    /// 根据能力维度ID更新单条数据内容
    /// </summary>
    [HttpPost]
    public async Task<ResponseInfo<bool>> UpdateAbilityDimensionAsync([FromBody] AbilityDimensionUpdateRequestDto request)
        => await _abilityDimensionService.UpdateAbilityDimensionAsync(request);
}
