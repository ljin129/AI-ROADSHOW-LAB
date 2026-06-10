using Beta.AIRoadshow.Application;
using Beta.AIRoadshow.Entity.Dto.Response;
using Beta.Framework.Entity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.PCApi.Controllers;

/// <summary>
/// 【控制器】客户
/// </summary>
public class CustomerController : BaseController
{
    private readonly CustomerService _customerService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CustomerController(CustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// 查询全量客户数据
    /// </summary>
    [HttpGet]
    public Task<ResponseInfo<IEnumerable<CustomerResponseDto>>> GetCustomerListAsync()
        => _customerService.GetCustomerListAsync();
}
