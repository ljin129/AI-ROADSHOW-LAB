using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Beta.AIRoadshow.Application;

[BsonIgnoreExtraElements]
public class CustomerMongoDocument
{
    public const string DatabaseName = "beta_aiprompt_db";
    public const string CollectionName = "aicoachCust";

    public const string Field_Id = "_id";
    public const string Field_CustId = "cid";
    public const string Field_Age = "a";
    public const string Field_CustName = "n";
    public const string Field_Type = "t";
    public const string Field_Occupation = "o";
    public const string Field_Gender = "g";
    public const string Field_HasChildren = "hc";
    public const string Field_Childrens = "hcs";
    public const string Field_Married = "m";
    public const string Field_InvestmentExperience = "ie";
    public const string Field_AssetLevel = "al";
    public const string Field_PhotoUrl = "pu";
    public const string Field_SpeakerCode = "sp";
    public const string Field_VideoUrl = "vu";
    public const string Field_Nationality = "nl";
    public const string Field_NativePlace = "np";
    public const string Field_Tags = "tag";
    public const string Field_Brief = "brief";
    public const string Field_InvestStyle = "invs";
    public const string Field_AssetInfo = "assi";
    public const string Field_ProductInfo = "proi";
    public const string Field_Emoji = "emoji";
    public const string Field_IsTop = "it";
    public const string Field_Badge = "b";
    public const string Field_Ext = "e";
    public const string Field_ExtV2 = "ev2";
    public const string Field_Status = "s";
    public const string Field_CreateBy = "cb";
    public const string Field_CreateTime = "ct";
    public const string Field_UpdateBy = "ub";
    public const string Field_UpdateTime = "ut";
    public const string Field_ChildInfo = "ci";
    public const string Field_Cls = "cls";
    public const string Field_CA = "ca";

    [BsonId]
    [BsonElement(Field_Id)]
    public string Id { get; set; } = string.Empty;

    [BsonElement(Field_CustId)]
    public int CustId { get; set; }

    [BsonElement(Field_Age)]
    public int Age { get; set; }

    [BsonElement(Field_CustName)]
    public string Name { get; set; } = string.Empty;

    [BsonElement(Field_Type)]
    public string CustType { get; set; } = string.Empty;

    [BsonElement(Field_Occupation)]
    public string Occupation { get; set; } = string.Empty;

    [BsonElement(Field_Gender)]
    public string Gender { get; set; } = string.Empty;

    [BsonElement(Field_HasChildren)]
    public bool HasChildren { get; set; }

    [BsonElement(Field_Childrens)]
    public int Childrens { get; set; }

    [BsonElement(Field_Married)]
    public bool Married { get; set; }

    [BsonElement(Field_InvestmentExperience)]
    public string InvestmentExperience { get; set; } = string.Empty;

    [BsonElement(Field_AssetLevel)]
    public string AssetLevel { get; set; } = string.Empty;

    [BsonElement(Field_PhotoUrl)]
    public string PhotoUrl { get; set; } = string.Empty;

    [BsonElement(Field_SpeakerCode)]
    public string SpeakerCode { get; set; } = string.Empty;

    [BsonElement(Field_VideoUrl)]
    public string VideoUrl { get; set; } = string.Empty;

    [BsonElement(Field_Nationality)]
    public string Nationality { get; set; } = string.Empty;

    [BsonElement(Field_NativePlace)]
    public string NativePlace { get; set; } = string.Empty;

    [BsonElement(Field_Tags)]
    public List<string> Tags { get; set; } = new();

    [BsonElement(Field_Brief)]
    public string Brief { get; set; } = string.Empty;

    [BsonElement(Field_InvestStyle)]
    public string InvestStyle { get; set; } = string.Empty;

    [BsonElement(Field_AssetInfo)]
    public string AssetInfo { get; set; } = string.Empty;

    [BsonElement(Field_ProductInfo)]
    public string ProductInfo { get; set; } = string.Empty;

    [BsonElement(Field_IsTop)]
    public bool IsTop { get; set; }

    [BsonElement(Field_Badge)]
    public int Badge { get; set; }

    [BsonElement(Field_Emoji)]
    public CustomerEmojiMongoDocument Emoji { get; set; } = new();

    [BsonElement(Field_Ext)]
    public Dictionary<string, string> ExtendProperties { get; set; } = new();

    [BsonElement(Field_ExtV2)]
    public Dictionary<string, string> ExtendPropertiesV2 { get; set; } = new();

    [BsonElement(Field_ChildInfo)]
    public string ChildInfo { get; set; } = string.Empty;

    [BsonElement(Field_Status)]
    public int Status { get; set; }

    [BsonElement(Field_CreateBy)]
    public string CreateBy { get; set; } = string.Empty;

    [BsonElement(Field_CreateTime)]
    public DateTime CreateTime { get; set; }

    [BsonElement(Field_UpdateBy)]
    public string UpdateBy { get; set; } = string.Empty;

    [BsonElement(Field_UpdateTime)]
    public DateTime UpdateTime { get; set; }

    [BsonElement(Field_Cls)]
    public int Cls { get; set; }

    [BsonElement(Field_CA)]
    public string CustAttr { get; set; } = string.Empty;
}

public class CustomerEmojiMongoDocument
{
    public const string Field_EmojiUrl = "emo";
    public const string Field_EmojiFrames = "fram";

    [BsonElement(Field_EmojiUrl)]
    public string EmojiUrl { get; set; } = string.Empty;

    [BsonElement(Field_EmojiFrames)]
    public List<int[]> EmojiFrames { get; set; } = new();
}
