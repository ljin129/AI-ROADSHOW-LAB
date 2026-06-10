using Beta.AIRoadshow.Entity.DBEntity;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.DataAccess;

[Service(ServiceLifetime.Transient)]
public class PracticeRepository : BaseRepository<AIRoadshowDbContext>
{
    public PracticeRepository(AIRoadshowDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<int> InsertPracticeRecordAsync(PracticeRecordEntity record)
    {
        const string sql = @"
            INSERT INTO `roadshow_practice_record`
            (
                `practice_record_id`,
                `user_id`,
                `activity_id`,
                `roadshow_material_id`,
                `score`,
                `score_comment`,
                `strengths`,
                `weaknesses`,
                `suggestions`,
                `practice_status`,
                `create_time`
            ) VALUES
            (
                @PracticeRecordId,
                @UserId,
                @ActivityId,
                @RoadshowMaterialId,
                @Score,
                @ScoreComment,
                @Strengths,
                @Weaknesses,
                @Suggestions,
                @PracticeStatus,
                @CreateTime
            );";

        return await ExecuteAsync(sql, record);
    }

    public async Task<PracticeRecordEntity> SelectPracticeRecordByIdAsync(long practiceRecordId)
    {
        const string sql = @"
            SELECT
                `practice_record_id`,
                `user_id`,
                `activity_id`,
                `roadshow_material_id`,
                `score`,
                `score_comment`,
                `strengths`,
                `weaknesses`,
                `suggestions`,
                `practice_status`,
                `create_time`
            FROM `roadshow_practice_record`
            WHERE `practice_record_id` = @PracticeRecordId
            LIMIT 1;";

        return await QueryFirstOrDefaultAsync<PracticeRecordEntity>(sql, new
        {
            PracticeRecordId = practiceRecordId
        });
    }

    public async Task<PracticeRecordEntity> SelectInProgressRecordAsync(long userId, long activityId)
    {
        const string sql = @"
            SELECT
                `practice_record_id`,
                `user_id`,
                `activity_id`,
                `roadshow_material_id`,
                `score`,
                `score_comment`,
                `strengths`,
                `weaknesses`,
                `suggestions`,
                `practice_status`,
                `create_time`
            FROM `roadshow_practice_record`
            WHERE `user_id` = @UserId
              AND `activity_id` = @ActivityId
              AND `practice_status` = 'in_progress'
            ORDER BY `create_time` DESC
            LIMIT 1;";

        return await QueryFirstOrDefaultAsync<PracticeRecordEntity>(sql, new
        {
            UserId = userId,
            ActivityId = activityId
        });
    }

    public async Task<int> UpdatePracticeRecordStatusAsync(long practiceRecordId, string practiceStatus, DateTime updateTime)
    {
        const string sql = @"
            UPDATE `roadshow_practice_record`
            SET `practice_status` = @PracticeStatus
            WHERE `practice_record_id` = @PracticeRecordId;";

        return await ExecuteAsync(sql, new
        {
            PracticeRecordId = practiceRecordId,
            PracticeStatus = practiceStatus
        });
    }

    public async Task<int> InsertPracticeRecordDetailAsync(PracticeRecordDetailEntity detail)
    {
        const string sql = @"
            INSERT INTO `roadshow_practice_record_detail`
            (
                `detail_id`,
                `practice_record_id`,
                `user_id`,
                `activity_id`,
                `dialog_content`,
                `dialog_voice_file_id`,
                `content_type`,
                `question_id`,
                `score`,
                `score_comment`,
                `strengths`,
                `weaknesses`,
                `suggestions`,
                `create_time`
            ) VALUES
            (
                @DetailId,
                @PracticeRecordId,
                @UserId,
                @ActivityId,
                @DialogContent,
                @DialogVoiceFileId,
                @ContentType,
                @QuestionId,
                @Score,
                @ScoreComment,
                @Strengths,
                @Weaknesses,
                @Suggestions,
                @CreateTime
            );";

        return await ExecuteAsync(sql, detail);
    }

    public async Task<PracticeRecordDetailEntity> SelectPracticeRecordDetailByIdAsync(long detailId)
    {
        const string sql = @"
            SELECT
                `detail_id`,
                `practice_record_id`,
                `user_id`,
                `activity_id`,
                `dialog_content`,
                `dialog_voice_file_id`,
                `content_type`,
                `question_id`,
                `score`,
                `score_comment`,
                `strengths`,
                `weaknesses`,
                `suggestions`,
                `create_time`
            FROM `roadshow_practice_record_detail`
            WHERE `detail_id` = @DetailId
            LIMIT 1;";

        return await QueryFirstOrDefaultAsync<PracticeRecordDetailEntity>(sql, new
        {
            DetailId = detailId
        });
    }

    public async Task<IEnumerable<PracticeRecordDetailEntity>> SelectPracticeRecordDetailsAsync(long practiceRecordId)
    {
        const string sql = @"
            SELECT
                `detail_id`,
                `practice_record_id`,
                `user_id`,
                `activity_id`,
                `dialog_content`,
                `dialog_voice_file_id`,
                `content_type`,
                `question_id`,
                `score`,
                `score_comment`,
                `strengths`,
                `weaknesses`,
                `suggestions`,
                `create_time`
            FROM `roadshow_practice_record_detail`
            WHERE `practice_record_id` = @PracticeRecordId
            ORDER BY `create_time`, `detail_id`;";

        return await QueryAsync<PracticeRecordDetailEntity>(sql, new
        {
            PracticeRecordId = practiceRecordId
        });
    }

    public async Task<IEnumerable<long>> SelectPracticeQuestionIdsAsync(long practiceRecordId)
    {
        const string sql = @"
            SELECT DISTINCT `question_id`
            FROM `roadshow_practice_record_detail`
            WHERE `practice_record_id` = @PracticeRecordId
              AND `content_type` = 'ai'
              AND `question_id` IS NOT NULL;";

        return await QueryAsync<long>(sql, new
        {
            PracticeRecordId = practiceRecordId
        });
    }

    public async Task<int> CountPracticeQuestionsByStageAsync(long practiceRecordId, long stageId)
    {
        const string sql = @"
            SELECT COUNT(DISTINCT d.`question_id`)
            FROM `roadshow_practice_record_detail` d
            INNER JOIN `roadshow_question_bank` q
                    ON q.`question_id` = d.`question_id`
                   AND q.`is_deleted` = 0
            WHERE d.`practice_record_id` = @PracticeRecordId
              AND d.`content_type` = 'ai'
              AND d.`question_id` IS NOT NULL
              AND q.`stage_id` = @StageId;";

        using IDbConnection conn = GetCurrentDbConnection();
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            PracticeRecordId = practiceRecordId,
            StageId = stageId
        }));
    }

    public async Task<IEnumerable<QuestionBankEntity>> SelectAvailableQuestionsByStageAsync(long activityId, long stageId, IEnumerable<long> excludedQuestionIds)
    {
        var excludedIdList = excludedQuestionIds?.Distinct().ToArray() ?? Array.Empty<long>();

        var sql = @"
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
            WHERE `activity_id` = @ActivityId
              AND `stage_id` = @StageId
              AND `is_deleted` = 0";

        if (excludedIdList.Length > 0)
        {
            sql += " AND `question_id` NOT IN @ExcludedQuestionIds";
        }

        sql += " ORDER BY `is_required` DESC, `create_time`, `question_id`;";

        using IDbConnection conn = GetCurrentDbConnection();
        return await conn.QueryAsync<QuestionBankEntity>(new CommandDefinition(sql, new
        {
            ActivityId = activityId,
            StageId = stageId,
            ExcludedQuestionIds = excludedIdList
        }));
    }

    public async Task<int> CountFollowUpAsksAsync(long practiceRecordId, long questionId)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM `roadshow_practice_record_detail`
            WHERE `practice_record_id` = @PracticeRecordId
              AND `question_id` = @QuestionId
              AND `content_type` = 'ai_followup';";

        using IDbConnection conn = GetCurrentDbConnection();
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            PracticeRecordId = practiceRecordId,
            QuestionId = questionId
        }));
    }

    public async Task<IEnumerable<PracticeRecordDetailEntity>> SelectDialogDetailsByQuestionAsync(long practiceRecordId, long questionId)
    {
        const string sql = @"
            SELECT
                `detail_id`,
                `practice_record_id`,
                `user_id`,
                `activity_id`,
                `dialog_content`,
                `dialog_voice_file_id`,
                `content_type`,
                `question_id`,
                `score`,
                `score_comment`,
                `strengths`,
                `weaknesses`,
                `suggestions`,
                `create_time`
            FROM `roadshow_practice_record_detail`
            WHERE `practice_record_id` = @PracticeRecordId
              AND `question_id` = @QuestionId
            ORDER BY `create_time`, `detail_id`;";

        return await QueryAsync<PracticeRecordDetailEntity>(sql, new
        {
            PracticeRecordId = practiceRecordId,
            QuestionId = questionId
        });
    }

    public async Task<int> UpdatePracticeRecordDetailScoreAsync(
        long detailId,
        decimal? score,
        string scoreComment,
        string strengths,
        string weaknesses,
        string suggestions)
    {
        const string sql = @"
            UPDATE `roadshow_practice_record_detail`
            SET `score` = @Score,
                `score_comment` = @ScoreComment,
                `strengths` = @Strengths,
                `weaknesses` = @Weaknesses,
                `suggestions` = @Suggestions
            WHERE `detail_id` = @DetailId;";

        return await ExecuteAsync(sql, new
        {
            DetailId = detailId,
            Score = score,
            ScoreComment = scoreComment,
            Strengths = strengths,
            Weaknesses = weaknesses,
            Suggestions = suggestions
        });
    }

    public async Task<PracticeRecordDetailEntity> SelectNextAiReplyAfterDetailAsync(long practiceRecordId, long afterDetailId)
    {
        const string sql = @"
            SELECT
                `detail_id`,
                `practice_record_id`,
                `user_id`,
                `activity_id`,
                `dialog_content`,
                `dialog_voice_file_id`,
                `content_type`,
                `question_id`,
                `score`,
                `score_comment`,
                `strengths`,
                `weaknesses`,
                `suggestions`,
                `create_time`
            FROM `roadshow_practice_record_detail`
            WHERE `practice_record_id` = @PracticeRecordId
              AND `detail_id` > @AfterDetailId
              AND `content_type` IN ('ai', 'ai_followup')
            ORDER BY `create_time`, `detail_id`
            LIMIT 1;";

        return await QueryFirstOrDefaultAsync<PracticeRecordDetailEntity>(sql, new
        {
            PracticeRecordId = practiceRecordId,
            AfterDetailId = afterDetailId
        });
    }

    public async Task<int> InsertPracticeRecordDetailExtBatchAsync(IEnumerable<PracticeRecordDetailExtEntity> items)
    {
        var itemList = items?.ToList() ?? new List<PracticeRecordDetailExtEntity>();
        if (itemList.Count == 0)
        {
            return 0;
        }

        const string sql = @"
            INSERT INTO `roadshow_practice_record_detail_ext`
            (
                `detail_id`,
                `ability_dimension_id`,
                `score`,
                `score_comment`,
                `create_time`
            ) VALUES
            (
                @DetailId,
                @AbilityDimensionId,
                @Score,
                @ScoreComment,
                NOW()
            );";

        using IDbConnection conn = GetCurrentDbConnection();
        return await conn.ExecuteAsync(new CommandDefinition(sql, itemList));
    }
}
