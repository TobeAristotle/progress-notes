using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows.Forms;

// These windows remain accessible from the desktop/tray without an Alt+Tab entry.
public class UtilityForm : Form {
    protected override CreateParams CreateParams { get { CreateParams p=base.CreateParams;p.ExStyle=(p.ExStyle&~0x00040000)|0x00000080;return p; } }
}
public sealed class HighlightFormatButton:EditorDoneButton {
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(Brush yellow=new SolidBrush(NoteFormatting.Yellow))e.Graphics.FillRectangle(yellow,12,Height-7,Width-24,3);}
}
public sealed class NoteTextPreview:SelectableNoteLabel {
    protected override RectangleF TextBounds {get{return new RectangleF(0,0,Width,Height);}}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);PaintNoteContent(e.Graphics);}
}

[DataContract]
public sealed class NoteFormat {
    [DataMember] public int Start;
    [DataMember] public int Length;
    [DataMember] public bool Bold;
    [DataMember] public bool Highlight;
}
public static class NoteFormatting {
    public static readonly Color Yellow=Color.FromArgb(255,232,112);
    public static readonly Color HighlightInk=Color.FromArgb(64,57,26);
    public static List<NoteFormat> Copy(List<NoteFormat> source){List<NoteFormat> result=new List<NoteFormat>();if(source!=null)foreach(NoteFormat f in source)result.Add(new NoteFormat{Start=f.Start,Length=f.Length,Bold=f.Bold,Highlight=f.Highlight});return result;}
    public static void Validate(NoteRecord note){if(note.Formats==null)note.Formats=new List<NoteFormat>();int end=0;foreach(NoteFormat f in note.Formats){if(f==null||f.Start<end||f.Length<=0||f.Start>note.Text.Length-f.Length||(!f.Bold&&!f.Highlight))throw new InvalidDataException("笔记文字格式无效，原文件已保留。");end=f.Start+f.Length;}}
    public static void Import(MixedRichTextBox box,List<NoteFormat> formats){int start=box.SelectionStart,length=box.SelectionLength;box.SelectAll();NativeCharacter.SetBold(box,false);box.SelectionBackColor=box.BackColor;box.SelectionColor=BlueTheme.Ink;if(formats!=null)foreach(NoteFormat f in formats){box.Select(f.Start,f.Length);NativeCharacter.SetBold(box,f.Bold);box.SelectionBackColor=f.Highlight?Yellow:box.BackColor;box.SelectionColor=f.Highlight?HighlightInk:BlueTheme.Ink;}box.Select(start,length);box.ApplyFonts();}
    public static List<NoteFormat> Export(MixedRichTextBox box){List<NoteFormat> result=new List<NoteFormat>();int start=box.SelectionStart,length=box.SelectionLength;Point scroll=NativeCharacter.Scroll(box);box.SuspendLayout();try{NoteFormat run=null;for(int i=0;i<box.TextLength;i++){box.Select(i,1);bool bold=NativeCharacter.IsBold(box),highlight=box.SelectionBackColor.ToArgb()==Yellow.ToArgb();if(!bold&&!highlight){run=null;continue;}if(run!=null&&run.Start+run.Length==i&&run.Bold==bold&&run.Highlight==highlight)run.Length++;else{run=new NoteFormat{Start=i,Length=1,Bold=bold,Highlight=highlight};result.Add(run);}}}finally{box.Select(start,length);NativeCharacter.Scroll(box,scroll);box.ResumeLayout();}return result;}
}
public static class NativeCharacter {
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct CHARFORMAT {
        public uint cbSize,mask,effects;public int height,offset,color;public byte charset,pitch;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string face;
    }
    [StructLayout(LayoutKind.Sequential)]struct POINT{public int X,Y;}
    [DllImport("user32.dll",EntryPoint="SendMessageW",CharSet=CharSet.Unicode)]static extern IntPtr Format(IntPtr h,int m,IntPtr w,ref CHARFORMAT f);
    [DllImport("user32.dll",EntryPoint="SendMessageW")]static extern IntPtr SendPoint(IntPtr h,int m,IntPtr w,ref POINT p);
    static CHARFORMAT New(){return new CHARFORMAT{cbSize=(uint)Marshal.SizeOf(typeof(CHARFORMAT)),face=""};}
    public static void Face(MixedRichTextBox box,Font font){CHARFORMAT f=New();f.mask=0xa0000000;f.face=font.Name;f.height=(int)Math.Round(font.SizeInPoints*20);Format(box.Handle,0x0444,new IntPtr(1),ref f);}
    public static void SetBold(MixedRichTextBox box,bool bold){CHARFORMAT f=New();f.mask=1;f.effects=bold?1u:0u;Format(box.Handle,0x0444,new IntPtr(1),ref f);}
    public static bool IsBold(MixedRichTextBox box){CHARFORMAT f=New();Format(box.Handle,0x043a,new IntPtr(1),ref f);return (f.mask&1)!=0&&(f.effects&1)!=0;}
    public static Point Scroll(MixedRichTextBox box){POINT p=new POINT();SendPoint(box.Handle,0x04dd,IntPtr.Zero,ref p);return new Point(p.X,p.Y);}
    public static void Scroll(MixedRichTextBox box,Point value){POINT p=new POINT{X=value.X,Y=value.Y};SendPoint(box.Handle,0x04de,IntPtr.Zero,ref p);}
}
public static class CategoryDeletion {
    public static bool Delete(NoteState state,string id,IWorkspaceRepository store){
        int index=state.Categories.FindIndex(delegate(NoteCategory c){return c.Id==id;});
        if(index<0||state.Categories.Count<=1)return false;
        // Save a complete, immutable snapshot before removing any category.
        string folder=Path.Combine(store.Folder,"backups","deleted-category-"+DateTime.Now.ToString("yyyyMMdd-HHmmssfff")+"-"+Guid.NewGuid().ToString("N"));
        LocalJsonRepository archive=new LocalJsonRepository(folder);archive.Save(state);
        string active=state.ActiveId;NoteCategory deleted=state.Categories[index];state.Categories.RemoveAt(index);
        if(state.ActiveId==id)state.ActiveId=state.Categories[Math.Min(index,state.Categories.Count-1)].Id;
        try{store.Save(state);}catch{state.Categories.Insert(index,deleted);state.ActiveId=active;throw;}
        return true;
    }
}
