using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

public interface IContentRepository {
    ContentDocument LoadContent();
    void SaveContent(ContentDocument document);
}
public interface IDeviceSettingsRepository {
    DeviceSettings LoadDeviceSettings();
    void SaveDeviceSettings(DeviceSettings settings);
}
public interface IWorkspaceRepository : IContentRepository,IDeviceSettingsRepository {
    string Folder {get;}
    NoteState Load();
    void Save(NoteState state);
}
// Two separate sections are committed in ONE atomic JSON file to avoid partial content/settings saves.
public sealed class LocalJsonRepository : IWorkspaceRepository {
    public string Folder {get;private set;}
    public string PathName {get{return Path.Combine(Folder,"workspace.json");}}
    public string BackupPath {get{return Path.Combine(Folder,"workspace.backup.json");}}
    bool recovered;
    public LocalJsonRepository(string folder){Folder=folder;}
    public NoteState ReadOne(string path) {
        WorkspaceEnvelope envelope;
        using(StreamReader reader=new StreamReader(path,Encoding.UTF8,true))using(MemoryStream f=new MemoryStream(Encoding.UTF8.GetBytes(reader.ReadToEnd())))envelope=(WorkspaceEnvelope)new DataContractJsonSerializer(typeof(WorkspaceEnvelope)).ReadObject(f);
        if(envelope==null)throw new InvalidDataException("工作区为空。");
        if(envelope.SchemaVersion>2)throw new UnsupportedDataVersionException("工作区",envelope.SchemaVersion);
        if(envelope.SchemaVersion!=1&&envelope.SchemaVersion!=2)throw new InvalidDataException("工作区格式版本无效。");
        NoteState state=new NoteState{Content=envelope.Content,Device=envelope.Device};StateValidation.Validate(state);return state;
    }
    public NoteState Load() {
        if(!File.Exists(PathName)) {
            if(File.Exists(BackupPath)){NoteState backup=ReadOne(BackupPath);recovered=true;return backup;}
            NoteState initial=new NoteState();initial.EnsureCategories();return initial;
        }
        try{NoteState state=ReadOne(PathName);recovered=false;return state;}
        catch(UnsupportedDataVersionException){throw;}
        catch(Exception primaryError) {
            if(File.Exists(BackupPath)){NoteState backup=ReadOne(BackupPath);recovered=true;return backup;}
            throw new InvalidDataException("数据无法读取，原文件已保留。请检查 data 文件夹。",primaryError);
        }
    }
    public void Save(NoteState state) {
        StateValidation.Validate(state);
        if(File.Exists(PathName)&&!recovered){ReadOne(PathName);SharedData.DailyBackup(Folder,PathName);}
        Directory.CreateDirectory(Folder);
        string temp=Path.Combine(Folder,"workspace-"+Guid.NewGuid().ToString("N")+".tmp");
        using(FileStream f=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
            WorkspaceEnvelope envelope=new WorkspaceEnvelope{Content=state.Content,Device=state.Device};
            new DataContractJsonSerializer(typeof(WorkspaceEnvelope)).WriteObject(f,envelope);f.Flush(true);
        }
        if(File.Exists(PathName)) {
            if(recovered) {
                File.Copy(PathName,Path.Combine(Folder,"workspace.corrupt-"+Guid.NewGuid().ToString("N")+".json"));
                File.Replace(temp,PathName,null,true);
            }else File.Replace(temp,PathName,BackupPath,true);
        }else File.Move(temp,PathName);
        recovered=false;
    }
    public ContentDocument LoadContent(){return Load().Content;}
    public DeviceSettings LoadDeviceSettings(){return Load().Device;}
    public void SaveContent(ContentDocument content){NoteState state=Load();state.Content=content;Save(state);}
    public void SaveDeviceSettings(DeviceSettings settings){NoteState state=Load();state.Device=settings;Save(state);}
}
[DataContract]
class LegacyCategory {
    [DataMember] public string Id {get;set;}
    [DataMember] public string Name {get;set;}
    [DataMember] public string Content {get;set;}
}
[DataContract]
class LegacyState {
    [DataMember] public string Note {get;set;}
    [DataMember] public string Goal {get;set;}
    [DataMember] public string Target {get;set;}
    [DataMember] public int X {get;set;}
    [DataMember] public int Y {get;set;}
    [DataMember] public System.Collections.Generic.List<LegacyCategory> Categories {get;set;}
    [DataMember] public string ActiveId {get;set;}
    [DataMember] public bool Collapsed {get;set;}
}
public static class LegacyImporter {
    public static NoteState ReadOne(string path) {
        LegacyState old;
        using(StreamReader reader=new StreamReader(path,Encoding.UTF8,true))using(MemoryStream f=new MemoryStream(Encoding.UTF8.GetBytes(reader.ReadToEnd())))old=(LegacyState)new DataContractJsonSerializer(typeof(LegacyState)).ReadObject(f);
        if(old==null||old.Note==null||old.Goal==null||old.Target==null)throw new InvalidDataException("旧版数据字段缺失。");
        NoteState state=new NoteState{Note=old.Note,Goal=old.Goal,Target=old.Target,X=old.X,Y=old.Y,Collapsed=old.Collapsed};
        if(old.Categories!=null) {
            if(old.Categories.Count==0)throw new InvalidDataException("旧分类为空。");
            state.Categories.Clear();
            foreach(LegacyCategory c in old.Categories) {
                if(c==null||c.Content==null)throw new InvalidDataException("旧分类正文缺失。");
                NoteCategory migrated=new NoteCategory(c.Name,c.Content);migrated.Id=c.Id;state.Categories.Add(migrated);
            }
        }
        state.ActiveId=old.ActiveId;state.EnsureCategories();return state;
    }
}
