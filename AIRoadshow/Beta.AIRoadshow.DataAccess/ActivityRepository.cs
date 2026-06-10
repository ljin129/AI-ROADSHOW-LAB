using Beta.AIRoadshow.Entity.DBEntity;
using Beta.AIRoadshow.Entity.Dto.Request;
using Beta.AIRoadshow.Entity.Dto.Response;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.DataAccess;

/// <summary>
/// 【仓存】路演活动
/// </summary>
[Service(ServiceLifetime.Transient)]
public class ActivityRepository : BaseRepository<AIRoadshowDbContext>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ActivityRepository(AIRoadshowDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 查询活动分页列表
    /// </summary>
    public async Task<(int totalCount, IEnumerable<ActivityEntity> items)> SelectActivityPageAsync(ActivityListRequestDto request, int? custCompanyId)
    {
        var pageIndex = request?.PageIndex > 0 ? request.PageIndex : 1;
        var pageSize = request?.PageSize > 0 ? request.PageSize : 10;
        var offset = (pageIndex - 1) * pageSize;

        var whereSql = BuildWhereSql(request, custCompanyId, out var parameters);
        var countSql = $"SELECT COUNT(1) FROM `roadshow_activity` WHERE {whereSql};";
        var listSql = $@"SELECT
                            `activity_id`,
                            `cust_company_id`,
                            `activity_name`,
                            `activity_desc`,
                            `cover_image`,
                            `core_goal`,
                            `customer_background`,
                            `start_time`,
                            `end_time`,
                            `recommended_duration_minutes`,
                            `publish_status`,
                            `is_deleted`,
                            `create_time`,
                            `update_time`
                         FROM `roadshow_activity`
                         WHERE {whereSql}
                         ORDER BY `create_time` DESC, `activity_id` DESC
                         LIMIT @Offset, @PageSize;";

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        using IDbConnection conn = GetCurrentDbConnection();
        var totalCount = await conn.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters));
        var items = await conn.QueryAsync<ActivityEntity>(new CommandDefinition(listSql, parameters));
        return (totalCount, items);
    }

    /// <summary>
    /// 查询当前机构下已发布活动分页列表
    /// </summary>
    public async Task<(int totalCount, IEnumerable<ActivityEntity> items)> SelectPublishedActivityPageAsync(ActivityListRequestDto request, int custCompanyId)
    {
        var pageIndex = request?.PageIndex > 0 ? request.PageIndex : 1;
        var pageSize = request?.PageSize > 0 ? request.PageSize : 10;
        var offset = (pageIndex - 1) * pageSize;

        request ??= new ActivityListRequestDto();
        request.PublishStatus = 1;

        var whereSql = BuildWhereSql(request, custCompanyId, out var parameters);
        var countSql = $"SELECT COUNT(1) FROM `roadshow_activity` WHERE {whereSql};";
        var listSql = $@"SELECT
                            `activity_id`,
                            `cust_company_id`,
                            `activity_name`,
                            `activity_desc`,
                            `cover_image`,
                            `core_goal`,
                            `customer_background`,
                            `start_time`,
                            `end_time`,
                            `recommended_duration_minutes`,
                            `publish_status`,
                            `is_deleted`,
                            `create_time`,
                            `update_time`
                         FROM `roadshow_activity`
                         WHERE {whereSql}
                         ORDER BY `start_time` ASC, `activity_id` DESC
                         LIMIT @Offset, @PageSize;";

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        using IDbConnection conn = GetCurrentDbConnection();
        var totalCount = await conn.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters));
        var items = await conn.QueryAsync<ActivityEntity>(new CommandDefinition(listSql, parameters));
        return (totalCount, items);
    }

    /// <summary>
    /// 查询活动统计
    /// </summary>
    public async Task<ActivityStatisticsResponseDto> SelectActivityStatisticsAsync(int? custCompanyId)
    {
        var sql = new StringBuilder(@"
            SELECT
                COUNT(1) AS TotalCount,
                SUM(CASE WHEN `publish_status` = 1 THEN 1 ELSE 0 END) AS PublishedCount,
                SUM(CASE WHEN `publish_status` <> 1 THEN 1 ELSE 0 END) AS UnpublishedCount,
                SUM(CASE
                        WHEN DATE_FORMAT(`create_time`, '%Y-%m') = DATE_FORMAT(NOW(), '%Y-%m')
                        THEN 1 ELSE 0
                    END) AS CurrentMonthNewCount
            FROM `roadshow_activity`
            WHERE `is_deleted` = 0");

        var parameters = new DynamicParameters();
        if (custCompanyId.HasValue && custCompanyId.Value > 0)
        {
            sql.Append(" AND `cust_company_id` = @CustCompanyId");
            parameters.Add("CustCompanyId", custCompanyId.Value);
        }

        return await QueryFirstOrDefaultAsync<ActivityStatisticsResponseDto>(sql.ToString(), parameters);
    }

    /// <summary>
    /// 查询活动详情
    /// </summary>
    public async Task<ActivityDetailQueryResult> SelectActivityDetailAsync(long activityId, int custCompanyId)
    {
        const string sql = @"
            SELECT
                `activity_id`,
                `cust_company_id`,
                `activity_name`,
                `activity_desc`,
                `cover_image`,
                `core_goal`,
                `customer_background`,
                `start_time`,
                `end_time`,
                `recommended_duration_minutes`,
                `publish_status`,
                `is_deleted`,
                `create_time`,
                `update_time`
            FROM `roadshow_activity`
            WHERE `activity_id` = @ActivityId
              AND `cust_company_id` = @CustCompanyId
              AND `is_deleted` = 0
            LIMIT 1;

            SELECT
                `role_id`,
                `activity_id`,
                `customer_id`,
                `role_nickname`,
                `job_title`,
                `project_role`,
                `personality`,
                `communication_style`,
                `project_requirement`,
                `is_deleted`,
                `create_time`,
                `update_time`
            FROM `roadshow_role`
            WHERE `activity_id` = @ActivityId
              AND `is_deleted` = 0
            ORDER BY `create_time`, `role_id`;

            SELECT
                `stage_id`,
                `activity_id`,
                `stage_name`,
                `stage_desc`,
                `stage_task`,
                `sort_no`,
                `question_count`,
                `is_deleted`,
                `create_time`,
                `update_time`
            FROM `roadshow_business_stage`
            WHERE `activity_id` = @ActivityId
              AND `is_deleted` = 0
            ORDER BY `sort_no`, `create_time`, `stage_id`;";

        using IDbConnection conn = GetCurrentDbConnection();
        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(sql, new
        {
            ActivityId = activityId,
            CustCompanyId = custCompanyId
        }));

        return new ActivityDetailQueryResult
        {
            Activity = await multi.ReadFirstOrDefaultAsync<ActivityEntity>(),
            Roles = (await multi.ReadAsync<RoleEntity>()).ToList(),
            BusinessStages = (await multi.ReadAsync<BusinessStageEntity>()).ToList()
        };
    }

    public async Task<ActivityEntity> SelectActivityAsync(long activityId, int custCompanyId)
    {
        const string sql = @"
            SELECT
                `activity_id`,
                `cust_company_id`,
                `activity_name`,
                `activity_desc`,
                `cover_image`,
                `core_goal`,
                `customer_background`,
                `start_time`,
                `end_time`,
                `recommended_duration_minutes`,
                `publish_status`,
                `is_deleted`,
                `create_time`,
                `update_time`
            FROM `roadshow_activity`
            WHERE `activity_id` = @ActivityId
              AND `cust_company_id` = @CustCompanyId
              AND `is_deleted` = 0
            LIMIT 1;";

        return await QueryFirstOrDefaultAsync<ActivityEntity>(sql, new
        {
            ActivityId = activityId,
            CustCompanyId = custCompanyId
        });
    }

    public async Task<ActivityEntity> SelectActivityByIdAsync(long activityId)
    {
        const string sql = @"
            SELECT
                `activity_id`,
                `cust_company_id`,
                `activity_name`,
                `activity_desc`,
                `cover_image`,
                `core_goal`,
                `customer_background`,
                `start_time`,
                `end_time`,
                `recommended_duration_minutes`,
                `publish_status`,
                `is_deleted`,
                `create_time`,
                `update_time`
            FROM `roadshow_activity`
            WHERE `activity_id` = @ActivityId
              AND `is_deleted` = 0
            LIMIT 1;";

        return await QueryFirstOrDefaultAsync<ActivityEntity>(sql, new
        {
            ActivityId = activityId
        });
    }

    public async Task<RoleEntity> SelectRoleByIdAsync(long roleId)
    {
        const string sql = @"
            SELECT
                `role_id`,
                `activity_id`,
                `customer_id`,
                `role_nickname`,
                `job_title`,
                `project_role`,
                `personality`,
                `communication_style`,
                `project_requirement`,
                `is_deleted`,
                `create_time`,
                `update_time`
            FROM `roadshow_role`
            WHERE `role_id` = @RoleId
              AND `is_deleted` = 0
            LIMIT 1;";

        return await QueryFirstOrDefaultAsync<RoleEntity>(sql, new
        {
            RoleId = roleId
        });
    }

    public async Task<BusinessStageEntity> SelectBusinessStageByIdAsync(long stageId)
    {
        const string sql = @"
            SELECT
                `stage_id`,
                `activity_id`,
                `stage_name`,
                `stage_desc`,
                `stage_task`,
                `sort_no`,
                `question_count`,
                `is_deleted`,
                `create_time`,
                `update_time`
            FROM `roadshow_business_stage`
            WHERE `stage_id` = @StageId
              AND `is_deleted` = 0
            LIMIT 1;";

        return await QueryFirstOrDefaultAsync<BusinessStageEntity>(sql, new
        {
            StageId = stageId
        });
    }

    public async Task<QuestionBankEntity> SelectQuestionByIdAsync(long questionId)
    {
        const string sql = @"
            SELECT
                `question_id`,
                `activity_id`,
                `role_id`,
                `question_stem`,
                `assessment_points`,
                `is_required`,
                `stage_id`,
                `is_deleted`,
                `create_time`,
                `update_time`
            FROM `roadshow_question_bank`
            WHERE `question_id` = @QuestionId
              AND `is_deleted` = 0
            LIMIT 1;";

        return await QueryFirstOrDefaultAsync<QuestionBankEntity>(sql, new
        {
            QuestionId = questionId
        });
    }

    public async Task<IEnumerable<QuestionBankEntity>> SelectQuestionsByIdsAsync(IEnumerable<long> questionIds)
    {
        var idList = questionIds?.Distinct().ToArray() ?? System.Array.Empty<long>();
        if (idList.Length == 0)
        {
            return System.Array.Empty<QuestionBankEntity>();
        }

        const string sql = @"
            SELECT
                `question_id`,
                `activity_id`,
                `role_id`,
                `question_stem`,
                `assessment_points`,
                `is_required`,
                `stage_id`,
                `is_deleted`,
                `create_time`,
                `update_time`
            FROM `roadshow_question_bank`
            WHERE `question_id` IN @QuestionIds
              AND `is_deleted` = 0;";

        using IDbConnection conn = GetCurrentDbConnection();
        return await conn.QueryAsync<QuestionBankEntity>(new CommandDefinition(sql, new
        {
            QuestionIds = idList
        }));
    }

    public async Task<(int totalCount, IEnumerable<ActivityQuestionItemResponseDto> items)> SelectActivityQuestionPageAsync(ActivityQuestionPageRequestDto request)
    {
        var pageIndex = request?.PageIndex > 0 ? request.PageIndex : 1;
        var pageSize = request?.PageSize > 0 ? request.PageSize : 10;
        var offset = (pageIndex - 1) * pageSize;

        var parameters = new DynamicParameters();
        parameters.Add("ActivityId", request?.ActivityId ?? 0);
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var whereSql = new StringBuilder("q.`activity_id` = @ActivityId AND q.`is_deleted` = 0");

        if (request?.StageId.HasValue == true && request.StageId.Value > 0)
        {
            whereSql.Append(" AND q.`stage_id` = @StageId");
            parameters.Add("StageId", request.StageId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request?.Keyword))
        {
            whereSql.Append(" AND (q.`question_stem` LIKE @Keyword OR q.`assessment_points` LIKE @Keyword)");
            parameters.Add("Keyword", $"%{request.Keyword.Trim()}%");
        }

        var countSql = $@"SELECT COUNT(1)
                          FROM `roadshow_question_bank` q
                          INNER JOIN `roadshow_business_stage` s
                                  ON s.`stage_id` = q.`stage_id`
                                 AND s.`is_deleted` = 0
                          WHERE {whereSql};";

        var listSql = $@"SELECT
                            q.`question_id` AS QuestionId,
                            q.`activity_id` AS ActivityId,
                            q.`role_id` AS RoleId,
                            r.`role_nickname` AS RoleNickname,
                            q.`question_stem` AS QuestionStem,
                            q.`assessment_points` AS AssessmentPoints,
                            q.`is_required` AS IsRequired,
                            q.`stage_id` AS StageId,
                            s.`stage_name` AS StageName,
                            q.`create_time` AS CreateTime,
                            q.`update_time` AS UpdateTime
                         FROM `roadshow_question_bank` q
                         INNER JOIN `roadshow_business_stage` s
                                 ON s.`stage_id` = q.`stage_id`
                                AND s.`is_deleted` = 0
                         LEFT JOIN `roadshow_role` r
                                ON r.`role_id` = q.`role_id`
                               AND r.`is_deleted` = 0
                         WHERE {whereSql}
                         ORDER BY q.`create_time` DESC, q.`question_id` DESC
                         LIMIT @Offset, @PageSize;";

        using IDbConnection conn = GetCurrentDbConnection();
        var totalCount = await conn.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters));
        var items = await conn.QueryAsync<ActivityQuestionItemResponseDto>(new CommandDefinition(listSql, parameters));
        return (totalCount, items);
    }

    public async Task<IEnumerable<BusinessStageEntity>> SelectActivityBusinessStagesAsync(long activityId)
    {
        const string sql = @"
            SELECT
                `stage_id`,
                `activity_id`,
                `stage_name`,
                `stage_desc`,
                `stage_task`,
                `sort_no`,
                `question_count`,
                `is_deleted`,
                `create_time`,
                `update_time`
            FROM `roadshow_business_stage`
            WHERE `activity_id` = @ActivityId
              AND `is_deleted` = 0
            ORDER BY `sort_no`, `create_time`, `stage_id`;";

        return await QueryAsync<BusinessStageEntity>(sql, new
        {
            ActivityId = activityId
        });
    }

    public async Task<IEnumerable<RoleEntity>> SelectActivityRolesAsync(long activityId)
    {
        const string sql = @"
            SELECT
                `role_id`,
                `activity_id`,
                `customer_id`,
                `role_nickname`,
                `job_title`,
                `project_role`,
                `personality`,
                `communication_style`,
                `project_requirement`,
                `is_deleted`,
                `create_time`,
                `update_time`
            FROM `roadshow_role`
            WHERE `activity_id` = @ActivityId
              AND `is_deleted` = 0
            ORDER BY `create_time`, `role_id`;";

        return await QueryAsync<RoleEntity>(sql, new
        {
            ActivityId = activityId
        });
    }

    public async Task<int> CountActivitiesAsync(IEnumerable<long> activityIds, int custCompanyId)
    {
        var activityIdList = activityIds?.Distinct().ToArray() ?? System.Array.Empty<long>();
        if (activityIdList.Length == 0 || custCompanyId <= 0)
        {
            return 0;
        }

        const string sql = @"
            SELECT COUNT(1)
            FROM `roadshow_activity`
            WHERE `activity_id` IN @ActivityIds
              AND `cust_company_id` = @CustCompanyId
              AND `is_deleted` = 0;";

        return await QueryFirstOrDefaultAsync<int>(sql, new
        {
            ActivityIds = activityIdList,
            CustCompanyId = custCompanyId
        });
    }

    public async Task<bool> ExistsQuestionByActivityAsync(long activityId)
    {
        const string sql = @"
            SELECT 1
            FROM `roadshow_question_bank`
            WHERE `activity_id` = @ActivityId
              AND `is_deleted` = 0
            LIMIT 1;";

        var result = await QueryFirstOrDefaultAsync<int?>(sql, new
        {
            ActivityId = activityId
        });

        return result.HasValue;
    }

    public async Task<bool> ExistsQuestionByRoleAsync(long roleId)
    {
        const string sql = @"
            SELECT 1
            FROM `roadshow_question_bank`
            WHERE `role_id` = @RoleId
              AND `is_deleted` = 0
            LIMIT 1;";

        var result = await QueryFirstOrDefaultAsync<int?>(sql, new
        {
            RoleId = roleId
        });

        return result.HasValue;
    }

    public async Task<bool> ExistsQuestionByStageAsync(long stageId)
    {
        const string sql = @"
            SELECT 1
            FROM `roadshow_question_bank`
            WHERE `stage_id` = @StageId
              AND `is_deleted` = 0
            LIMIT 1;";

        var result = await QueryFirstOrDefaultAsync<int?>(sql, new
        {
            StageId = stageId
        });

        return result.HasValue;
    }

    /// <summary>
    /// 查询机构下业务环节简要信息
    /// </summary>
    /// <summary>
    /// 导入活动及其角色、环节数据
    /// </summary>
    public async Task InsertExtractedActivityAsync(
        ActivityEntity activity,
        IEnumerable<RoleEntity> roles)
    {
        const string activitySql = @"INSERT INTO `roadshow_activity`
                                    (
                                        `activity_id`,
                                        `cust_company_id`,
                                        `activity_name`,
                                        `activity_desc`,
                                        `cover_image`,
                                        `core_goal`,
                                        `customer_background`,
                                        `start_time`,
                                        `end_time`,
                                        `recommended_duration_minutes`,
                                        `publish_status`,
                                        `is_deleted`,
                                        `create_time`,
                                        `update_time`
                                    ) VALUES
                                    (
                                        @ActivityId,
                                        @CustCompanyId,
                                        @ActivityName,
                                        @ActivityDesc,
                                        @CoverImage,
                                        @CoreGoal,
                                        @CustomerBackground,
                                        @StartTime,
                                        @EndTime,
                                        @RecommendedDurationMinutes,
                                        @PublishStatus,
                                        @IsDeleted,
                                        @CreateTime,
                                        @UpdateTime
                                    );";

        const string roleSql = @"INSERT INTO `roadshow_role`
                                (
                                    `role_id`,
                                    `activity_id`,
                                    `customer_id`,
                                    `role_nickname`,
                                    `job_title`,
                                    `project_role`,
                                    `personality`,
                                    `communication_style`,
                                    `project_requirement`,
                                    `is_deleted`,
                                    `create_time`,
                                    `update_time`
                                ) VALUES
                                (
                                    @RoleId,
                                    @ActivityId,
                                    @CustomerId,
                                    @RoleNickname,
                                    @JobTitle,
                                    @ProjectRole,
                                    @Personality,
                                    @CommunicationStyle,
                                    @ProjectRequirement,
                                    @IsDeleted,
                                    @CreateTime,
                                    @UpdateTime
                                );";

        var roleList = roles?.ToList() ?? new List<RoleEntity>();

        using IDbConnection conn = GetCurrentDbConnection();
        using var transaction = conn.BeginTransaction();
        try
        {
            await ExecuteAsync(conn, activitySql, activity, transaction);

            foreach (var role in roleList)
            {
                await ExecuteAsync(conn, roleSql, role, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<int> UpdateActivityAsync(ActivityEntity activity)
    {
        const string sql = @"
            UPDATE `roadshow_activity`
            SET `activity_name` = @ActivityName,
                `activity_desc` = @ActivityDesc,
                `cover_image` = @CoverImage,
                `core_goal` = @CoreGoal,
                `customer_background` = @CustomerBackground,
                `start_time` = @StartTime,
                `end_time` = @EndTime,
                `recommended_duration_minutes` = @RecommendedDurationMinutes,
                `publish_status` = @PublishStatus,
                `update_time` = @UpdateTime
            WHERE `activity_id` = @ActivityId
              AND `is_deleted` = 0;";

        return await ExecuteAsync(sql, activity);
    }

    public async Task<int> DeleteActivityAsync(long activityId, System.DateTime updateTime)
    {
        const string activitySql = @"
            UPDATE `roadshow_activity`
            SET `is_deleted` = 1,
                `update_time` = @UpdateTime
            WHERE `activity_id` = @ActivityId
              AND `is_deleted` = 0;";

        const string roleSql = @"
            UPDATE `roadshow_role`
            SET `is_deleted` = 1,
                `update_time` = @UpdateTime
            WHERE `activity_id` = @ActivityId
              AND `is_deleted` = 0;";

        const string stageSql = @"
            UPDATE `roadshow_business_stage`
            SET `is_deleted` = 1,
                `update_time` = @UpdateTime
            WHERE `activity_id` = @ActivityId
              AND `is_deleted` = 0;";

        const string questionSql = @"
            UPDATE `roadshow_question_bank`
            SET `is_deleted` = 1,
                `update_time` = @UpdateTime
            WHERE `activity_id` = @ActivityId
              AND `is_deleted` = 0;";

        using IDbConnection conn = GetCurrentDbConnection();
        using var transaction = conn.BeginTransaction();
        try
        {
            await ExecuteAsync(conn, questionSql, new
            {
                ActivityId = activityId,
                UpdateTime = updateTime
            }, transaction);

            await ExecuteAsync(conn, roleSql, new
            {
                ActivityId = activityId,
                UpdateTime = updateTime
            }, transaction);

            await ExecuteAsync(conn, stageSql, new
            {
                ActivityId = activityId,
                UpdateTime = updateTime
            }, transaction);

            var count = await ExecuteAsync(conn, activitySql, new
            {
                ActivityId = activityId,
                UpdateTime = updateTime
            }, transaction);

            transaction.Commit();
            return count;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<int> InsertRoleAsync(RoleEntity role)
    {
        const string sql = @"
            INSERT INTO `roadshow_role`
            (
                `role_id`,
                `activity_id`,
                `role_nickname`,
                `job_title`,
                `project_role`,
                `personality`,
                `communication_style`,
                `project_requirement`,
                `is_deleted`,
                `create_time`,
                `update_time`
            ) VALUES
            (
                @RoleId,
                @ActivityId,
                @CustomerId,
                @RoleNickname,
                @JobTitle,
                @ProjectRole,
                @Personality,
                @CommunicationStyle,
                @ProjectRequirement,
                @IsDeleted,
                @CreateTime,
                @UpdateTime
            );";

        return await ExecuteAsync(sql, role);
    }

    public async Task<int> UpdateRoleAsync(RoleEntity role)
    {
        const string sql = @"
            UPDATE `roadshow_role`
            SET `customer_id` = @CustomerId,
                `role_nickname` = @RoleNickname,
                `job_title` = @JobTitle,
                `project_role` = @ProjectRole,
                `personality` = @Personality,
                `communication_style` = @CommunicationStyle,
                `project_requirement` = @ProjectRequirement,
                `update_time` = @UpdateTime
            WHERE `role_id` = @RoleId
              AND `is_deleted` = 0;";

        return await ExecuteAsync(sql, role);
    }

    public async Task<int> DeleteRoleAsync(long roleId, System.DateTime updateTime)
    {
        const string sql = @"
            UPDATE `roadshow_role`
            SET `is_deleted` = 1,
                `update_time` = @UpdateTime
            WHERE `role_id` = @RoleId
              AND `is_deleted` = 0;";

        return await ExecuteAsync(sql, new
        {
            RoleId = roleId,
            UpdateTime = updateTime
        });
    }

    public async Task<int> InsertBusinessStageAsync(BusinessStageEntity stage)
    {
        const string sql = @"
            INSERT INTO `roadshow_business_stage`
            (
                `stage_id`,
                `activity_id`,
                `stage_name`,
                `stage_desc`,
                `stage_task`,
                `sort_no`,
                `question_count`,
                `is_deleted`,
                `create_time`,
                `update_time`
            ) VALUES
            (
                @StageId,
                @ActivityId,
                @StageName,
                @StageDesc,
                @StageTask,
                @SortNo,
                @QuestionCount,
                @IsDeleted,
                @CreateTime,
                @UpdateTime
            );";

        return await ExecuteAsync(sql, stage);
    }

    public async Task<int> UpdateBusinessStageAsync(BusinessStageEntity stage)
    {
        const string sql = @"
            UPDATE `roadshow_business_stage`
            SET `stage_name` = @StageName,
                `stage_desc` = @StageDesc,
                `stage_task` = @StageTask,
                `sort_no` = @SortNo,
                `question_count` = @QuestionCount,
                `update_time` = @UpdateTime
            WHERE `stage_id` = @StageId
              AND `is_deleted` = 0;";

        return await ExecuteAsync(sql, stage);
    }

    public async Task<int> DeleteBusinessStageAsync(long stageId, System.DateTime updateTime)
    {
        const string sql = @"
            UPDATE `roadshow_business_stage`
            SET `is_deleted` = 1,
                `update_time` = @UpdateTime
            WHERE `stage_id` = @StageId
              AND `is_deleted` = 0;";

        return await ExecuteAsync(sql, new
        {
            StageId = stageId,
            UpdateTime = updateTime
        });
    }

    public async Task<int> InsertQuestionAsync(QuestionBankEntity question)
    {
        const string questionSql = @"
            INSERT INTO `roadshow_question_bank`
            (
                `question_id`,
                `activity_id`,
                `role_id`,
                `question_stem`,
                `assessment_points`,
                `is_required`,
                `stage_id`,
                `is_deleted`,
                `create_time`,
                `update_time`
            ) VALUES
            (
                @QuestionId,
                @ActivityId,
                @RoleId,
                @QuestionStem,
                @AssessmentPoints,
                @IsRequired,
                @StageId,
                @IsDeleted,
                @CreateTime,
                @UpdateTime
            );";

        return await ExecuteAsync(questionSql, question);
    }

    public async Task<int> UpdateQuestionAsync(QuestionBankEntity question)
    {
        const string questionSql = @"
            UPDATE `roadshow_question_bank`
            SET `role_id` = @RoleId,
                `question_stem` = @QuestionStem,
                `assessment_points` = @AssessmentPoints,
                `is_required` = @IsRequired,
                `stage_id` = @StageId,
                `update_time` = @UpdateTime
            WHERE `question_id` = @QuestionId
              AND `is_deleted` = 0;";

        return await ExecuteAsync(questionSql, question);
    }

    public async Task<int> DeleteQuestionAsync(long questionId, long activityId, System.DateTime updateTime)
    {
        const string questionSql = @"
            UPDATE `roadshow_question_bank`
            SET `is_deleted` = 1,
                `update_time` = @UpdateTime
            WHERE `question_id` = @QuestionId
              AND `is_deleted` = 0;";

        return await ExecuteAsync(questionSql, new
        {
            QuestionId = questionId,
            UpdateTime = updateTime
        });
    }

    public async Task<int> DeleteQuestionsAsync(IEnumerable<long> questionIds, IEnumerable<long> activityIds, System.DateTime updateTime)
    {
        var idList = questionIds?.Distinct().ToArray() ?? System.Array.Empty<long>();
        if (idList.Length == 0 || !(activityIds?.Any() ?? false))
        {
            return 0;
        }

        const string questionSql = @"
            UPDATE `roadshow_question_bank`
            SET `is_deleted` = 1,
                `update_time` = @UpdateTime
            WHERE `question_id` IN @QuestionIds
              AND `is_deleted` = 0;";

        using IDbConnection conn = GetCurrentDbConnection();
        return await conn.ExecuteAsync(new CommandDefinition(questionSql, new
        {
            QuestionIds = idList,
            UpdateTime = updateTime
        }));
    }

    /// <summary>
    /// 导入题库及其能力维度关系数据
    /// </summary>
    public async Task InsertExtractedQuestionsAsync(
        IEnumerable<BusinessStageEntity> stagesToInsert,
        IEnumerable<QuestionBankEntity> questions,
        IEnumerable<QuestionAbilityRelEntity> questionAbilityRelations)
    {
        const string stageSql = @"INSERT INTO `roadshow_business_stage`
                                 (
                                     `stage_id`,
                                     `activity_id`,
                                     `stage_name`,
                                     `stage_desc`,
                                     `stage_task`,
                                     `sort_no`,
                                     `question_count`,
                                     `is_deleted`,
                                     `create_time`,
                                     `update_time`
                                 ) VALUES
                                 (
                                     @StageId,
                                     @ActivityId,
                                     @StageName,
                                     @StageDesc,
                                     @StageTask,
                                     @SortNo,
                                     @QuestionCount,
                                     @IsDeleted,
                                     @CreateTime,
                                     @UpdateTime
                                 );";

        const string questionSql = @"INSERT INTO `roadshow_question_bank`
                                    (
                                        `question_id`,
                                        `activity_id`,
                                        `role_id`,
                                        `question_stem`,
                                        `assessment_points`,
                                        `is_required`,
                                        `stage_id`,
                                        `is_deleted`,
                                        `create_time`,
                                        `update_time`
                                    ) VALUES
                                    (
                                        @QuestionId,
                                        @ActivityId,
                                        @RoleId,
                                        @QuestionStem,
                                        @AssessmentPoints,
                                        @IsRequired,
                                        @StageId,
                                        @IsDeleted,
                                        @CreateTime,
                                        @UpdateTime
                                    );";

        const string questionAbilityRelSql = @"INSERT INTO `roadshow_question_ability_rel`
                                              (
                                                  `question_id`,
                                                  `ability_dimension_id`,
                                                  `difficulty_level`,
                                                  `create_time`
                                              ) VALUES
                                              (
                                                  @QuestionId,
                                                  @AbilityDimensionId,
                                                  @DifficultyLevel,
                                                  @CreateTime
                                              );";

        var stageInsertList = stagesToInsert?.ToList() ?? new List<BusinessStageEntity>();
        var questionList = questions?.ToList() ?? new List<QuestionBankEntity>();
        var questionAbilityRelList = questionAbilityRelations?.ToList() ?? new List<QuestionAbilityRelEntity>();

        using IDbConnection conn = GetCurrentDbConnection();
        using var transaction = conn.BeginTransaction();
        try
        {
            foreach (var stage in stageInsertList)
            {
                await ExecuteAsync(conn, stageSql, stage, transaction);
            }

            foreach (var question in questionList)
            {
                await ExecuteAsync(conn, questionSql, question, transaction);
            }

            foreach (var relation in questionAbilityRelList)
            {
                await ExecuteAsync(conn, questionAbilityRelSql, relation, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static string BuildWhereSql(ActivityListRequestDto request, int? custCompanyId, out DynamicParameters parameters)
    {
        parameters = new DynamicParameters();
        var sql = new StringBuilder("`is_deleted` = 0");

        if (custCompanyId.HasValue && custCompanyId.Value > 0)
        {
            sql.Append(" AND `cust_company_id` = @CustCompanyId");
            parameters.Add("CustCompanyId", custCompanyId.Value);
        }

        if (request?.StartDate != null)
        {
            sql.Append(" AND `start_time` >= @StartDate");
            parameters.Add("StartDate", request.StartDate.Value.Date);
        }

        if (request?.EndDate != null)
        {
            sql.Append(" AND `end_time` < @EndDateExclusive");
            parameters.Add("EndDateExclusive", request.EndDate.Value.Date.AddDays(1));
        }

        if (request?.PublishStatus != null)
        {
            sql.Append(" AND `publish_status` = @PublishStatus");
            parameters.Add("PublishStatus", request.PublishStatus.Value);
        }

        return sql.ToString();
    }

    public async Task<IEnumerable<QuestionAbilityRelEntity>> SelectQuestionAbilityRelsAsync(long questionId)
    {
        const string sql = @"
            SELECT
                `question_id`,
                `ability_dimension_id`,
                `difficulty_level`,
                `create_time`
            FROM `roadshow_question_ability_rel`
            WHERE `question_id` = @QuestionId;";

        return await QueryAsync<QuestionAbilityRelEntity>(sql, new
        {
            QuestionId = questionId
        });
    }

    public sealed class ActivityDetailQueryResult
    {
        public ActivityEntity Activity { get; set; }

        public List<RoleEntity> Roles { get; set; } = new();

        public List<BusinessStageEntity> BusinessStages { get; set; } = new();
    }
}
