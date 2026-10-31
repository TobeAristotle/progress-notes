using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public static class PaperBitmap {
    // Background alpha is separate from text alpha. Never lower the entire form's Opacity.
    public static Bitmap Convert(Bitmap source,int transparencyPercent) {
        Bitmap output=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppArgb);
        using(Graphics g=Graphics.FromImage(output))g.DrawImage(source,new Rectangle(0,0,source.Width,source.Height),0,0,source.Width,source.Height,GraphicsUnit.Pixel);
        Rectangle rect=new Rectangle(0,0,output.Width,output.Height);
        BitmapData data=output.LockBits(rect,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
        try {
            byte[] bytes=new byte[data.Stride*data.Height];Marshal.Copy(data.Scan0,bytes,0,bytes.Length);
            byte alpha=(byte)Math.Round(255.0*(100-transparencyPercent)/100);
            for(int y=0;y<data.Height;y++)for(int x=0;x<data.Width;x++) {
                int i=y*data.Stride+x*4;byte r=bytes[i+2],green=bytes[i+1],blue=bytes[i];
                bytes[i+3]=BlueTheme.IsBackground(r,green,blue)?alpha:(byte)255;
            }
            // A narrow light outline keeps dark letter strokes legible on dark wallpapers.
            byte[] original=(byte[])bytes.Clone();
            for(int y=1;y<data.Height-1;y++)for(int x=1;x<data.Width-1;x++) {
                int i=y*data.Stride+x*4;
                if(alpha==255||original[i+3]!=alpha)continue;
                bool nearInk=false;
                for(int dy=-1;dy<=1&&!nearInk;dy++)for(int dx=-1;dx<=1;dx++) {
                    int j=(y+dy)*data.Stride+(x+dx)*4;
                    if(original[j+3]==255&&(BlueTheme.IsDark?(original[j+2]>190&&original[j+1]>190&&original[j]>190):(original[j+2]<160&&original[j+1]<175&&original[j]<160))){nearInk=true;break;}
                }
                if(nearInk){bytes[i]=BlueTheme.IsDark?(byte)48:(byte)246;bytes[i+1]=BlueTheme.IsDark?(byte)33:(byte)251;bytes[i+2]=BlueTheme.IsDark?(byte)25:(byte)244;bytes[i+3]=210;}
            }
            for(int y=0;y<data.Height;y++)for(int x=0;x<data.Width;x++){
                int i=y*data.Stride+x*4;
                if(!PaperShapes.Inside(x+.5f,y+.5f,0,0,data.Width,data.Height,Math.Min(28,data.Height/2f)))bytes[i+3]=0;
                else {
                    if(x<2||y<2||x>=data.Width-2||y>=data.Height-2){float t=(float)y/Math.Max(1,data.Height-1);Color outer=BlueTheme.OuterAt(t);bytes[i]=outer.B;bytes[i+1]=outer.G;bytes[i+2]=outer.R;bytes[i+3]=alpha;}
                    float top=data.Height<100?12:19,bottom=data.Height<100?50:799;
                    if(x>=76&&x<444&&y>=top&&y<bottom&&!PaperShapes.Inside(x+.5f,y+.5f,76,top,444,bottom,Math.Min(24,(bottom-top)/2))){
                        float t=(float)y/Math.Max(1,data.Height-1);Color outer=BlueTheme.OuterAt(t);bytes[i]=outer.B;bytes[i+1]=outer.G;bytes[i+2]=outer.R;bytes[i+3]=alpha;
                    }
                }
            }
            Marshal.Copy(bytes,0,data.Scan0,bytes.Length);
        }finally{output.UnlockBits(data);}
        return output;
    }
}
public sealed class PaperLayer : IDisposable {
    readonly Form form;int transparency;readonly Timer timer;readonly Func<Size> targetSize;
    bool dirty=true,rendering,started;
    public int FrameCount {get;private set;}
    public bool LastPresented {get;private set;}
    public PaperLayer(Form target,int percent,Func<Size> size){form=target;transparency=percent;targetSize=size;timer=new Timer{Interval=100};timer.Tick+=delegate{if(dirty)Present();};Watch(form);}
    void Watch(Control c) {
        c.Invalidated+=delegate{if(!rendering)dirty=true;};c.SizeChanged+=delegate{dirty=true;};c.VisibleChanged+=delegate{dirty=true;};
        c.ControlAdded+=delegate(object sender,ControlEventArgs e){Watch(e.Control);dirty=true;};c.ControlRemoved+=delegate{dirty=true;};
        c.MouseEnter+=delegate{dirty=true;};c.MouseLeave+=delegate{dirty=true;};c.MouseDown+=delegate{dirty=true;};c.MouseUp+=delegate{dirty=true;};
        foreach(Control child in c.Controls)Watch(child);
    }
    public void Start(){if(started)return;started=true;int style=Native.GetWindowLong(form.Handle,-20);Native.SetWindowLong(form.Handle,-20,style|0x00080000);Present();timer.Start();}
    public void SetTransparency(int value){transparency=value;dirty=true;}
    public void Invalidate(){dirty=true;}
    void Present() {
        if(!started||rendering||form.IsDisposed||form.Width<=0||form.Height<=0)return;
        rendering=true;
        try {
            Size size=targetSize();
            using(Bitmap source=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb)) {
                form.DrawToBitmap(source,new Rectangle(Point.Empty,size));
                using(Bitmap frame=PaperBitmap.Convert(source,transparency))Push(frame);
            }
            dirty=false;FrameCount++;
        }finally{rendering=false;}
    }
    void Push(Bitmap frame) {
        IntPtr screen=GetDC(IntPtr.Zero),memory=IntPtr.Zero,bitmap=IntPtr.Zero,previous=IntPtr.Zero;
        try {
            memory=CreateCompatibleDC(screen);
            // GDI requires premultiplied alpha for a per-pixel layered window.
            using(Bitmap premultiplied=new Bitmap(frame.Width,frame.Height,PixelFormat.Format32bppPArgb)) {
                using(Graphics g=Graphics.FromImage(premultiplied)){g.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;g.DrawImage(frame,new Rectangle(0,0,frame.Width,frame.Height),0,0,frame.Width,frame.Height,GraphicsUnit.Pixel);}
                bitmap=premultiplied.GetHbitmap(Color.FromArgb(0));
            }
            previous=SelectObject(memory,bitmap);SIZE size=new SIZE(frame.Width,frame.Height);POINT source=new POINT(0,0);BLEND blend=new BLEND{BlendOp=0,BlendFlags=0,SourceConstantAlpha=255,AlphaFormat=1};
            LastPresented=UpdateLayeredWindow(form.Handle,screen,IntPtr.Zero,ref size,memory,ref source,0,ref blend,2);
            if(!LastPresented)throw new InvalidOperationException("透明纸张渲染失败，错误码："+Marshal.GetLastWin32Error());
        }finally{
            if(previous!=IntPtr.Zero&&memory!=IntPtr.Zero)SelectObject(memory,previous);
            if(bitmap!=IntPtr.Zero)DeleteObject(bitmap);if(memory!=IntPtr.Zero)DeleteDC(memory);if(screen!=IntPtr.Zero)ReleaseDC(IntPtr.Zero,screen);
        }
    }
    public void Dispose(){timer.Stop();timer.Dispose();}
    [StructLayout(LayoutKind.Sequential)]struct POINT{public int X,Y;public POINT(int x,int y){X=x;Y=y;}}
    [StructLayout(LayoutKind.Sequential)]struct SIZE{public int W,H;public SIZE(int w,int h){W=w;H=h;}}
    [StructLayout(LayoutKind.Sequential,Pack=1)]struct BLEND{public byte BlendOp,BlendFlags,SourceConstantAlpha,AlphaFormat;}
    [DllImport("user32.dll")]static extern IntPtr GetDC(IntPtr h);
    [DllImport("user32.dll")]static extern int ReleaseDC(IntPtr h,IntPtr dc);
    [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr h);
    [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr h);
    [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr h);
    [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
    [DllImport("user32.dll",SetLastError=true)]static extern bool UpdateLayeredWindow(IntPtr h,IntPtr dst,IntPtr position,ref SIZE size,IntPtr source,ref POINT p,uint key,ref BLEND blend,uint flags);
}

public static class TransparencyChecks {
    public static void Run(string path){using(Bitmap source=new Bitmap(464,818)){using(Graphics g=Graphics.FromImage(source)){g.Clear(BlueTheme.Outer);using(Brush shell=new SolidBrush(BlueTheme.Paper))g.FillRectangle(shell,76,19,368,780);using(Brush paper=new SolidBrush(BlueTheme.Surface)){g.FillRectangle(paper,88,64,344,146);g.FillRectangle(paper,88,279,344,520);}using(Brush task=new SolidBrush(BlueTheme.Soft))g.FillRectangle(task,88,216,344,63);using(Brush border=new SolidBrush(BlueTheme.Border))g.FillRectangle(border,100,350,20,2);using(Brush ink=new SolidBrush(BlueTheme.Ink))g.FillRectangle(ink,160,400,4,4);}
        foreach(int percent in new int[]{0,50,80,99})using(Bitmap result=PaperBitmap.Convert(source,percent)){int alpha=(int)Math.Round(255.0*(100-percent)/100);foreach(Point sample in new Point[]{new Point(20,400),new Point(80,400),new Point(100,100),new Point(100,240),new Point(100,400),new Point(108,350),new Point(1,400),new Point(77,20)})if(result.GetPixel(sample.X,sample.Y).A!=alpha)throw new Exception("Background alpha mismatch: "+sample+" at "+percent);if(result.GetPixel(161,401).A!=255)throw new Exception("Note ink faded");if(result.GetPixel(0,0).A!=0)throw new Exception("Rounded corner not transparent");}
    }System.IO.File.WriteAllText(path,"PASS: all background layers, borders and outer frame follow 0/50/80/99 percent; note ink stays opaque; rounded corners remain transparent.");}
}
