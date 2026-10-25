using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public static class Native {
    public delegate bool EnumProc(IntPtr h, IntPtr p);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr p);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string name);
    [DllImport("user32.dll", SetLastError=true)] public static extern IntPtr SetParent(IntPtr child, IntPtr parent);
    [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr child);
    [DllImport("user32.dll", EntryPoint="GetWindowLongW")] public static extern int GetWindowLong(IntPtr h, int index);
    [DllImport("user32.dll", EntryPoint="SetWindowLongW", SetLastError=true)] public static extern int SetWindowLong(IntPtr h, int index, int value);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int height, uint flags);
    [DllImport("user32.dll")] public static extern bool ScreenToClient(IntPtr h, ref POINT point);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X,Y; public POINT(int x,int y) { X=x;Y=y; } }
    public static IntPtr DesktopView() {
        IntPtr found = IntPtr.Zero;
        EnumWindows(delegate(IntPtr h, IntPtr p) {
            IntPtr v = FindWindowEx(h, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (v != IntPtr.Zero) { found=v; return false; } return true;
        },IntPtr.Zero);
        return found;
    }
}
public class CalendarView : Control {
    public DateTime Month=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1);
    public DateTime? SelectedDate;
    public CalendarView(){DoubleBuffered=true;Size=new Size(344,158);BackColor=BlueTheme.Paper;}
    public static int FirstColumn(DateTime month){return ((int)month.DayOfWeek+6)%7;}
    public static string LunarText(DateTime date) {
        System.Globalization.ChineseLunisolarCalendar cal=new System.Globalization.ChineseLunisolarCalendar();
        if(date<cal.MinSupportedDateTime||date>cal.MaxSupportedDateTime)return "";
        int year=cal.GetYear(date),month=cal.GetMonth(date),day=cal.GetDayOfMonth(date),leap=cal.GetLeapMonth(year);
        bool isLeap=leap>0&&month==leap;if(leap>0&&month>=leap)month--;
        string[] months={"正","二","三","四","五","六","七","八","九","十","冬","腊"};string[] digit={"一","二","三","四","五","六","七","八","九"};
        string text=day==10?"初十":day==20?"二十":day==30?"三十":day<10?"初"+digit[day-1]:day<20?"十"+digit[day-11]:"廿"+digit[day-21];
        return (isLeap?"闰":"")+months[month-1]+"月"+text;
    }
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);Graphics g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using(System.Drawing.Drawing2D.GraphicsPath card=new System.Drawing.Drawing2D.GraphicsPath()) {
            int r=18;card.AddArc(0,0,r,r,180,90);card.AddArc(Width-r-1,0,r,r,270,90);card.AddArc(Width-r-1,Height-r-1,r,r,0,90);card.AddArc(0,Height-r-1,r,r,90,90);card.CloseFigure();using(SolidBrush paper=new SolidBrush(BlueTheme.Surface))g.FillPath(paper,card);
        }
        using(Font title=new Font("Microsoft YaHei UI",18,FontStyle.Bold,GraphicsUnit.Pixel))
        using(Font normal=new Font("Microsoft YaHei UI",15,FontStyle.Regular,GraphicsUnit.Pixel))
        using(Font small=new Font("Microsoft YaHei UI",13,FontStyle.Regular,GraphicsUnit.Pixel))
        using(SolidBrush ink=new SolidBrush(BlueTheme.Ink))
        using(SolidBrush muted=new SolidBrush(Color.FromArgb(135,139,145)))
        using(SolidBrush red=new SolidBrush(Color.FromArgb(226,73,70))) {
            Typography.DrawString(g,Month.Month+"月",title,red,10,3);Typography.DrawString(g,"│",small,muted,48,6);
            DateTime lunar=Month.Year==DateTime.Today.Year&&Month.Month==DateTime.Today.Month?DateTime.Today:Month;
            Typography.DrawString(g,LunarText(lunar),small,muted,65,5);
            Typography.DrawString(g,"‹",title,ink,Width-80,2);Typography.DrawString(g,"今",small,ink,Width-55,5);Typography.DrawString(g,"›",title,ink,Width-26,2);
            string[] week={"一","二","三","四","五","六","日"};float cw=(Width-24)/7f;
            StringFormat centered=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};
            for(int col=0;col<7;col++)Typography.DrawString(g,week[col],small,col>=5?muted:ink,new RectangleF(12+col*cw,31,cw,23),centered);
            int offset=FirstColumn(Month);
            for(int day=1;day<=DateTime.DaysInMonth(Month.Year,Month.Month);day++){
                int n=offset+day-1,col=n%7,row=n/7;float x=12+col*cw,y=54+row*18;DateTime date=Month.AddDays(day-1);bool today=date==DateTime.Today;
                if(today)g.FillEllipse(red,x+(cw-20)/2,y,20,20);
                Typography.DrawString(g,day.ToString(),normal,today?Brushes.White:col>=5?muted:ink,new RectangleF(x,y,cw,18),centered);
                if(!today&&SelectedDate.HasValue&&SelectedDate.Value==date)using(Pen p=new Pen(Color.FromArgb(226,73,70),1.5f))g.DrawLine(p,x+cw/2-8,y+17,x+cw/2+8,y+17);
            }
            centered.Dispose();
        }
    }
    protected override void OnMouseClick(MouseEventArgs e){base.OnMouseClick(e);
        if(e.Y<24&&e.X>Width-88){if(e.X<Width-63)Month=Month.AddMonths(-1);else if(e.X<Width-35)Month=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1);else Month=Month.AddMonths(1);Invalidate();return;}
        if(e.Y>=54&&e.X>=12&&e.X<Width-12){int col=(int)((e.X-12)/((Width-24)/7f)),row=(e.Y-54)/18,day=row*7+col-FirstColumn(Month)+1;if(day>=1&&day<=DateTime.DaysInMonth(Month.Year,Month.Month)){SelectedDate=Month.AddDays(day-1);Invalidate();}}
    }
}
public class NoteEditor : Form {
    readonly MixedRichTextBox box; readonly Label status; readonly System.Windows.Forms.Timer timer;
    readonly Func<string,bool> persist; bool dirty; bool closing;
    public NoteEditor(string text, Rectangle rect, Func<string,bool> save) {
        persist=save;Text="编辑便签 · Esc 完成";StartPosition=FormStartPosition.Manual;
        Icon=AppIcons.Main;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;MinimumSize=new Size(280,180);
        Font=Typography.Chinese(14,FontStyle.Regular);BackColor=BlueTheme.Paper;Padding=new Padding(14,56,14,38);KeyPreview=true;Location=rect.Location;ClientSize=new Size(rect.Width,rect.Height+56);
        status=new MixedLabel { Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom,Location=new Point(20,ClientSize.Height-30),Size=new Size(ClientSize.Width-40,22),Font=Typography.Chinese(12,FontStyle.Regular),Text="",Visible=false,ForeColor=Color.FromArgb(190,66,66) };
        box=new MixedRichTextBox { Dock=DockStyle.Fill,Multiline=true,ScrollBars=RichTextBoxScrollBars.Vertical,BorderStyle=BorderStyle.None,Font=new Font("Microsoft YaHei UI",17f,FontStyle.Regular,GraphicsUnit.Pixel),Text=text };
        Panel writing=new EditorPaper{Dock=DockStyle.Fill,Padding=new Padding(16),BackColor=BlueTheme.Paper};
        box.BackColor=BlueTheme.Surface;box.ForeColor=BlueTheme.Ink;writing.Controls.Add(box);Controls.Add(writing);Controls.Add(status);
        Label title=new MixedLabel{Text="编辑便签",Location=new Point(20,18),Size=new Size(200,25),Font=Typography.Chinese(18,FontStyle.Regular),ForeColor=BlueTheme.Ink};
        TextChanged+=delegate{string[] parts=Text.Split(new string[]{" · "},StringSplitOptions.None);title.Text=parts.Length>1?parts[1]:"编辑便签";};
        Button done=new EditorDoneButton{Text="完成",Anchor=AnchorStyles.Top|AnchorStyles.Right,Location=new Point(ClientSize.Width-74,12),Size=new Size(56,32),Font=Typography.Chinese(15,FontStyle.Regular),Cursor=Cursors.Hand,TabStop=false};
        done.Click+=delegate{Close();};Controls.Add(title);Controls.Add(done);title.BringToFront();done.BringToFront();
        Point dragOrigin=Point.Empty,windowOrigin=Point.Empty;bool moving=false;
        title.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){moving=true;dragOrigin=Cursor.Position;windowOrigin=Location;title.Capture=true;}};
        title.MouseMove+=delegate{if(moving)Location=new Point(windowOrigin.X+Cursor.Position.X-dragOrigin.X,windowOrigin.Y+Cursor.Position.Y-dragOrigin.Y);};
        title.MouseUp+=delegate{moving=false;title.Capture=false;};
        SizeChanged+=delegate{using(var shape=PaperShapes.Rounded(new RectangleF(0,0,Width,Height),20)){Region previous=Region;Region=new Region(shape);if(previous!=null)previous.Dispose();}};
        using(var shape=PaperShapes.Rounded(new RectangleF(0,0,Width,Height),20))Region=new Region(shape);
        timer=new System.Windows.Forms.Timer { Interval=1000 };
        timer.Tick+=delegate { timer.Stop();if(box.IsImeComposing){timer.Start();return;}Save(); };
        box.TextChanged+=delegate { dirty=true;timer.Stop();timer.Start(); };
        box.CompositionEnded+=delegate{if(dirty){timer.Stop();timer.Start();}};
        Shown+=delegate { box.Focus();box.SelectionStart=box.TextLength; };
        KeyDown+=delegate(object sender,KeyEventArgs e) { if(e.KeyCode==Keys.Escape&&!box.IsImeComposing) { e.SuppressKeyPress=true;Close(); } };
        Deactivate+=delegate { if(!closing&&dirty){timer.Stop();timer.Start();} };
        FormClosing+=delegate(object sender,FormClosingEventArgs e) { if(box.IsImeComposing){e.Cancel=true;return;}closing=true;timer.Stop();if(!Save()) { e.Cancel=true;closing=false; } };
        FormClosed+=delegate { timer.Dispose(); };
    }
    public void SaveEditorPreview(string path){using(Bitmap image=new Bitmap(Width,Height)){DrawToBitmap(image,ClientRectangle);Point pos=PointToClient(box.PointToScreen(Point.Empty));using(Graphics g=Graphics.FromImage(image))using(SolidBrush ink=new SolidBrush(box.ForeColor))Typography.Draw(g,box.Text,box.Font,ink,new RectangleF(pos.X+1,pos.Y+1,box.Width-2,box.Height-2),null,true,1.2f);image.Save(path,System.Drawing.Imaging.ImageFormat.Png);}}
    public static void CheckEditor(string report){bool allow=false;string captured=null;using(NoteEditor form=new NoteEditor("原始记录",new Rectangle(0,0,344,520),delegate(string value){captured=value;return allow;})){form.Show();Application.DoEvents();form.box.Text="修改后的记录 / Test 123";form.Close();if(form.IsDisposed||captured!=form.box.Text)throw new Exception("Failed save did not retain editor");allow=true;form.Close();if(!form.IsDisposed||captured!="修改后的记录 / Test 123")throw new Exception("Final save failed");}File.WriteAllText(report,"PASS: failed save retains editor and text; successful close persists exact text.",Encoding.UTF8);}
    [System.Runtime.InteropServices.DllImport("user32.dll",EntryPoint="SendMessageW")]static extern IntPtr SendImeMessage(IntPtr handle,int message,IntPtr w,IntPtr l);
    public static void CheckImeEditing(string report){int saves=0;string stored=null;using(NoteEditor form=new NoteEditor("原始记录",new Rectangle(0,0,344,520),delegate(string text){saves++;stored=text;return true;})){form.Show();Application.DoEvents();SendImeMessage(form.box.Handle,0x10d,IntPtr.Zero,IntPtr.Zero);form.box.Text="pin yin";if(!form.box.IsImeComposing||form.Save()||saves!=0)throw new Exception("IME composition was saved");using(Form other=new Form()){other.Show();other.Activate();Application.DoEvents();if(form.IsDisposed)throw new Exception("Focus loss closed editor");other.Close();}form.Close();if(form.IsDisposed)throw new Exception("Composition close lost input");form.box.Text="拼音输入完成 / Test 123";SendImeMessage(form.box.Handle,0x10e,IntPtr.Zero,IntPtr.Zero);Application.DoEvents();DateTime deadline=DateTime.Now.AddSeconds(3);while(saves==0&&DateTime.Now<deadline){Application.DoEvents();System.Threading.Thread.Sleep(20);}if(saves!=1||stored!="拼音输入完成 / Test 123"||form.status.Visible||form.IsDisposed)throw new Exception("Quiet autosave failed");form.Close();}File.WriteAllText(report,"PASS: IME start defers save and close; focus loss keeps editor; IME end triggers silent idle save; committed text preserved. Actual Pinyin candidate-window behavior still needs user testing.",Encoding.UTF8);}
    bool Save() {
        if(box.IsImeComposing)return false;
        if(!dirty) return true;
        if(persist(box.Text)) {dirty=false;status.Text="";status.Visible=false;return true;}
        status.Text="保存失败，内容仍保留，请稍后重试。";status.Visible=true;return false;
    }
}
public class EditorPaper:Panel {
    public EditorPaper(){DoubleBuffered=true;}
    protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var shape=PaperShapes.Rounded(new RectangleF(.5f,.5f,Width-1,Height-1),15))using(var fill=new SolidBrush(BlueTheme.Surface))using(var border=new Pen(BlueTheme.Border)){e.Graphics.FillPath(fill,shape);e.Graphics.DrawPath(border,shape);}}
}
public class EditorDoneButton:Button {
    public EditorDoneButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BlueTheme.Paper);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var shape=PaperShapes.Rounded(new RectangleF(0,0,Width-1,Height-1),12))using(var fill=new SolidBrush(BlueTheme.Soft))using(var ink=new SolidBrush(BlueTheme.Accent))using(var sf=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center}){e.Graphics.FillPath(fill,shape);Typography.Draw(e.Graphics,Text,Font,ink,new RectangleF(0,0,Width,Height),sf,false);}}
}
public class SideTab : Button {
    public bool Selected;
    public SideTab() {Size=new Size(60,37);Margin=new Padding(0,0,0,7);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;TabStop=false;}
    protected override void OnPaint(PaintEventArgs e) {
        using(System.Drawing.Drawing2D.LinearGradientBrush background=new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0,-(Top+(Parent==null?0:Parent.Top)),Width,Parent==null||Parent.Parent==null?740:Parent.Parent.Height),Color.FromArgb(217,232,245),Color.FromArgb(196,215,233),90))e.Graphics.FillRectangle(background,ClientRectangle);
        e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using(System.Drawing.Drawing2D.GraphicsPath shape=PaperShapes.Rounded(new RectangleF(.5f,.5f,Width-1,Height-1),7))using(SolidBrush fill=new SolidBrush(Selected?BlueTheme.Soft:BlueTheme.Surface))using(Pen border=new Pen(Selected?Color.FromArgb(182,207,234):BlueTheme.Border)){
            e.Graphics.FillPath(fill,shape);e.Graphics.DrawPath(border,shape);
        }
        if(Selected)using(Pen accent=new Pen(BlueTheme.Accent,3))e.Graphics.DrawLine(accent,2,8,2,Height-8);
        using(SolidBrush brush=new SolidBrush(Selected?BlueTheme.Accent:BlueTheme.Muted))using(StringFormat sf=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})Typography.Draw(e.Graphics,Text,Font,brush,new RectangleF(Width<50?3:7,0,Width-(Width<50?6:13),Height-3),sf,false);
    }
}
public class PanelForm : Form {
    readonly IWorkspaceRepository store; readonly NoteState state; readonly bool preview;
    readonly Label note,goal,caption,heading; readonly CalendarView calendar;
    readonly FlowLayoutPanel tabs; readonly Button addTab,foldButton,appearanceButton;
    readonly Button personalButton;Action refreshPositionMenu;
    readonly NotifyIcon tray; readonly System.Windows.Forms.Timer clock;
    readonly PaperLayer paperLayer;
    IntPtr desktop; bool unlocked, dragging; Point dragStart, origin; NoteEditor editor;
    public string SaveError="";
    protected override bool ShowWithoutActivation { get { return true; } }
    public PanelForm(IWorkspaceRepository data, NoteState initial, bool isPreview) {
        store=data;state=initial;state.EnsureCategories();preview=isPreview;
        Text="进步签 · 技术样例";Icon=AppIcons.Main;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;
        AutoScaleMode=AutoScaleMode.None;ClientSize=new Size(464,818);BackColor=BlueTheme.Paper;
        Font=new Font("Microsoft YaHei UI",13.333f,FontStyle.Regular,GraphicsUnit.Pixel);StartPosition=FormStartPosition.Manual;
        heading=new MixedLabel { Text="无限进步才能掌握主动",Location=new Point(92,30),Size=new Size(260,22),Font=Typography.Chinese(15,FontStyle.Regular),ForeColor=BlueTheme.Ink };
        calendar=new CalendarView { Location=new Point(88,64),Size=new Size(344,146) };
        goal=new PaperCountdownLabel {Location=new Point(88,216),Size=new Size(344,63),BackColor=BlueTheme.Paper,Padding=new Padding(8,3,8,0),AutoEllipsis=true,ForeColor=BlueTheme.Ink};
        caption=new MixedLabel {Visible=false,Text="随手记",Location=new Point(20,348),Size=new Size(320,25),ForeColor=BlueTheme.Muted};
        note=new PaperRecordLabel {Font=Typography.Chinese(17f,FontStyle.Regular),Text=state.ActiveCategory.Content,Location=new Point(88,279),Size=new Size(344,520),BackColor=BlueTheme.Surface,Padding=new Padding(12),Cursor=Cursors.IBeam,AutoEllipsis=true};
        Label hint=new MixedLabel {Text="双击便签编辑 · 右键打开菜单",Location=new Point(20,620),Size=new Size(320,24),ForeColor=BlueTheme.Muted,Font=new Font("Microsoft YaHei UI",12f,FontStyle.Regular,GraphicsUnit.Pixel)};
        hint.Visible=false;Controls.AddRange(new Control[]{heading,calendar,goal,caption,note,hint});
        tabs=new SideRailPanel {Location=new Point(15,279),Size=new Size(61,452),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=BlueTheme.Outer};
        addTab=new AddCategoryButton {Text="新增分类",Location=new Point(22,753),Size=new Size(34,34),FlatStyle=FlatStyle.Flat,ForeColor=BlueTheme.Ink};
        addTab.Click+=delegate {PromptCategory(null);};
        Controls.Add(tabs);Controls.Add(addTab);RenderTabs();
        foldButton=new TopIconButton {Text="收起 ∧",Location=new Point(404,25),Size=new Size(27,27),FlatStyle=FlatStyle.Flat,BackColor=BackColor,ForeColor=BlueTheme.Ink,Font=new Font("Microsoft YaHei UI",12f,FontStyle.Regular,GraphicsUnit.Pixel),TabStop=false};
                appearanceButton=new TopIconButton {OpacityIcon=true,Text="透明度",Location=new Point(374,25),Size=new Size(27,27),FlatStyle=FlatStyle.Flat,BackColor=BackColor,ForeColor=BlueTheme.Ink,Font=new Font("Microsoft YaHei UI",12,FontStyle.Regular,GraphicsUnit.Pixel),TabStop=false};
        appearanceButton.FlatAppearance.BorderSize=0;appearanceButton.Click+=delegate{ShowTransparencySettings();};Controls.Add(appearanceButton);
        foldButton.FlatAppearance.BorderSize=0;foldButton.Click+=delegate {SetCollapsed(!state.Collapsed);};Controls.Add(foldButton);foldButton.BringToFront();
        personalButton=new PersonalAvatarButton{Location=new Point(22,25),Size=new Size(34,34),AccessibleName="个人中心",Cursor=Cursors.Hand,TabStop=true};personalButton.Click+=delegate{ShowPersonalCenter();};Controls.Add(personalButton);
        note.DoubleClick+=delegate { EditNote(); };
        goal.DoubleClick+=delegate { EditGoal(); };
        heading.MouseDown+=delegate(object s,MouseEventArgs e) { if(unlocked&&e.Button==MouseButtons.Left) {dragging=true;dragStart=Cursor.Position;origin=new Point(state.X,state.Y);heading.Capture=true;} };
        heading.MouseMove+=delegate { if(dragging) { state.X=origin.X+Cursor.Position.X-dragStart.X;state.Y=origin.Y+Cursor.Position.Y-dragStart.Y;Place(); } };
        heading.MouseUp+=delegate { if(dragging) {dragging=false;heading.Capture=false;Clamp();Place();Persist();} };
        ContextMenuStrip menu=new ContextMenuStrip{Renderer=new MixedMenuRenderer(),Font=Typography.Chinese(13.333f,FontStyle.Regular)};
        menu.Items.Add("个人中心",null,delegate{ShowPersonalCenter();});
        menu.Items.Add("编辑便签",null,delegate { EditNote(); });
        menu.Items.Add("设置倒计时",null,delegate { EditGoal(); });
        menu.Items.Add("新增分类",null,delegate {PromptCategory(null);});
        menu.Items.Add("重命名当前分类",null,delegate {PromptCategory(state.ActiveCategory);});
        ToolStripMenuItem move=new ToolStripMenuItem("解锁位置（拖动标题）");
        refreshPositionMenu=delegate{move.Text=unlocked?"锁定位置":"解锁位置（拖动标题）";heading.Text="无限进步才能掌握主动";};
        move.Click+=delegate {SetPositionUnlocked(!unlocked);};
        menu.Items.Add(move);
        menu.Items.Add("返回主屏右侧",null,delegate {DefaultPosition();Place();Persist();});
        menu.Items.Add("打开数据文件夹",null,delegate { Directory.CreateDirectory(store.Folder);System.Diagnostics.Process.Start("explorer.exe",store.Folder); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出",null,delegate { Close(); });
        ContextMenuStrip=menu;
        foreach(Control c in Controls)c.ContextMenuStrip=menu;
        tray=new NotifyIcon {Icon=AppIcons.Tray,Text="进步签 · 双击编辑便签",ContextMenuStrip=menu,Visible=!preview};
        tray.DoubleClick+=delegate { EditNote(); };
        clock=new System.Windows.Forms.Timer {Interval=15000};clock.Tick+=delegate {UpdateGoal();calendar.Invalidate();};
        if(!preview)clock.Start();
        paperLayer=new PaperLayer(this,state.Device.BackgroundTransparencyPercent,delegate{return new Size(464,state.Collapsed?62:818);});
        FormClosing+=delegate(object sender,FormClosingEventArgs e) {if(editor!=null&&!editor.IsDisposed) {editor.Close();if(!editor.IsDisposed) {e.Cancel=true;return;}} if(!preview&&!Persist()) e.Cancel=true;};
        FormClosed+=delegate {paperLayer.Dispose();clock.Dispose();tray.Visible=false;tray.Dispose();};
        ApplyCollapsedLayout();if(state.X==-9999)DefaultPosition();Clamp();UpdateGoal();
    }
    protected override void OnPaintBackground(PaintEventArgs e){
        e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using(System.Drawing.Drawing2D.LinearGradientBrush outer=new System.Drawing.Drawing2D.LinearGradientBrush(ClientRectangle,Color.FromArgb(217,232,245),Color.FromArgb(196,215,233),90))e.Graphics.FillRectangle(outer,ClientRectangle);
        float y=state.Collapsed?12:19,h=state.Collapsed?38:780;
        using(System.Drawing.Drawing2D.GraphicsPath body=PaperShapes.Rounded(new RectangleF(76.5f,y+.5f,367,h-1),24))using(SolidBrush b=new SolidBrush(BlueTheme.Paper))using(Pen p=new Pen(Color.White)){e.Graphics.FillPath(b,body);e.Graphics.DrawPath(p,body);}
    }
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);
        if(!state.Collapsed)using(Pen p=new Pen(BlueTheme.Border))e.Graphics.DrawLine(p,77,57,443,57);
    }
    public bool SetBackgroundTransparency(int value) {
        if(value<0||value>99)throw new ArgumentOutOfRangeException("value");
        int previous=state.Device.BackgroundTransparencyPercent;state.Device.BackgroundTransparencyPercent=value;
        if(!Persist()){state.Device.BackgroundTransparencyPercent=previous;paperLayer.SetTransparency(previous);return false;}
        paperLayer.SetTransparency(value);return true;
    }
    void SetPositionUnlocked(bool value){unlocked=value;if(refreshPositionMenu!=null)refreshPositionMenu();}
    void ShowTransparencySettings(){ShowPersonalCenter();}
    void ShowPersonalCenter(){
        if(!FinishEditor())return;
        using(PersonalCenter f=new PersonalCenter(state.Device.BackgroundTransparencyPercent,delegate{return unlocked;},delegate(bool value){SetPositionUnlocked(value);},delegate(int value){if(!SetBackgroundTransparency(value))throw new Exception(SaveError);return true;},delegate(int value){paperLayer.SetTransparency(value);},delegate{int x=state.X,y=state.Y;DefaultPosition();Clamp();Place();if(!Persist()){state.X=x;state.Y=y;Place();throw new Exception(SaveError);}},delegate{Directory.CreateDirectory(store.Folder);System.Diagnostics.Process.Start("explorer.exe",store.Folder);}))f.ShowDialog();
    }
    public bool SetCollapsed(bool collapsed) {
        if(collapsed==state.Collapsed)return true;
        if(!FinishEditor())return false;
        bool old=state.Collapsed;state.Collapsed=collapsed;
        if(!Persist()){state.Collapsed=old;return false;}
        ApplyCollapsedLayout();Clamp();if(IsHandleCreated)Place();return true;
    }
    void ApplyCollapsedLayout() {
        SuspendLayout();
        calendar.Visible=goal.Visible=note.Visible=tabs.Visible=addTab.Visible=!state.Collapsed;
        ClientSize=new Size(464,state.Collapsed?62:818);
        personalButton.Top=state.Collapsed?14:25;heading.Top=state.Collapsed?22:30;appearanceButton.Top=foldButton.Top=state.Collapsed?17:25;
        foldButton.Text=state.Collapsed?"展开 ∨":"收起 ∧";foldButton.BringToFront();ResumeLayout();
    }
    void DefaultPosition() {Rectangle r=Screen.PrimaryScreen.WorkingArea;state.X=r.Right-Width-24;state.Y=r.Top+24;}
    void Clamp() {
        Rectangle desired=new Rectangle(state.X,state.Y,Width,818);Rectangle r=Screen.FromRectangle(desired).WorkingArea;
        state.X=Math.Max(r.Left,Math.Min(state.X,r.Right-Width));state.Y=Math.Max(r.Top,Math.Min(state.Y,r.Bottom-818));
    }
    public bool Persist() {
        try {store.Save(state);SaveError="";return true;}catch(Exception e) {SaveError=e.Message;heading.Text="保存失败："+e.Message;return false;}
    }
    public static string Countdown(string target,DateTime today) {
        if(String.IsNullOrEmpty(target))return "双击设置目标日期";
        DateTime d=DateTime.ParseExact(target,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
        int days=(d.Date-today.Date).Days;
        return days>0?"还有 "+days+" 天":days==0?"就是今天":"已过去 "+(-days)+" 天";
    }
    void UpdateGoal() {goal.Text=(state.Goal.Length==0?"目标倒计时":state.Goal)+" · "+(String.IsNullOrEmpty(state.Target)?"点击设置 ›":Countdown(state.Target,DateTime.Today));}
    protected override void OnShown(EventArgs e) {
        base.OnShown(e);ApplyCollapsedLayout();if(preview)return;
        desktop=Native.DesktopView();
        if(desktop==IntPtr.Zero)throw new InvalidOperationException("未找到 Explorer 桌面视图，无法嵌入。请确认资源管理器正在运行。");
        int style=Native.GetWindowLong(Handle,-16);
        Native.SetWindowLong(Handle,-16,(style&~unchecked((int)0x80000000))|0x40000000);
        Native.SetParent(Handle,desktop);
        if(Native.GetParent(Handle)!=desktop)throw new InvalidOperationException("嵌入桌面失败，错误码："+Marshal.GetLastWin32Error());
        Place();Persist();paperLayer.Start();
    }
    void Place() {
        Native.POINT p=new Native.POINT(state.X,state.Y);
        if(desktop!=IntPtr.Zero)Native.ScreenToClient(desktop,ref p);
        Native.SetWindowPos(Handle,IntPtr.Zero,p.X,p.Y,464,state.Collapsed?62:818,0x0010|0x0040);
    }
    void EditNote() {
        if(state.Collapsed&&!SetCollapsed(false))return;
        if(editor!=null&&!editor.IsDisposed) {editor.Activate();return;}
        Rectangle area=note.RectangleToScreen(note.ClientRectangle);
        // Keep the actual editor textbox as tall as the 2/3 display area.
        Rectangle work=Screen.FromRectangle(area).WorkingArea;
        area.Y=Math.Max(work.Top,Math.Min(area.Y,work.Bottom-area.Height-90));
        NoteCategory editing=state.ActiveCategory;
        editor=new NoteEditor(editing.Content,area,delegate(string value) {editing.Content=value;if(state.ActiveId==editing.Id)note.Text=value;bool ok=Persist();if(ok)caption.Text=editing.Name+" · 已保存";return ok;});
        editor.Text="编辑 · "+editing.Name+" · Esc 完成";
        editor.Show();editor.Activate();
    }
    bool FinishEditor() {
        if(editor!=null&&!editor.IsDisposed) {editor.Close();if(!editor.IsDisposed){editor.Activate();return false;}}
        return true;
    }
    public bool SelectCategory(string id) {
        NoteCategory next=state.Categories.Find(delegate(NoteCategory c){return c.Id==id;});
        if(next==null)return false;
        if(!FinishEditor())return false;
        string previous=state.ActiveId;state.ActiveId=id;
        if(!Persist()){state.ActiveId=previous;return false;}
        note.Text=next.Content;RenderTabs();return true;
    }
    public bool AddCategory(string name) {
        if(!FinishEditor())return false;
        name=ValidateCategoryName(name,null);string previous=state.ActiveId;
        NoteCategory c=new NoteCategory(name,"");state.Categories.Add(c);state.ActiveId=c.Id;
        if(!Persist()){state.Categories.Remove(c);state.ActiveId=previous;return false;}
        note.Text=c.Content;RenderTabs();return true;
    }
    public bool RenameCategory(string id,string name) {
        if(!FinishEditor())return false;
        NoteCategory c=state.Categories.Find(delegate(NoteCategory item){return item.Id==id;});
        if(c==null)return false;
        name=ValidateCategoryName(name,id);string old=c.Name;c.Name=name;
        if(!Persist()){c.Name=old;return false;}RenderTabs();return true;
    }
    string ValidateCategoryName(string name,string exceptId) {
        name=(name??"").Trim();if(name.Length==0||name.Length>12)throw new ArgumentException("分类名称请填写 1–12 个字符。");
        foreach(NoteCategory c in state.Categories)if(c.Id!=exceptId&&String.Equals(c.Name,name,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("已有同名分类，请换一个名称。");
        return name;
    }
    void RenderTabs() {
        ((PaperRecordLabel)note).CategoryName=state.ActiveCategory.Name;
        tabs.SuspendLayout();
        while(tabs.Controls.Count>0){Control old=tabs.Controls[0];tabs.Controls.Remove(old);old.Dispose();}
        foreach(NoteCategory category in state.Categories) {
            NoteCategory captured=category;
            SideTab button=new SideTab {Text=category.Name,Selected=category.Id==state.ActiveId,Font=new Font("Microsoft YaHei UI",12f,FontStyle.Regular,GraphicsUnit.Pixel)};
            button.Click+=delegate {SelectCategory(captured.Id);};
            ContextMenuStrip actions=new ContextMenuStrip{Renderer=new MixedMenuRenderer(),Font=Typography.Chinese(13.333f,FontStyle.Regular)};
            actions.Items.Add("重命名“"+category.Name+"”",null,delegate {PromptCategory(captured);});
            button.ContextMenuStrip=actions;button.Disposed+=delegate {actions.Dispose();};tabs.Controls.Add(button);
        }
        tabs.ResumeLayout();
    }
    void PromptCategory(NoteCategory category) {
        if(!FinishEditor())return;
        using(SettingsSheet f=SettingsSheet.Category(category==null?"新增分类":"重命名分类",category==null?"":category.Name,delegate(string name){bool saved=category==null?AddCategory(name):RenameCategory(category.Id,name);if(!saved)throw new Exception(SaveError);return saved;}))f.ShowDialog();
    }
    void EditGoal() {
        if(!FinishEditor())return;
        using(SettingsSheet f=SettingsSheet.Countdown(state.Goal,state.Target,delegate(string name,DateTime date){string oldGoal=state.Goal,oldTarget=state.Target;state.Goal=name.Trim();state.Target=date.ToString("yyyy-MM-dd");if(Persist()){UpdateGoal();return true;}state.Goal=oldGoal;state.Target=oldTarget;throw new Exception(SaveError);}))f.ShowDialog();
    }
    public void SavePaperPreview(string path) {
        using(Bitmap source=new Bitmap(Width,Height)){DrawToBitmap(source,ClientRectangle);using(Bitmap frame=PaperBitmap.Convert(source,state.Device.BackgroundTransparencyPercent))frame.Save(path,System.Drawing.Imaging.ImageFormat.Png);}
    }
    public string Diagnostics() {
        if(calendar.Left!=goal.Left||goal.Left!=note.Left||calendar.Width!=goal.Width||goal.Width!=note.Width)throw new Exception("Section bounds differ");
        return "time="+DateTime.Now.ToString("o")+"\r\nhandle="+Handle+"\r\ndesktop="+desktop+"\r\nparent="+Native.GetParent(Handle)+"\r\nembedded="+(desktop!=IntPtr.Zero&&Native.GetParent(Handle)==desktop)+"\r\nsize="+Width+"x"+Height+"\r\nnoteHeight="+note.Height+"\r\nnoteRatio="+((double)note.Height/780).ToString("F4")+"\r\nbackgroundTransparency="+state.Device.BackgroundTransparencyPercent+"\r\ntextAlpha=255\r\nlayerPresented="+paperLayer.LastPresented+"\r\nlayerFrames="+paperLayer.FrameCount+"\r\nsaveError="+SaveError;
    }
}
public static class Program {
    [STAThread] public static void Main(string[] args) {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        try {
            if(args.Length>=2&&args[0]=="--self-test") {SelfTest(args[1]);return;}
            if(args.Length>=2&&args[0]=="--editor-self-test"){NoteEditor.CheckEditor(args[1]);return;}
            if(args.Length>=2&&args[0]=="--editor-preview") {using(NoteEditor editor=new NoteEditor("1、自我成长\n2、记录今日想法\n3、整理阅读笔记",new Rectangle(0,0,344,520),delegate{return true;})){editor.Show();Application.DoEvents();editor.SaveEditorPreview(args[1]);editor.Close();}return;}
            if(args.Length>=2&&args[0]=="--settings-preview"){SettingsSheet.PreviewAndCheck(args[1]);return;}
            if(args.Length>=2&&args[0]=="--personal-preview"){PersonalCenter.PreviewAndCheck(args[1]);return;}
            if(args.Length>=2&&args[0]=="--transparency-test"){TransparencyChecks.Run(args[1]);return;}
            if(args.Length>=2&&args[0]=="--ime-test"){NoteEditor.CheckImeEditing(args[1]);return;}
            if(args.Length>=2&&args[0]=="--shared-data-test"){SharedDataTests.Run(args[1]);return;}
            if(args.Length>=2&&args[0]=="--autostart-test"){AutoStartTests.Run(args[1]);return;}
            string root=AppDomain.CurrentDomain.BaseDirectory;
            bool preview=args.Length>=2&&args[0]=="--preview";
            bool smoke=args.Length>=2&&args[0]=="--smoke";
            bool migrateOnly=args.Length>=1&&args[0]=="--migrate-data";
            string importSource=args.Length==2&&args[0]=="--import-data"?Path.GetFullPath(args[1]):null;
            LocalJsonRepository store=new LocalJsonRepository(preview||smoke?Path.Combine(root,"test-data"):SharedData.Folder);
            bool created;using(Mutex singleton=new Mutex(true,"Local\\DesktopNotePrototype-719624",out created)) {
                if(!created&&!preview) {if(args.Length==0)MessageBox.Show("进步签已经运行，请先退出旧版。","进步签");Environment.ExitCode=2;return;}
                using(FileStream writeLease=preview?null:SharedData.OpenWriterLock(store.Folder)) {
                if(!preview&&!smoke){if(importSource!=null&&(File.Exists(store.PathName)||File.Exists(store.BackupPath)))throw new IOException("已有本机数据，已拒绝覆盖导入。请先在程序关闭时备份并另存现有目录。");SharedData.Prepare(root,store,importSource);}
                if(migrateOnly||importSource!=null)return;
                if(!preview&&!smoke){AutoStart.RefreshForCurrentVersion();if(Array.IndexOf(args,"--autostart")>=0)AutoStart.WaitForShell(delegate{return Native.DesktopView()!=IntPtr.Zero;},delegate{System.Threading.Thread.Sleep(1000);});}
                PanelForm panel=new PanelForm(store,preview?new NoteState{Device=new DeviceSettings{BackgroundTransparencyPercent=80},Goal="提交论文",Target=DateTime.Today.AddDays(6).ToString("yyyy-MM-dd"),Note="把重要的事，留在眼前。\n在这里记录想法、片段与备忘。"}:store.Load(),preview);
                if(preview) {
                    if(args.Length>=3) foreach(Control c in panel.Controls) if(c is CalendarView) ((CalendarView)c).Month=DateTime.ParseExact(args[2],"yyyy-MM",System.Globalization.CultureInfo.InvariantCulture);panel.Show();Application.DoEvents();File.WriteAllText(args[1]+".layout.txt",panel.Diagnostics(),Encoding.UTF8);panel.SavePaperPreview(args[1]);panel.Dispose();return;
                }
                if(smoke) {
                    System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer {Interval=3000};
                    panel.Shown+=delegate {timer.Start();};
                    timer.Tick+=delegate {timer.Stop();File.WriteAllText(args[1],panel.Diagnostics(),Encoding.UTF8);panel.Close();timer.Dispose();};
                }
                Application.Run(panel);
                }
            }
        }catch(Exception e) {
            string log=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"startup-error.txt");
            try {File.WriteAllText(log,e.ToString(),Encoding.UTF8);}catch{}
            if(args.Length==0)MessageBox.Show(e.Message,"进步签启动失败");
            Environment.ExitCode=1;
        }
    }
    static void Assert(bool condition,string message) {if(!condition)throw new Exception("FAIL: "+message);}
    static void SelfTest(string folder) {
        Directory.CreateDirectory(folder);LocalJsonRepository s=new LocalJsonRepository(Path.Combine(folder,"case-"+Guid.NewGuid().ToString("N")));
        Assert(s.Load().Note.Length>0,"default");
        NoteState state=new NoteState {Note="中文输入\r\n第二行 😀",Goal="测试目标",Target="2026-10-26",X=123,Y=456};
        s.Save(state);NoteState loaded=s.Load();Assert(loaded.Note==state.Note&&loaded.X==123&&loaded.Target==state.Target,"JSON roundtrip");
        state.Note="第二版";s.Save(state);Assert(File.Exists(s.BackupPath),"backup exists");
        File.WriteAllText(s.PathName,"broken");Assert(s.Load().Note==loaded.Note,"backup recovery");s.Save(loaded);Assert(s.Load().Note==loaded.Note,"save after recovery");
        Assert(PanelForm.Countdown("2026-10-26",new DateTime(2026,10,25))=="还有 1 天","tomorrow");
        Assert(PanelForm.Countdown("2026-10-25",new DateTime(2026,10,25))=="就是今天","today");
        Assert(PanelForm.Countdown("2026-10-24",new DateTime(2026,10,25))=="已过去 1 天","past");
        Assert(PanelForm.Countdown("2027-01-01",new DateTime(2026,12,31))=="还有 1 天","year boundary");
        Assert(PanelForm.Countdown("2028-03-01",new DateTime(2028,2,28))=="还有 2 天","leap year");
        Assert(PanelForm.Countdown("",DateTime.Today)=="双击设置目标日期","unset");
        File.WriteAllText(Path.Combine(folder,"self-test-result.txt"),"PASS: JSON roundtrip, backup recovery, save after recovery, countdown tomorrow/today/past/year/leap/unset.",Encoding.UTF8);
    }
}
