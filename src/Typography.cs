using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Runtime.InteropServices;using System.Windows.Forms;
public static class Typography {
    public static bool ChineseCharacter(char c){return c>='\u2e80'&&c<='\u9fff'||c>='\uf900'&&c<='\ufaff'||c>='\uff00'&&c<='\uffef';}
    public static Font Chinese(float pixels,FontStyle style){return new Font("Microsoft YaHei UI",pixels,style,GraphicsUnit.Pixel);}
    public static Font Latin(float pixels,FontStyle style){return new Font("Times New Roman",pixels,style,GraphicsUnit.Pixel);}
    public static void DrawString(Graphics g,string text,Font font,Brush brush,float x,float y){Draw(g,text,font,brush,new RectangleF(x,y,10000,10000),null,false);}
    public static void DrawString(Graphics g,string text,Font font,Brush brush,RectangleF rect,StringFormat format){Draw(g,text,font,brush,rect,format,false);}
    public static void Draw(Graphics g,string text,Font font,Brush brush,RectangleF rect,StringFormat format,bool wrap,float lineSpacing=1f){
        if(String.IsNullOrEmpty(text)||rect.Width<=0||rect.Height<=0)return;
        float pixels=font.Unit==GraphicsUnit.Pixel?font.Size:font.SizeInPoints*96/72;
        using(Font cn=Chinese(pixels,font.Style))using(Font en=Latin(pixels,font.Style))using(StringFormat measure=(StringFormat)StringFormat.GenericTypographic.Clone()){
            measure.FormatFlags|=StringFormatFlags.MeasureTrailingSpaces;float lineHeight=Math.Max(cn.GetHeight(g),en.GetHeight(g))*lineSpacing;
            System.Collections.Generic.List<string> lines=new System.Collections.Generic.List<string>();string current="";float width=0;
            foreach(char c in text.Replace("\r\n","\n")){if(c=='\r')continue;if(c=='\n'){lines.Add(current);current="";width=0;continue;}Font f=ChineseCharacter(c)?cn:en;float next=g.MeasureString(c.ToString(),f,10000,measure).Width;if(wrap&&current.Length>0&&width+next>rect.Width){lines.Add(current);current="";width=0;}current+=c;width+=next;}lines.Add(current);
            GraphicsState saved=g.Save();g.SetClip(rect,CombineMode.Intersect);
            try{g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;float top=rect.Y;if(format!=null&&format.LineAlignment==StringAlignment.Center)top+=(rect.Height-lineHeight*lines.Count)/2;
            foreach(string line in lines){float total=0;foreach(char c in line)total+=g.MeasureString(c.ToString(),ChineseCharacter(c)?cn:en,10000,measure).Width;float left=rect.X;if(format!=null&&format.Alignment==StringAlignment.Center)left+=(rect.Width-total)/2;else if(format!=null&&format.Alignment==StringAlignment.Far)left+=rect.Width-total;
            foreach(char c in line){Font f=ChineseCharacter(c)?cn:en;float ascent=f.FontFamily.GetCellAscent(f.Style)*f.Size/f.FontFamily.GetEmHeight(f.Style);float cnAscent=cn.FontFamily.GetCellAscent(cn.Style)*cn.Size/cn.FontFamily.GetEmHeight(cn.Style),enAscent=en.FontFamily.GetCellAscent(en.Style)*en.Size/en.FontFamily.GetEmHeight(en.Style);g.DrawString(c.ToString(),f,brush,left,top+Math.Max(cnAscent,enAscent)-ascent,measure);left+=g.MeasureString(c.ToString(),f,10000,measure).Width;}top+=lineHeight;if(top>=rect.Bottom)break;}}
            finally{g.Restore(saved);}
        }
    }
}
public class MixedLabel:Label {
    protected override void OnPaint(PaintEventArgs e){using(SolidBrush b=new SolidBrush(ForeColor))Typography.Draw(e.Graphics,Text,Font,b,new RectangleF(Padding.Left,Padding.Top,Width-Padding.Horizontal,Height-Padding.Vertical),null,true);}
}
public class MixedButton:Button {
    protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);if(FlatAppearance.BorderSize>0)using(Pen p=new Pen(ForeColor))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);using(SolidBrush b=new SolidBrush(ForeColor))using(StringFormat sf=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})Typography.Draw(e.Graphics,Text,Font,b,new RectangleF(0,0,Width,Height),sf,false);}
}
public class MixedMenuRenderer:ToolStripProfessionalRenderer {
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){using(SolidBrush b=new SolidBrush(e.TextColor))Typography.Draw(e.Graphics,e.Text,e.TextFont,b,e.TextRectangle,null,false);}
}
public class MixedRichTextBox:RichTextBox {
    bool formatting,composing;IntPtr document;
    public bool IsImeComposing {get{return composing;}}
    public event EventHandler CompositionEnded;
    public MixedRichTextBox(){Font=Typography.Latin(14.667f,FontStyle.Regular);DetectUrls=false;EnableAutoDragDrop=false;}
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);IntPtr ole;SendMessage(Handle,0x043c,IntPtr.Zero,out ole);if(ole!=IntPtr.Zero){Guid id=new Guid("8cc497c0-a1df-11ce-8098-00aa0047be5d");Marshal.QueryInterface(ole,ref id,out document);Marshal.Release(ole);}ApplyFonts();}
    protected override void OnHandleDestroyed(EventArgs e){if(document!=IntPtr.Zero){Marshal.Release(document);document=IntPtr.Zero;}base.OnHandleDestroyed(e);}
    protected override void OnTextChanged(EventArgs e){if(formatting)return;if(!composing)ApplyFonts();base.OnTextChanged(e);}
    protected override void WndProc(ref Message m){if(m.Msg==0x10d)composing=true;base.WndProc(ref m);if(m.Msg==0x10e){composing=false;if(IsHandleCreated)BeginInvoke((Action)delegate{if(!IsDisposed&&IsHandleCreated)ApplyFonts();});if(CompositionEnded!=null)CompositionEnded(this,EventArgs.Empty);}}
    void UndoRecording(bool resume){if(document==IntPtr.Zero)return;IntPtr table=Marshal.ReadIntPtr(document);UndoDelegate undo=(UndoDelegate)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(table,22*IntPtr.Size),typeof(UndoDelegate));int count;undo(document,resume?-9999994:-9999995,out count);}
    public void ApplyFonts(){if(formatting||composing||!IsHandleCreated)return;formatting=true;int start=SelectionStart,length=SelectionLength;bool modified=Modified;POINT scroll=new POINT();SendPoint(Handle,0x04dd,IntPtr.Zero,ref scroll);SendSimple(Handle,11,IntPtr.Zero,IntPtr.Zero);
        try{UndoRecording(false);float px=Font.Unit==GraphicsUnit.Pixel?Font.Size:Font.SizeInPoints*96/72;using(Font cn=Typography.Chinese(px,Font.Style))using(Font en=Typography.Latin(px,Font.Style)){
            SelectAll();SelectionFont=en;string text=Text;for(int i=0;i<text.Length;){if(!Typography.ChineseCharacter(text[i])){i++;continue;}int first=i;while(i<text.Length&&Typography.ChineseCharacter(text[i]))i++;Select(first,i-first);SelectionFont=cn;}Select(Math.Min(start,TextLength),Math.Min(length,Math.Max(0,TextLength-start)));if(length==0)SelectionFont=start>0&&start<=text.Length&&Typography.ChineseCharacter(text[start-1])?cn:en;
        }}finally{UndoRecording(true);Modified=modified;SendPoint(Handle,0x04de,IntPtr.Zero,ref scroll);SendSimple(Handle,11,new IntPtr(1),IntPtr.Zero);formatting=false;Invalidate();}}
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int UndoDelegate(IntPtr self,int value,out int count);
    [StructLayout(LayoutKind.Sequential)]struct POINT{public int X,Y;}
    [DllImport("user32.dll",EntryPoint="SendMessageW")]static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,out IntPtr value);
    [DllImport("user32.dll",EntryPoint="SendMessageW")]static extern IntPtr SendSimple(IntPtr h,int msg,IntPtr w,IntPtr value);
    [DllImport("user32.dll",EntryPoint="SendMessageW")]static extern IntPtr SendPoint(IntPtr h,int msg,IntPtr w,ref POINT value);
}
