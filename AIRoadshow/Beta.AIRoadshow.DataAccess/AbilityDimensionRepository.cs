using Beta.AIRoadshow.Entity.DBEntity;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.DataAccess;

/// <summary>
/// 【仓存】能力维度
/// </summary>
[Service(ServiceLifetime.Transient)]
public class AbilityDimensionRepository : BaseRepository<AIRoadshowDbContext>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public AbilityDimensionRepository(AIRoadshowDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 插入能力维度
    /// </summary>
    public async Task<int> InsertAbilityDimensionAsync(AbilityDimensionEntity abilityDimension)
    {
        const string sql = @"INSERT INTO `roadshow_ability_dimension`
                            (
                                `ability_dimension_id`,
                                `ability_dimension_name`,
                                `ability_dimension_desc`,
                                `weight`,
                                `parent_ability_dimension_id`,
                                `is_deleted`,
                                `create_time`,
                                `update_time`
                            ) VALUES
                            (
                                @AbilityDimensionId,
                                @AbilityDimensionName,
                                @AbilityDimensionDesc,
                                @Weight,
                                @ParentAbilityDimensionId,
                                0,
                                NOW(),
                                NOW()
                            );";
        return await ExecuteAsync(sql, abilityDimension);
    }

    /// <summary>
    /// 查询能力维度列表
    /// </summary>
    public async Task<IEnumerable<AbilityDimensionEntity>> SelectAbilityDimensionListAsync()
    {
        const string sql = @"SELECT
                                `ability_dimension_id`,
                                `ability_dimension_name`,
                                `ability_dimension_desc`,
                                `weight`,
                                `parent_ability_dimension_id`,
                                `is_deleted`,
                                `create_time`,
                                `update_time`
                             FROM `roadshow_ability_dimension`
                             WHERE `is_deleted` = 0
                             ORDER BY
                                CASE WHEN `parent_ability_dimension_id` IS NULL THEN 0 ELSE 1 END,
                                `parent_ability_dimension_id`,
                                `create_time`,
                                `ability_dimension_id`;";
        return await QueryAsync<AbilityDimensionEntity>(sql);
    }

    /// <summary>
    /// 根据ID查询能力维度
    /// </summary>
    public async Task<AbilityDimensionEntity> SelectAbilityDimensionByIdAsync(long abilityDimensionId)
    {
        const string sql = @"SELECT
                                `ability_dimension_id`,
                                `ability_dimension_name`,
                                `ability_dimension_desc`,
                                `weight`,
                                `parent_ability_dimension_id`,
                                `is_deleted`,
                                `create_time`,
                                `update_time`
                             FROM `roadshow_ability_dimension`
                             WHERE `ability_dimension_id` = @AbilityDimensionId
                               AND `is_deleted` = 0
                             LIMIT 1;";
        return await QueryFirstOrDefaultAsync<AbilityDimensionEntity>(sql, new
        {
            AbilityDimensionId = abilityDimensionId
        });
    }

    /// <summary>
    /// 更新能力维度
    /// </summary>
    public async Task<int> UpdateAbilityDimensionAsync(AbilityDimensionEntity abilityDimension)
    {
        const string sql = @"UPDATE `roadshow_ability_dimension`
                             SET `ability_dimension_name` = @AbilityDimensionName,
                                 `ability_dimension_desc` = @AbilityDimensionDesc,
                                 `weight` = @Weight,
                                 `parent_ability_dimension_id` = @ParentAbilityDimensionId,
                                 `update_time` = NOW()
                             WHERE `ability_dimension_id` = @AbilityDimensionId
                               AND `is_deleted` = 0;";
        return await ExecuteAsync(sql, abilityDimension);
    }

    /// <summary>
    /// 删除能力维度及其子级
    /// </summary>
    public async Task<int> DeleteAbilityDimensionsAsync(IEnumerable<long> abilityDimensionIds)
    {
        var idList = abilityDimensionIds?.Distinct().ToArray() ?? Array.Empty<long>();
        if (idList.Length == 0)
        {
            return 0;
        }

        const string sql = @"UPDATE `roadshow_ability_dimension`
                             SET `is_deleted` = 1,
                                 `update_time` = NOW()
                             WHERE `ability_dimension_id` IN @AbilityDimensionIds
                               AND `is_deleted` = 0;";
        using IDbConnection conn = GetCurrentDbConnection();
        var command = new CommandDefinition(sql, new
        {
            AbilityDimensionIds = idList
        });
        return await conn.ExecuteAsync(command);
    }
}
