using System;
using System.Collections.Generic;

namespace Beta.AIRoadshow.Entity.Dto.Response;

/// <summary>
/// 客户返回数据
/// </summary>
public class CustomerResponseDto
{
    public string Id { get; set; } = string.Empty;

    public int CustId { get; set; }

    public int Age { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CustType { get; set; } = string.Empty;

    public string Occupation { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public bool HasChildren { get; set; }

    public int Childrens { get; set; }

    public bool Married { get; set; }

    public string InvestmentExperience { get; set; } = string.Empty;

    public string AssetLevel { get; set; } = string.Empty;

    public string PhotoUrl { get; set; } = string.Empty;

    public string SpeakerCode { get; set; } = string.Empty;

    public string VideoUrl { get; set; } = string.Empty;

    public string Nationality { get; set; } = string.Empty;

    public string NativePlace { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new();

    public string Brief { get; set; } = string.Empty;

    public string InvestStyle { get; set; } = string.Empty;

    public string AssetInfo { get; set; } = string.Empty;

    public string ProductInfo { get; set; } = string.Empty;

    public bool IsTop { get; set; }

    public int Badge { get; set; }

    public CustomerEmojiResponseDto Emoji { get; set; } = new();

    public Dictionary<string, string> ExtendProperties { get; set; } = new();

    public Dictionary<string, string> ExtendPropertiesV2 { get; set; } = new();

    public string ChildInfo { get; set; } = string.Empty;

    public int Status { get; set; }

    public string CreateBy { get; set; } = string.Empty;

    public DateTime CreateTime { get; set; }

    public string UpdateBy { get; set; } = string.Empty;

    public DateTime UpdateTime { get; set; }

    public int Cls { get; set; }

    public string CustAttr { get; set; } = string.Empty;
}

public class CustomerEmojiResponseDto
{
    public string EmojiUrl { get; set; } = string.Empty;

    public List<int[]> EmojiFrames { get; set; } = new();
}
