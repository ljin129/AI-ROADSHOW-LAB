using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.DataAccess;

/// <summary>
/// 【仓存】实体
/// </summary>
public class BaseRepository<TDbContext>
    where TDbContext : AIRoadshowDbContext
{
    private readonly TDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public BaseRepository(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 获取当前链接对象
    /// </summary>
    protected IDbConnection GetCurrentDbConnection()
    {
        var dbConnection = _dbContext.GetConnection;
        if (dbConnection.State == ConnectionState.Open)
        {
            return dbConnection;
        }
        dbConnection.Open();
        return dbConnection;
    }

    /// <summary>
    /// 查询列表
    /// </summary>
    protected async Task<IEnumerable<TResult>> QueryAsync<TResult>(string sql, object param = null, CancellationToken cancellationToken = default)
    {
        using IDbConnection conn = GetCurrentDbConnection();
        var commandDefinition = new CommandDefinition(sql, param, cancellationToken: cancellationToken);
        return await conn.QueryAsync<TResult>(commandDefinition);
    }

    /// <summary>
    /// 查询单一对象
    /// </summary>
    protected async Task<TResult> QueryFirstOrDefaultAsync<TResult>(string sql, object param = null, CancellationToken cancellationToken = default)
    {
        using IDbConnection conn = GetCurrentDbConnection();
        var commandDefinition = new CommandDefinition(sql, param, cancellationToken: cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<TResult>(commandDefinition);
    }

    public async Task<int> ExecuteAsync(string sql, object param = null, CancellationToken cancellationToken = default)
    {
        using IDbConnection conn = GetCurrentDbConnection();
        var commandDefinition = new CommandDefinition(sql, param, cancellationToken: cancellationToken);
        return await conn.ExecuteAsync(commandDefinition);
    }

    public async Task<int> ExecuteAsync(IDbConnection dbConnection, string sql, object param = null, IDbTransaction dbTransaction = null, CancellationToken cancellationToken = default)
    {
        var commandDefinition = new CommandDefinition(sql, param, cancellationToken: cancellationToken, transaction: dbTransaction);
        return await dbConnection.ExecuteAsync(commandDefinition);
    }
}