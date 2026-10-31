using System;using System.IO;using System.Drawing;using System.Drawing.Drawing2D;using System.Windows.Forms;using Microsoft.Win32;

public interface IStartupEntry {string Read();void Write(string command);}
public sealed class RegistryStartupEntry:IStartupEntry {
    const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";const string EntryName="JinbuNote";
    public string Read(){using(RegistryKey key=Registry.CurrentUser.OpenSubKey(RunKey,false))return key==null?null:key.GetValue(EntryName,null,RegistryValueOptions.DoNotExpandEnvironmentNames) as string;}
    public void Write(string command){using(RegistryKey key=Registry.CurrentUser.CreateSubKey(RunKey)){if(key==null)throw new IOException("无法访问当前账户的启动设置。");if(command==null)key.DeleteValue(EntryName,false);else key.SetValue(EntryName,command,RegistryValueKind.String);}}
}
public sealed class StartupSetting {
    readonly IStartupEntry entry;readonly string executable;
    public StartupSetting(IStartupEntry backend,string path){entry=backend;executable=Path.GetFullPath(path);}
    public bool Enabled {get{return !String.IsNullOrWhiteSpace(entry.Read());}}
    public static string Command(string executable){if(executable.IndexOf('"')>=0)throw new ArgumentException("程序路径无效。");return "\""+Path.GetFullPath(executable)+"\" --autostart";}
    public void SetEnabled(bool enabled){string previous=entry.Read();string wanted=enabled?Command(executable):null;try{entry.Write(wanted);if(entry.Read()!=wanted)throw new IOException("启动设置写入未成功，请重试。");}catch{try{entry.Write(previous);}catch{}throw;}}
    public void Refresh(){if(Enabled&&entry.Read()!=Command(executable))SetEnabled(true);}
}
public static class AutoStart {
    public static readonly StartupSetting Current=new StartupSetting(new RegistryStartupEntry(),Application.ExecutablePath);
    public static string LastError="";
    public static void RefreshForCurrentVersion(){try{Current.Refresh();LastError="";}catch(Exception e){LastError="开机启动路径未能更新："+e.Message;}}
    public static StartupSetting ForTests(){return new StartupSetting(new MemoryStartupEntry(),Path.Combine(Path.GetTempPath(),"进步签 test.exe"));}
    public static void WaitForShell(Func<bool> ready,Action pause){for(int i=0;i<30;i++){if(ready())return;pause();}if(!ready())throw new IOException("等待桌面就绪超时，请登录后手动打开进步签。");}
}
public sealed class MemoryStartupEntry:IStartupEntry {public string Value;public bool FailNextWrite;public string Read(){return Value;}public void Write(string command){if(FailNextWrite){FailNextWrite=false;throw new IOException("模拟启动设置写入失败");}Value=command;}}
public class StartupToggleButton:Button {
    bool isOn;public bool IsOn {get{return isOn;}set{isOn=value;AccessibleDescription=value?"已开启":"已关闭";Invalidate();}}
    public StartupToggleButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);Cursor=Cursors.Hand;Font=Typography.Chinese(15,FontStyle.Regular);AccessibleRole=AccessibleRole.CheckButton;}
    protected override void OnPaint(PaintEventArgs e){Graphics g=e.Graphics;g.Clear(BlueTheme.Paper);g.SmoothingMode=SmoothingMode.AntiAlias;using(GraphicsPath card=PaperShapes.Rounded(new RectangleF(0,0,Width-1,Height-1),12))using(Brush white=new SolidBrush(BlueTheme.Surface))using(Brush ink=new SolidBrush(BlueTheme.Ink))using(StringFormat sf=new StringFormat{LineAlignment=StringAlignment.Center}){g.FillPath(white,card);Typography.Draw(g,Text,Font,ink,new RectangleF(16,0,Width-92,Height),sf,false);float x=Width-64,y=(Height-24)/2;using(GraphicsPath track=PaperShapes.Rounded(new RectangleF(x,y,44,24),12))using(Brush color=new SolidBrush(IsOn?BlueTheme.Accent:BlueTheme.Border)){g.FillPath(color,track);g.FillEllipse(white,x+(IsOn?22:2),y+2,20,20);}}if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(5,5,Width-10,Height-10));}
}
public static class AutoStartTests {
    static void Check(bool result,string message){if(!result)throw new Exception(message);}
    public static void Run(string directory){Directory.CreateDirectory(directory);MemoryStartupEntry entry=new MemoryStartupEntry();string old=Path.Combine(directory,"older version","进步签.exe"),current=Path.Combine(directory,"new version","进步签.exe");StartupSetting setting=new StartupSetting(entry,current);Check(!setting.Enabled,"Startup must default off");setting.Refresh();Check(entry.Value==null,"Upgrade must not enable default-off startup");setting.SetEnabled(true);Check(setting.Enabled&&entry.Value==StartupSetting.Command(current),"Enable and quoted path failed");entry.Value=StartupSetting.Command(old);setting.Refresh();Check(entry.Value==StartupSetting.Command(current),"Upgrade refresh failed");string previous=entry.Value;entry.FailNextWrite=true;bool failed=false;try{setting.SetEnabled(false);}catch(IOException){failed=true;}Check(failed&&entry.Value==previous,"Failed change must retain old setting");setting.SetEnabled(false);Check(!setting.Enabled&&entry.Value==null,"Disable failed");int calls=0,pauses=0;AutoStart.WaitForShell(delegate{return ++calls>=3;},delegate{pauses++;});Check(pauses==2,"Desktop readiness wait failed");bool timedOut=false;try{AutoStart.WaitForShell(delegate{return false;},delegate{});}catch(IOException){timedOut=true;}Check(timedOut,"Desktop readiness timeout missing");File.WriteAllText(Path.Combine(directory,"autostart-test-result.txt"),"PASS: default off, enable/disable, quoted paths, enabled-only upgrade refresh, failure rollback, desktop readiness and timeout. Tests used an in-memory startup store; real Windows startup entries were not changed. Actual sign-in startup still requires a real sign-in test.");}
}
