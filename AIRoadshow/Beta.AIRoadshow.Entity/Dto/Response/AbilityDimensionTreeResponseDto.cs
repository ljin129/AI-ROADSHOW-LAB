using System.Collections.Generic;

namespace Beta.AIRoadshow.Entity.Dto.Response;

/// <summary>
/// 【响应DTO】能力维度树节点
/// </summary>
public class AbilityDimensionTreeResponseDto : BaseResponseDto
{
    /// <summary>
    /// 能力维度ID
    /// </summary>
    public long AbilityDimensionId { get; set; }

    /// <summary>
    /// 能力维度名称
    /// </summary>
    public string AbilityDimensionName { get; set; }

    /// <summary>
    /// 能力维度描述
    /// </summary>
    public string AbilityDimensionDesc { get; set; }

    /// <summary>
    /// 权重
    /// </summary>
    public decimal Weight { get; set; }

    /// <summary>
    /// 父级能力维度ID
    /// </summary>
    public long? ParentAbilityDimensionId { get; set; }

    /// <summary>
    /// 子级节点
    /// </summary>
    public IEnumerable<AbilityDimensionTreeResponseDto> Children { get; set; } = new List<AbilityDimensionTreeResponseDto>();
}
