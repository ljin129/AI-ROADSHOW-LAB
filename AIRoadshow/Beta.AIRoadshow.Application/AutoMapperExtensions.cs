using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Beta.AIRoadshow.Application;

/// <summary>
/// 
/// </summary>
public static class AutoMapperExtensions
{
    #region fileds & inject

    private static IServiceProvider _serviceProvider;

    /// <summary>
    /// 
    /// </summary>
    public static IApplicationBuilder UseAutoMapper(this IApplicationBuilder applicationBuilder)
    {
        _serviceProvider = applicationBuilder.ApplicationServices;
        return applicationBuilder;
    }

    #endregion

    /// <summary>
    /// 将数据转换成指定类型
    /// </summary>
    public static TDestination MapTo<TDestination>(this object source)
    {
        var mapper = _serviceProvider.GetRequiredService<IMapper>();
        return mapper != null ? mapper.Map<TDestination>(source) : default;
    }

    /// <summary>
    /// 将指定集合转换成目标类型集合
    /// 该方法只可以返回List[T]其他集合类型可以直接使用 MapTo[T]
    /// </summary>
    public static IEnumerable<TDestination> MapToList<TDestination>(this IEnumerable source)
    {
        var mapper = _serviceProvider.GetRequiredService<IMapper>();
        return mapper.Map<IEnumerable<TDestination>>(source) ?? new List<TDestination>();
    }

    /// <summary>
    /// 将指定集合转换成目标类型集合
    /// </summary>
    /// <typeparam name="TTarget">目标类型</typeparam>
    /// <param name="source"></param>
    /// <param name="predicate"></param>
    /// <returns>返回目标集合</returns>
    public static IEnumerable<TTarget> MapToList<TTarget>(this IEnumerable source, Action<TTarget> predicate)
    {
        var listData = new List<TTarget>();
        var mapper = _serviceProvider?.GetRequiredService<IMapper>();
        if (source != null)
        {
            foreach (var obj in source)
            {
                var t = mapper != null ? mapper.Map<TTarget>(obj) : default;
                if (t != null)
                {
                    listData.Add(t);
                    predicate?.Invoke(t);
                }
                else throw new BetaException("AutoMapper.MapToList<TTarget> return List<TTarget>:a occurred exception in conversion");
            }
        }

        return listData;
    }

    /// <summary>
    /// 将数据转换成指定类型
    /// </summary>
    public static TTarget MapTo<TTarget>(this object source, Action<TTarget> predicate)
    {
        var mapper = _serviceProvider?.GetRequiredService<IMapper>();
        var item = mapper != null ? mapper.Map<TTarget>(source) : default;
        if (predicate != null && item != null)
        {
            predicate(item);
        }

        return item;
    }

    /// <summary>
    /// 将指定集合转换成目标类型集合
    /// </summary>
    public static IEnumerable<TTarget> MapToList<TTarget>(this IEnumerable source, Func<int, TTarget, bool> predicate)
    {
        var listData = new List<TTarget>();
        var mapper = _serviceProvider?.GetRequiredService<IMapper>();
        if (source != null)
        {
            int i = 0;
            foreach (var item in source)
            {
                var t = mapper != null ? mapper.Map<TTarget>(item) : default;
                if (t != null)
                {
                    listData.Add(t);
                    if (predicate != null)
                    {
                        try
                        {
                            if (!predicate(i, t))
                                break;
                        }
                        catch (Exception ex)
                        {
                            throw new BetaException("AutoMapper.MapToList<TTarget> return List<TTarget>:a occurred exception in conversion", ex);
                        }
                    }

                    i++;
                }
                else throw new BetaException("AutoMapper.MapToList<TTarget> return List<TTarget>:a occurred exception in conversion");
            }
        }

        return listData;
    }

    /// <summary>
    /// 将指定集合转换成目标类型集合
    /// </summary>
    public static IEnumerable<TTarget> MapToList<TSource, TTarget>(this IEnumerable<TSource> source, Func<int, TSource, TTarget, bool> predicate)
    {
        var listData = new List<TTarget>();
        var mapper = _serviceProvider?.GetRequiredService<IMapper>();
        if (source != null)
        {
            for (int i = 0; i < source.Count(); i++)
            {
                var item = source.ElementAt(i);
                var t = mapper != null ? mapper.Map<TTarget>(item) : default;
                if (t != null)
                {
                    listData.Add(t);
                    if (predicate != null)
                    {
                        try
                        {
                            if (!predicate(i, item, t))
                                break;
                        }
                        catch (Exception)
                        {
                            break;
                        }
                    }
                }
                else throw new BetaException("AutoMapper.MapToList<TSource, TTarget> return List<TTarget>:a occurred exception in conversion");
            }
        }

        return listData;
    }
}
