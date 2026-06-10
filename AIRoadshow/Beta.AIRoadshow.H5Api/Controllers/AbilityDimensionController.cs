using Beta.AIRoadshow.Application;
using Beta.AIRoadshow.Entity.Dto.Response;
using Beta.Framework.Entity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.H5Api.Controllers;

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
    /// 根据机构ID查询能力维度列表
    /// </summary>
    [HttpGet]
    public Task<ResponseInfo<IEnumerable<AbilityDimensionTreeResponseDto>>> GetAbilityDimensionListByCustCompanyIdAsync([FromQuery] int custCompanyId)
        => _abilityDimensionService.GetAbilityDimensionListByCustCompanyIdAsync(custCompanyId);
}
