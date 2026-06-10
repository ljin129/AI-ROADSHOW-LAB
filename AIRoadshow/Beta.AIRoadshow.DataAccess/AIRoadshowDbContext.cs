using System;
using System.Data;

namespace Beta.AIRoadshow.DataAccess;

/// <summary>
/// 
/// </summary>
public class AIRoadshowDbContext
{
    private Func<IDbConnection> DbContentBuild { get; set; }

    public IDbConnection GetConnection => DbContentBuild();

    /// <summary>
    /// 构造函数
    /// </summary>
    public AIRoadshowDbContext(Func<IDbConnection> dbContentBuild)
    {
        DbContentBuild = dbContentBuild;
    }
}
