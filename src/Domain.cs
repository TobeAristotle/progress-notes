using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;

[DataContract]
public class NoteRecord {
    [DataMember] public string Id;
    [DataMember] public string Text;
    [DataMember] public List<NoteFormat> Formats=new List<NoteFormat>();
    public NoteRecord(string text) {Id=Guid.NewGuid().ToString("N");Text=text;}
}
[DataContract]
public class NoteCategory {
    [DataMember] public string Id;
    [DataMember] public string Name;
    [DataMember] public NoteRecord Note;
    public string Content {get{return Note.Text;}set{Note.Text=value;Note.Formats=new List<NoteFormat>();}}
    public NoteCategory(string name,string text) {Id=Guid.NewGuid().ToString("N");Name=name;Note=new NoteRecord(text);}
}
[DataContract]
public class CountdownRecord {
    [DataMember] public string Id=Guid.NewGuid().ToString("N");
    [DataMember] public string Name="";
    [DataMember] public string TargetDate="";
}
[DataContract]
public class ContentDocument {
    [DataMember] public int SchemaVersion=2;
    [DataMember] public string Id=Guid.NewGuid().ToString("N");
    [DataMember] public CountdownRecord Countdown=new CountdownRecord();
    [DataMember] public List<NoteCategory> Categories=new List<NoteCategory>();
    public ContentDocument() {
        Categories.Add(new NoteCategory("随手记","双击这里，写下你的第一条便签。"));
        Categories.Add(new NoteCategory("工作",""));Categories.Add(new NoteCategory("学习",""));Categories.Add(new NoteCategory("生活",""));
    }
}
[DataContract]
public class DeviceSettings {
    [DataMember] public int SchemaVersion=2;
    [DataMember] public int X=-9999;
    [DataMember] public int Y=-9999;
    [DataMember] public int ExpandedWidth=464;
    [DataMember] public int ExpandedHeight=818;
    [DataMember] public bool Collapsed;
        [DataMember] public string ActiveCategoryId;
    [DataMember] public string ThemeName="ios-paper";
    [DataMember] public int BackgroundTransparencyPercent=50;
    [OnDeserializing] void InitializeAppearance(StreamingContext context){ThemeName="ios-paper";BackgroundTransparencyPercent=50;}
}
// Application state combines two independently usable models; only Content is eligible for future synchronization.
public class NoteState {
    public ContentDocument Content=new ContentDocument();
    public DeviceSettings Device=new DeviceSettings();
    public string Note {get{return Content.Categories[0].Content;}set{Content.Categories[0].Content=value;}}
    public string Goal {get{return Content.Countdown.Name;}set{Content.Countdown.Name=value;}}
    public string Target {get{return Content.Countdown.TargetDate;}set{Content.Countdown.TargetDate=value;}}
    public int X {get{return Device.X;}set{Device.X=value;}}
    public int Y {get{return Device.Y;}set{Device.Y=value;}}
    public bool Collapsed {get{return Device.Collapsed;}set{Device.Collapsed=value;}}
    public string ActiveId {get{return Device.ActiveCategoryId;}set{Device.ActiveCategoryId=value;}}
    public List<NoteCategory> Categories {get{return Content.Categories;}}
    public NoteCategory ActiveCategory {get{return Categories.Find(delegate(NoteCategory c){return c.Id==ActiveId;});}}
    public void EnsureCategories() {StateValidation.Validate(this);}
}
[DataContract]
public class WorkspaceEnvelope {
    [DataMember] public int SchemaVersion=2;
    [DataMember] public ContentDocument Content;
    [DataMember] public DeviceSettings Device;
}
public class UnsupportedDataVersionException : IOException {
    public UnsupportedDataVersionException(string section,int version):base(section+"的数据版本（"+version+"）高于当前程序支持范围，请使用更新版本；原文件未修改。"){}
}
public static class StateValidation {
    public static void Version(string section,int version) {
        if(version>1)throw new UnsupportedDataVersionException(section,version);
        if(version!=1)throw new InvalidDataException(section+"缺少有效版本号。");
    }
    public static void Validate(NoteState state) {
        if(state==null||state.Content==null||state.Device==null)throw new InvalidDataException("内容或设备设置缺失。");
        ContentDocument d=state.Content;DeviceSettings local=state.Device;
        if(d.SchemaVersion>2)throw new UnsupportedDataVersionException("内容",d.SchemaVersion);if(d.SchemaVersion<1)throw new InvalidDataException("内容版本无效。");d.SchemaVersion=2;if(local.SchemaVersion>2)throw new UnsupportedDataVersionException("设备设置",local.SchemaVersion);if(local.SchemaVersion<1)throw new InvalidDataException("设备设置版本无效。");
        if(d.Countdown==null||d.Categories==null||d.Categories.Count==0)throw new InvalidDataException("便签或倒计时数据缺失。");
        HashSet<string> ids=new HashSet<string>(StringComparer.Ordinal);HashSet<string> names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        RequireId(d.Id,ids);RequireId(d.Countdown.Id,ids);
        if(d.Countdown.Name==null||d.Countdown.TargetDate==null)throw new InvalidDataException("倒计时字段缺失。");
        DateTime date;
        if(d.Countdown.TargetDate.Length>0&&!DateTime.TryParseExact(d.Countdown.TargetDate,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out date))throw new InvalidDataException("倒计时日期无效。");
        foreach(NoteCategory c in d.Categories) {
            if(c==null||String.IsNullOrWhiteSpace(c.Name)||!names.Add(c.Name)||c.Note==null||c.Note.Text==null)throw new InvalidDataException("分类或正文数据无效。");
            RequireId(c.Id,ids);RequireId(c.Note.Id,ids);NoteFormatting.Validate(c.Note);
        }
        // The current prototype has fixed expanded dimensions; these fields reserve device-only layout state.
        if(local.ThemeName=="sage-paper"||local.ThemeName=="ice-blue")local.ThemeName="ios-paper";
        if(!Themes.Contains(local.ThemeName)||local.BackgroundTransparencyPercent<0||local.BackgroundTransparencyPercent>99)throw new InvalidDataException("本机外观设置无效。");
        local.SchemaVersion=2;
        if(local.ExpandedWidth==440&&local.ExpandedHeight==654){local.ExpandedWidth=464;local.ExpandedHeight=818;}
        if(local.ExpandedWidth==464&&local.ExpandedHeight==740)local.ExpandedHeight=818;
        if(local.ExpandedWidth!=464||local.ExpandedHeight!=818)throw new InvalidDataException("此版本不支持该面板尺寸。");
        if(d.Categories.Find(delegate(NoteCategory c){return c.Id==local.ActiveCategoryId;})==null)local.ActiveCategoryId=d.Categories[0].Id;
    }
    static void RequireId(string id,HashSet<string> ids) {if(String.IsNullOrWhiteSpace(id)||!ids.Add(id))throw new InvalidDataException("记录 ID 缺失或重复。");}
}
