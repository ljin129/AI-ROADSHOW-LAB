using AutoMapper;
using Beta.AIRoadshow.Entity.Dto.Response;
using Beta.Framework.Entity;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.Application;

/// <summary>
/// 【服务】客户
/// </summary>
[Service(ServiceLifetime.Transient)]
public class CustomerService : BaseService
{
    private readonly IMapper _mapper;
    private readonly IMongoCollection<CustomerMongoDocument> _customerCollection;

    public CustomerService(IMapper mapper, IMongoClient mongoClient)
    {
        _mapper = mapper;
        _customerCollection = mongoClient
            .GetDatabase(CustomerMongoDocument.DatabaseName)
            .GetCollection<CustomerMongoDocument>(CustomerMongoDocument.CollectionName);
    }

    /// <summary>
    /// 查询全量客户数据
    /// </summary>
    public async Task<ResponseInfo<IEnumerable<CustomerResponseDto>>> GetCustomerListAsync()
    {
        var sort = Builders<CustomerMongoDocument>.Sort
            .Ascending(x => x.Cls)
            .Descending(x => x.IsTop)
            .Descending(x => x.CreateTime)
            .Ascending(x => x.CustId);

        var entities = await _customerCollection
            .Find(Builders<CustomerMongoDocument>.Filter.Empty)
            .Sort(sort)
            .ToListAsync();

        return new ResponseInfo<IEnumerable<CustomerResponseDto>>
        {
            State = ResultState.Successed,
            Data = _mapper.Map<List<CustomerResponseDto>>(entities)
        };
    }

    public async Task<CustomerResponseDto> GetCustomerByCustIdAsync(int custId)
    {
        if (custId <= 0)
        {
            return null;
        }

        var entity = await _customerCollection
            .Find(x => x.CustId == custId)
            .FirstOrDefaultAsync();

        return entity == null ? null : _mapper.Map<CustomerResponseDto>(entity);
    }

    public async Task<Dictionary<int, CustomerResponseDto>> GetCustomerMapByCustIdsAsync(IEnumerable<int> custIds)
    {
        var custIdList = custIds?
            .Where(x => x > 0)
            .Distinct()
            .ToArray() ?? new int[0];

        if (custIdList.Length == 0)
        {
            return new Dictionary<int, CustomerResponseDto>();
        }

        var entities = await _customerCollection
            .Find(x => custIdList.Contains(x.CustId))
            .ToListAsync();

        return _mapper.Map<List<CustomerResponseDto>>(entities)
            .GroupBy(x => x.CustId)
            .ToDictionary(x => x.Key, x => x.First());
    }
}
