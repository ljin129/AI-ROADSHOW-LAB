using Beta.Framework.Entity;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Beta.AIRoadshow.H5Api.Controllers;

/// <summary>
/// 
/// </summary>
public class TestController : BaseController
{
    /// <summary>
    /// 
    /// </summary>
    [HttpGet]
    public ResponseInfo<string> GetNowTime()
    => new ResponseInfo<string>
    {
        State = ResultState.Successed,
        Data = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
    };
}
