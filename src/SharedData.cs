using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;

public static class SharedData {
    public const string ProgramVersion="1.23";
    public static string Folder {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DesktopNote","data");}}
    public static FileStream OpenWriterLock(string folder){Directory.CreateDirectory(folder);try{return new FileStream(Path.Combine(folder,"workspace.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}catch(IOException e){throw new IOException("数据目录正在被另一个便签进程使用，请先退出旧版。",e);}}
    public static string Hash(string file){using(SHA256 sha=SHA256.Create())using(FileStream stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");}
    public static string StateHash(NoteState state){using(MemoryStream stream=new MemoryStream()){new DataContractJsonSerializer(typeof(WorkspaceEnvelope)).WriteObject(stream,new WorkspaceEnvelope{Content=state.Content,Device=state.Device});using(SHA256 sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream.ToArray()));}}
    static string Unique(string prefix){return prefix+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmssfff")+"-"+Guid.NewGuid().ToString("N").Substring(0,8);}
    static void CopyVerified(string source,string destination){Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(source,destination,false);if(Hash(source)!=Hash(destination))throw new IOException("备份校验失败，未迁移数据："+source);}
    static void AtomicText(string file,string text){Directory.CreateDirectory(Path.GetDirectoryName(file));string temp=file+"-"+Guid.NewGuid().ToString("N")+".tmp";using(FileStream stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){byte[] bytes=Encoding.UTF8.GetBytes(text);stream.Write(bytes,0,bytes.Length);stream.Flush(true);}if(File.Exists(file))File.Replace(temp,file,null,true);else File.Move(temp,file);}
    public static int Schema(string file){using(StreamReader reader=new StreamReader(file,Encoding.UTF8,true))using(MemoryStream stream=new MemoryStream(Encoding.UTF8.GetBytes(reader.ReadToEnd()))){WorkspaceEnvelope envelope=(WorkspaceEnvelope)new DataContractJsonSerializer(typeof(WorkspaceEnvelope)).ReadObject(stream);if(envelope==null)throw new InvalidDataException("工作区为空。");return envelope.SchemaVersion;}}
    static int ContentSchema(string file){using(FileStream stream=File.OpenRead(file)){WorkspaceEnvelope envelope=(WorkspaceEnvelope)new DataContractJsonSerializer(typeof(WorkspaceEnvelope)).ReadObject(stream);return envelope.Content.SchemaVersion;}}
    public static void DailyBackup(string folder,string primary){string file=Path.Combine(folder,"backups","daily",DateTime.Now.ToString("yyyy-MM-dd")+"-workspace.json");if(!File.Exists(file))CopyVerified(primary,file);}
    static string UpgradeBackup(LocalJsonRepository repository,string previous){string backup=Path.Combine(repository.Folder,"backups",Unique("upgrade"));Directory.CreateDirectory(backup);foreach(string name in new string[]{"workspace.json","workspace.backup.json","application-version.txt","migration-complete.txt"}){string source=Path.Combine(repository.Folder,name);if(File.Exists(source))CopyVerified(source,Path.Combine(backup,name));}File.WriteAllText(Path.Combine(backup,"说明.txt"),"升级前副本。程序版本 "+previous+" → "+ProgramVersion+"。如需恢复，请先退出程序并保留当前数据目录。",Encoding.UTF8);return backup;}
    static string ArchiveLegacy(string programRoot,string destination,string selected){string archive=Path.Combine(destination,"backups",Unique("legacy-import"));Directory.CreateDirectory(archive);StringBuilder manifest=new StringBuilder("source\tarchive\tsha256\r\n");        foreach(string name in new string[]{"workspace.json","workspace.backup.json","state.json","state.backup.json"}){string source=Path.Combine(selected,name);if(File.Exists(source)){string relative=Path.Combine("selected-source",name);string target=Path.Combine(archive,relative);CopyVerified(source,target);manifest.Append(source).Append('\t').Append(relative).Append('\t').Append(Hash(target)).Append("\r\n");}}
        File.WriteAllText(Path.Combine(archive,"manifest.tsv"),manifest.ToString(),Encoding.UTF8);return archive;
    }
    // Call only while holding the application mutex and fixed-directory writer lock.
    public static void Prepare(string programRoot,LocalJsonRepository repository,string selectedSource=null){
        string marker=Path.Combine(repository.Folder,"application-version.txt");NoteState state;
        if(!File.Exists(repository.PathName)&&!File.Exists(repository.BackupPath)){
            string source=selectedSource;
            bool sourceExists=source!=null&&(File.Exists(Path.Combine(source,"workspace.json"))||File.Exists(Path.Combine(source,"workspace.backup.json")));
            if(!sourceExists){if(source!=null)throw new IOException("所选目录不包含可读取的 workspace.json 或 workspace.backup.json，未导入数据。");state=new NoteState();state.EnsureCategories();repository.Save(state);}
            else {LocalJsonRepository old=new LocalJsonRepository(source);state=old.Load();string before=StateHash(state);string archive=ArchiveLegacy(programRoot,repository.Folder,source);repository.Save(state);if(StateHash(repository.Load())!=before)throw new IOException("迁入后校验失败，原件与归档已保留。");AtomicText(Path.Combine(repository.Folder,"migration-complete.txt"),"source="+source+"\r\narchive="+archive+"\r\nstateHash="+before+"\r\nprogramVersion="+ProgramVersion+"\r\n");}
        }
        // Loading first rejects future formats without silently recovering an older backup.
        state=repository.Load();string previous=File.Exists(marker)?File.ReadAllText(marker,Encoding.UTF8).Trim():"legacy";
        bool needsFormat=!File.Exists(repository.PathName)||Schema(repository.PathName)<2||ContentSchema(repository.PathName)<2;
        if(previous!=ProgramVersion||needsFormat){UpgradeBackup(repository,previous);if(needsFormat)repository.Save(state);AtomicText(marker,ProgramVersion);}
    }
}

public static class SharedDataTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);}
    public static void Run(string output){Directory.CreateDirectory(output);string root=Path.Combine(output,"case-"+Guid.NewGuid().ToString("N"));string oldFolder=Path.Combine(root,"DesktopNotePrototype-v1.10","data");string program=Path.Combine(root,"DesktopNotePrototype-v1.11");Directory.CreateDirectory(program);LocalJsonRepository old=new LocalJsonRepository(oldFolder);NoteState original=new NoteState{Note="用户记录\n第二行",Goal="提交论文",Target="2026-11-01",X=123,Y=44};old.Save(original);
        File.WriteAllText(old.PathName,File.ReadAllText(old.PathName,Encoding.UTF8).Replace("\"SchemaVersion\":2","\"SchemaVersion\":1"),Encoding.UTF8);string originalHash=SharedData.Hash(old.PathName);
        LocalJsonRepository fixedStore=new LocalJsonRepository(Path.Combine(root,"fixed","data"));using(FileStream lease=SharedData.OpenWriterLock(fixedStore.Folder)){
            bool locked=false;try{using(FileStream second=SharedData.OpenWriterLock(fixedStore.Folder)){} }catch(IOException){locked=true;}Check(locked,"second writer rejected");
            SharedData.Prepare(program,fixedStore,oldFolder);NoteState migrated=fixedStore.Load();Check(migrated.Note==original.Note&&migrated.Goal==original.Goal&&migrated.Target==original.Target&&migrated.X==123,"migration preserves note, date and settings");Check(SharedData.Schema(fixedStore.PathName)==2,"format upgraded");Check(SharedData.Hash(old.PathName)==originalHash,"legacy source untouched");string[] archives=Directory.GetDirectories(Path.Combine(fixedStore.Folder,"backups"),"legacy-import-*");Check(archives.Length==1,"legacy archive exists");Check(SharedData.Hash(Path.Combine(archives[0],"selected-source","workspace.json"))==originalHash,"archive exact hash");
            original.Note="旧目录后续改动";old.Save(original);SharedData.Prepare(program,fixedStore,oldFolder);Check(fixedStore.Load().Note==migrated.Note,"never reimport after fixed data exists");
            string marker=Path.Combine(fixedStore.Folder,"application-version.txt");File.WriteAllText(marker,"previous-version");string before=SharedData.Hash(fixedStore.PathName);int count=Directory.GetDirectories(Path.Combine(fixedStore.Folder,"backups"),"upgrade-*").Length;SharedData.Prepare(program,fixedStore,oldFolder);Check(Directory.GetDirectories(Path.Combine(fixedStore.Folder,"backups"),"upgrade-*").Length==count+1,"upgrade snapshot created");Check(SharedData.Hash(fixedStore.PathName)==before,"upgrade without format change preserves data");
            migrated.Note="新版编辑";fixedStore.Save(migrated);string daily=Path.Combine(fixedStore.Folder,"backups","daily",DateTime.Now.ToString("yyyy-MM-dd")+"-workspace.json");Check(File.Exists(daily),"daily backup exists");string dailyHash=SharedData.Hash(daily);migrated.Note="再次编辑";fixedStore.Save(migrated);Check(SharedData.Hash(daily)==dailyHash,"daily backup immutable");
            string future=File.ReadAllText(fixedStore.PathName,Encoding.UTF8).Replace("\"SchemaVersion\":2","\"SchemaVersion\":99");File.WriteAllText(fixedStore.PathName,future,Encoding.UTF8);string futureHash=SharedData.Hash(fixedStore.PathName);bool rejected=false;try{fixedStore.Load();}catch(UnsupportedDataVersionException){rejected=true;}Check(rejected,"future format does not fall back to old backup");rejected=false;try{fixedStore.Save(migrated);}catch(UnsupportedDataVersionException){rejected=true;}Check(rejected&&SharedData.Hash(fixedStore.PathName)==futureHash,"downgrade save rejected without changing bytes");
        }
        LocalJsonRepository clean=new LocalJsonRepository(Path.Combine(root,"fresh-install","data"));using(FileStream lease=SharedData.OpenWriterLock(clean.Folder)){SharedData.Prepare(program,clean);Check(clean.Load().Note!=original.Note&&clean.Load().Goal.Length==0,"fresh install must not import neighboring old records");}
        File.WriteAllText(Path.Combine(output,"shared-data-test-result.txt"),"PASS: exclusive writer, legacy v1 to v2 migration, exact source/archive hashes, note/date/settings retention, no reimport, upgrade snapshots, immutable daily backup, future-format read/write rejection. All cases isolated from real user data.",Encoding.UTF8);
    }
}
