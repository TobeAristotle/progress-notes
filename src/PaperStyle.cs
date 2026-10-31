using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Windows.Forms;
public static class PaperShapes {
    public static GraphicsPath Rounded(RectangleF r,float radius){GraphicsPath p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
    public static bool Inside(float x,float y,float left,float top,float right,float bottom,float radius){if(x<left||x>=right||y<top||y>=bottom)return false;float cx=x<left+radius?left+radius:x>right-radius?right-radius:x;float cy=y<top+radius?top+radius:y>bottom-radius?bottom-radius:y;return (x-cx)*(x-cx)+(y-cy)*(y-cy)<=radius*radius;}
}
public class PaperRecordLabel:SelectableNoteLabel {
    string category="随手记";public string CategoryName{get{return category;}set{if(category!=value)ResetView();category=value;Invalidate();}}
    protected override void OnPaint(PaintEventArgs e){Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(BlueTheme.Paper);
        using(GraphicsPath card=PaperShapes.Rounded(new RectangleF(.5f,.5f,Width-1,Height-13),16))using(SolidBrush fill=new SolidBrush(BlueTheme.Surface))using(Pen border=new Pen(BlueTheme.Border)){g.FillPath(fill,card);g.DrawPath(border,card);}
        using(Font title=Typography.Chinese(20,FontStyle.Regular))using(SolidBrush ink=new SolidBrush(BlueTheme.Ink))Typography.DrawString(g,CategoryName,title,ink,16,18);
        using(Pen line=new Pen(BlueTheme.Border))g.DrawLine(line,16,52,Width-16,52);
        PaintNoteContent(g);
        using(Pen pen=new Pen(BlueTheme.Muted,1)){g.DrawLine(pen,Width-29,24,Width-22,17);g.DrawLine(pen,Width-26,27,Width-19,20);g.DrawLine(pen,Width-29,24,Width-30,28);g.DrawLine(pen,Width-30,28,Width-26,27);g.DrawLine(pen,Width-22,17,Width-19,20);}
    }
}
public class PaperCountdownLabel:MixedLabel {
    protected override void OnPaint(PaintEventArgs e){
        Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(BlueTheme.Paper);
        using(GraphicsPath shape=PaperShapes.Rounded(new RectangleF(.5f,.5f,Width-1,Height-1),15))using(SolidBrush fill=new SolidBrush(BlueTheme.Soft))using(Pen border=new Pen(BlueTheme.Border)){g.FillPath(fill,shape);g.DrawPath(border,shape);}
        int separator=Text.LastIndexOf(" · ",StringComparison.Ordinal);string title=separator<0?Text:Text.Substring(0,separator),count=separator<0?"":Text.Substring(separator+3);
        var match=System.Text.RegularExpressions.Regex.Match(count,@"^(还有|已过去) (\d+) 天$");
        using(SolidBrush ink=new SolidBrush(BlueTheme.Ink))using(SolidBrush accent=new SolidBrush(BlueTheme.Accent))using(Font titleFont=Typography.Chinese(20,FontStyle.Regular))using(Font small=Typography.Chinese(14,FontStyle.Regular))using(Font number=Typography.Latin(30,FontStyle.Regular))using(StringFormat left=new StringFormat{LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap})using(StringFormat right=new StringFormat{Alignment=StringAlignment.Far,LineAlignment=StringAlignment.Center}){
            float countWidth=match.Success?g.MeasureString(match.Groups[1].Value,small).Width+g.MeasureString(match.Groups[2].Value,number).Width+g.MeasureString("天",small).Width+12:Math.Min(165,g.MeasureString(count,small).Width);
            Typography.Draw(g,title,titleFont,ink,new RectangleF(18,0,Math.Max(20,Width-48-countWidth),Height),left,false);
            if(match.Success){float x=Width-18-countWidth;Typography.Draw(g,match.Groups[1].Value,small,accent,new RectangleF(x,4,44,Height),left,false);x+=g.MeasureString(match.Groups[1].Value,small).Width+5;float nw=g.MeasureString(match.Groups[2].Value,number).Width;Typography.Draw(g,match.Groups[2].Value,number,accent,new RectangleF(x,0,nw+2,Height),left,false);Typography.Draw(g,"天",small,accent,new RectangleF(x+nw+5,4,24,Height),left,false);}
            else Typography.Draw(g,count,small,accent,new RectangleF(Width-18-countWidth,0,countWidth,Height),right,false);
        }
    }
}
public class TopIconButton:MixedButton {
    public bool OpacityIcon;
    protected override void OnPaint(PaintEventArgs e){Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(BackColor);using(Pen p=new Pen(BlueTheme.Muted,1.3f)){float x=Width/2f,y=Height/2f;if(OpacityIcon){g.DrawEllipse(p,x-5,y-5,10,10);using(SolidBrush b=new SolidBrush(p.Color))g.FillPie(b,x-5,y-5,10,10,-90,180);}else {bool down=Text.Contains("展开");PointF[] points={new PointF(x-4,y+(down?-2:2)),new PointF(x,y+(down?2:-2)),new PointF(x+4,y+(down?-2:2))};g.DrawLines(p,points);}}
    }
}
public class AddCategoryButton:MixedButton {
    protected override void OnPaint(PaintEventArgs e){Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(BlueTheme.Outer);using(GraphicsPath box=PaperShapes.Rounded(new RectangleF(.5f,.5f,Width-1,Height-1),10))using(SolidBrush b=new SolidBrush(BlueTheme.Surface))using(Pen p=new Pen(BlueTheme.Border)){g.FillPath(b,box);g.DrawPath(p,box);}using(Pen p=new Pen(BlueTheme.Accent,1)){g.DrawLine(p,11,Height/2,Width-11,Height/2);g.DrawLine(p,Width/2,11,Width/2,Height-11);}}
}
public class SideRailPanel:FlowLayoutPanel {
    bool layingOut;
    protected override void OnPaintBackground(PaintEventArgs e){using(LinearGradientBrush b=new LinearGradientBrush(new Rectangle(0,-Top,Math.Max(1,Width),Parent==null?740:Parent.Height),BlueTheme.OuterTop,BlueTheme.OuterBottom,90))e.Graphics.FillRectangle(b,ClientRectangle);}
    protected override void OnLayout(LayoutEventArgs e){
        if(layingOut){base.OnLayout(e);return;}layingOut=true;
        try{bool compact=Controls.Count>9;foreach(Control c in Controls){c.Width=compact?38:60;c.Font=Typography.Chinese(compact?11:14,FontStyle.Regular);}base.OnLayout(e);HorizontalScroll.Enabled=false;HorizontalScroll.Visible=false;}
        finally{layingOut=false;}
    }
}

public class SettingsSheet:UtilityForm {
    public readonly Button Submit;readonly Label error;readonly System.Collections.Generic.List<MixedRichTextBox> fields=new System.Collections.Generic.List<MixedRichTextBox>();
    SettingsSheet(string title,int height){
        Text=title;AutoScaleMode=AutoScaleMode.None;Icon=AppIcons.Main;FormBorderStyle=FormBorderStyle.None;ClientSize=new Size(380,height);BackColor=BlueTheme.Paper;Font=Typography.Chinese(15,FontStyle.Regular);StartPosition=FormStartPosition.CenterScreen;ShowInTaskbar=false;KeyPreview=true;
        Label heading=new MixedLabel{Text=title,Location=new Point(24,22),Size=new Size(270,30),Font=Typography.Chinese(20,FontStyle.Regular),ForeColor=BlueTheme.Ink};Controls.Add(heading);
        Button close=new EditorDoneButton{Text="×",Location=new Point(326,18),Size=new Size(32,32),Font=Typography.Latin(23,FontStyle.Regular),DialogResult=DialogResult.Cancel};Controls.Add(close);
        Button cancel=new EditorDoneButton{Text="取消",Location=new Point(152,height-60),Size=new Size(96,36),Font=Font,DialogResult=DialogResult.Cancel};
        Submit=new EditorDoneButton{Text="保存",Location=new Point(260,height-60),Size=new Size(96,36),Font=Font};Controls.Add(cancel);Controls.Add(Submit);AcceptButton=Submit;CancelButton=cancel;
        error=new MixedLabel{Location=new Point(24,height-92),Size=new Size(332,28),Font=Typography.Chinese(12,FontStyle.Regular),ForeColor=BlueTheme.Danger};Controls.Add(error);
        Point start=Point.Empty,origin=Point.Empty;bool moving=false;heading.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){moving=true;start=Cursor.Position;origin=Location;heading.Capture=true;}};heading.MouseMove+=delegate{if(moving)Location=new Point(origin.X+Cursor.Position.X-start.X,origin.Y+Cursor.Position.Y-start.Y);};heading.MouseUp+=delegate{moving=false;heading.Capture=false;};
        using(GraphicsPath rounded=PaperShapes.Rounded(new RectangleF(0,0,Width,Height),22))Region=new Region(rounded);
        Shown+=delegate{if(fields.Count>0){fields[0].Focus();fields[0].SelectAll();}};
    }
    void Caption(string text,int y){Controls.Add(new MixedLabel{Text=text,Location=new Point(24,y),Size=new Size(332,24),Font=Typography.Chinese(14,FontStyle.Regular),ForeColor=BlueTheme.Muted});}
    MixedRichTextBox Field(string text,int y,int max){EditorPaper card=new EditorPaper{Location=new Point(24,y),Size=new Size(332,46),Padding=new Padding(12,10,12,8),BackColor=BlueTheme.Paper};MixedRichTextBox input=new MixedRichTextBox{Text=text,Multiline=false,WordWrap=false,BorderStyle=BorderStyle.None,Dock=DockStyle.Fill,MaxLength=max,Font=Typography.Chinese(17,FontStyle.Regular),ForeColor=BlueTheme.Ink,BackColor=BlueTheme.Surface,ScrollBars=RichTextBoxScrollBars.None};card.Controls.Add(input);Controls.Add(card);fields.Add(input);return input;}
    void Commit(Func<bool> save){try{if(save()){DialogResult=DialogResult.OK;Close();}else error.Text="保存失败，请重试。";}catch(Exception ex){error.Text=ex.Message;}}
    public static SettingsSheet Category(string title,string name,Func<string,bool> save){SettingsSheet f=new SettingsSheet(title,260);f.Caption("分类名称 · 最多 12 个字符",76);MixedRichTextBox input=f.Field(name,106,12);f.Submit.Click+=delegate{f.Commit(delegate{return save(input.Text);});};return f;}
    public static SettingsSheet Countdown(string name,string target,Func<string,DateTime,bool> save){SettingsSheet f=new SettingsSheet("设置倒计时",360);f.Caption("目标名称",76);MixedRichTextBox input=f.Field(name,106,80);f.Caption("目标日期",174);
        EditorPaper card=new EditorPaper{Location=new Point(24,204),Size=new Size(332,46),BackColor=BlueTheme.Paper};DateTimePicker date=new DateTimePicker{Location=new Point(12,10),Size=new Size(308,28),Font=Typography.Latin(17,FontStyle.Regular),Format=DateTimePickerFormat.Custom,CustomFormat="yyyy-MM-dd",CalendarFont=Typography.Chinese(15,FontStyle.Regular)};DateTime parsed;if(DateTime.TryParse(target,out parsed))date.Value=parsed;card.Controls.Add(date);f.Controls.Add(card);f.Submit.Click+=delegate{f.Commit(delegate{return save(input.Text,date.Value.Date);});};return f;}
    void SavePreview(string path){Show();Application.DoEvents();foreach(Control c in Controls){if(c.Right>ClientSize.Width||c.Bottom>ClientSize.Height)throw new Exception("Dialog content clipped");}foreach(MixedRichTextBox field in fields)if(field.Height<22)throw new Exception("Input height too small");using(Bitmap image=new Bitmap(Width,Height)){DrawToBitmap(image,ClientRectangle);using(Graphics g=Graphics.FromImage(image))foreach(MixedRichTextBox field in fields){Point pt=PointToClient(field.PointToScreen(Point.Empty));using(SolidBrush fill=new SolidBrush(field.BackColor))g.FillRectangle(fill,new Rectangle(pt,field.Size));using(SolidBrush ink=new SolidBrush(field.ForeColor))Typography.Draw(g,field.Text,field.Font,ink,new RectangleF(pt.X+1,pt.Y,field.Width-2,field.Height),null,false);}image.Save(path,System.Drawing.Imaging.ImageFormat.Png);}Close();}
    public static void PreviewAndCheck(string folder){System.IO.Directory.CreateDirectory(folder);using(SettingsSheet f=Countdown("提交论文",DateTime.Today.AddDays(6).ToString("yyyy-MM-dd"),delegate{return true;}))f.SavePreview(System.IO.Path.Combine(folder,"倒计时设置预览.png"));using(SettingsSheet f=Category("新增分类","阅读笔记",delegate{return true;}))f.SavePreview(System.IO.Path.Combine(folder,"分类设置预览.png"));
        bool accepted=false,allow=false;string captured=null;using(SettingsSheet f=Category("新增分类","测试分类",delegate(string name){captured=name;return allow;})){f.Show();Application.DoEvents();f.Submit.PerformClick();if(f.IsDisposed||captured!="测试分类")throw new Exception("Failed save must retain dialog");allow=true;f.Submit.PerformClick();accepted=f.IsDisposed;}if(!accepted)throw new Exception("Save did not close dialog");DateTime wanted=DateTime.Today.AddDays(6);bool correct=false;using(SettingsSheet f=Countdown("提交论文",wanted.ToString("yyyy-MM-dd"),delegate(string name,DateTime date){correct=name=="提交论文"&&date==wanted;return true;})){f.Show();Application.DoEvents();f.Submit.PerformClick();}if(!correct)throw new Exception("Countdown values changed");System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"设置窗口检查.txt"),"PASS: bounds, input heights, save failure retention, save success, exact task and date.");}
}

public class PersonalAvatarButton:Button {
    public PersonalAvatarButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;}
    protected override void OnPaint(PaintEventArgs e){Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;int parentHeight=Parent==null?818:Parent.Height;using(LinearGradientBrush outer=new LinearGradientBrush(new Rectangle(0,-Top,Math.Max(1,Width),parentHeight),BlueTheme.OuterTop,BlueTheme.OuterBottom,90))g.FillRectangle(outer,ClientRectangle);using(SolidBrush fill=new SolidBrush(BlueTheme.Surface))g.FillEllipse(fill,1,1,Width-2,Height-2);using(Pen ink=new Pen(BlueTheme.Accent,1.5f)){float cx=Width/2f;g.DrawEllipse(ink,cx-4,7,8,8);g.DrawArc(ink,cx-9,18,18,15,185,170);}if(Focused)using(Pen p=new Pen(BlueTheme.Accent))g.DrawEllipse(p,2,2,Width-4,Height-4);}
}
public class CenterActionButton:Button {
    public CenterActionButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);Cursor=Cursors.Hand;Font=Typography.Chinese(15,FontStyle.Regular);}
    protected override void OnPaint(PaintEventArgs e){Graphics g=e.Graphics;g.Clear(BlueTheme.Paper);g.SmoothingMode=SmoothingMode.AntiAlias;using(GraphicsPath shape=PaperShapes.Rounded(new RectangleF(0,0,Width-1,Height-1),12))using(Brush fill=new SolidBrush(BlueTheme.Surface))using(Brush ink=new SolidBrush(BlueTheme.Ink))using(Brush muted=new SolidBrush(BlueTheme.Muted))using(StringFormat sf=new StringFormat{LineAlignment=StringAlignment.Center}){g.FillPath(fill,shape);Typography.Draw(g,Text,Font,ink,new RectangleF(16,0,Width-54,Height),sf,false);Typography.Draw(g,"›",Font,muted,new RectangleF(Width-28,0,20,Height),sf,false);}if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(5,5,Width-10,Height-10));}
}
public class PersonalCenter:UtilityForm {
    readonly StartupToggleButton startupToggle;readonly StartupSetting startup;readonly TrackBar slider;readonly Label info,error;readonly Button position;readonly System.Windows.Forms.Timer debounce;readonly Func<bool> isUnlocked;readonly Action<bool> setUnlocked;readonly Func<int,bool> save;readonly Action<int> preview;int committed;
    public PersonalCenter(int transparency,Func<bool> unlocked,Action<bool> toggle,Func<int,bool> persist,Action<int> live,Action reset,Action folder,StartupSetting startupSetting=null,Func<string> currentTheme=null,Func<string,bool> chooseTheme=null){
        startup=startupSetting??AutoStart.Current;committed=transparency;isUnlocked=unlocked;setUnlocked=toggle;save=persist;preview=live;AutoScaleMode=AutoScaleMode.None;Text="个人中心";Icon=AppIcons.Main;FormBorderStyle=FormBorderStyle.None;ClientSize=new Size(400,650);BackColor=BlueTheme.Paper;Font=Typography.Chinese(15,FontStyle.Regular);StartPosition=FormStartPosition.CenterScreen;ShowInTaskbar=false;
        Label title=LabelAt("个人中心",24,22,270,32,22,BlueTheme.Ink);Button close=new EditorDoneButton{Text="完成",Location=new Point(320,20),Size=new Size(56,32),Font=Typography.Chinese(15,FontStyle.Regular)};close.Click+=delegate{Close();};Controls.Add(close);AcceptButton=close;
        LabelAt("主题配色",24,78,352,24,14,BlueTheme.Muted);
        Button themeEntry=new CenterActionButton{Text=Themes.Find(BlueTheme.CurrentId).Name,Location=new Point(24,108),Size=new Size(352,46)};Controls.Add(themeEntry);themeEntry.Click+=delegate{using(ThemePicker picker=new ThemePicker(currentTheme??delegate{return BlueTheme.CurrentId;},chooseTheme??delegate(string id){BlueTheme.Apply(id);return true;})){picker.ShowDialog();}themeEntry.Text=Themes.Find(BlueTheme.CurrentId).Name;};
        EditorPaper appearance=new EditorPaper{Location=new Point(24,176),Size=new Size(352,108),BackColor=BlueTheme.Paper};Controls.Add(appearance);
        info=new MixedLabel{BackColor=BlueTheme.Surface,Location=new Point(16,12),Size=new Size(320,24),Font=Font,ForeColor=BlueTheme.Ink};appearance.Controls.Add(info);
        slider=new TrackBar{Location=new Point(12,40),Size=new Size(328,32),Minimum=0,Maximum=99,TickStyle=TickStyle.None,Value=transparency,BackColor=BlueTheme.Surface};appearance.Controls.Add(slider);appearance.Controls.Add(new MixedLabel{Text="外围与各区背景同步透明，文字保持清晰",BackColor=BlueTheme.Surface,Location=new Point(16,78),Size=new Size(320,20),Font=Typography.Chinese(12,FontStyle.Regular),ForeColor=BlueTheme.Muted});
        LabelAt("桌面与数据",24,302,352,24,13,BlueTheme.Muted);position=Row("",338);position.Click+=delegate{setUnlocked(!isUnlocked());UpdatePosition();};UpdatePosition();
        Row("返回主屏右侧",390).Click+=delegate{TryAction(reset);};Row("打开数据文件夹",514).Click+=delegate{TryAction(folder);};LabelAt("进步签  v"+SharedData.ProgramVersion,24,582,352,24,12,BlueTheme.Muted);
        error=LabelAt("",24,612,352,28,12,BlueTheme.Danger);debounce=new System.Windows.Forms.Timer{Interval=350};debounce.Tick+=delegate{SaveTransparency();};slider.ValueChanged+=delegate{info.Text="整体背景透明度  "+slider.Value+"%";preview(slider.Value);debounce.Stop();debounce.Start();};info.Text="整体背景透明度  "+transparency+"%";
        startupToggle=new StartupToggleButton{Text="开机自动启动",Location=new Point(24,452),Size=new Size(352,46),AccessibleName="开机自动启动"};Controls.Add(startupToggle);
        TryAction(delegate{startupToggle.IsOn=startup.Enabled;});startupToggle.Click+=delegate{TryAction(delegate{startup.SetEnabled(!startupToggle.IsOn);startupToggle.IsOn=startup.Enabled;});};
        FormClosing+=delegate(object sender,FormClosingEventArgs e){if(!SaveTransparency())e.Cancel=true;};FormClosed+=delegate{debounce.Dispose();};KeyPreview=true;KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Escape){e.SuppressKeyPress=true;Close();}};
        using(GraphicsPath shape=PaperShapes.Rounded(new RectangleF(0,0,Width,Height),22))Region=new Region(shape);Point start=Point.Empty,origin=Point.Empty;bool moving=false;title.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){moving=true;start=Cursor.Position;origin=Location;title.Capture=true;}};title.MouseMove+=delegate{if(moving)Location=new Point(origin.X+Cursor.Position.X-start.X,origin.Y+Cursor.Position.Y-start.Y);};title.MouseUp+=delegate{moving=false;title.Capture=false;};
    }
    Label LabelAt(string text,int x,int y,int w,int h,float size,Color color){Label label=new MixedLabel{Text=text,Location=new Point(x,y),Size=new Size(w,h),Font=Typography.Chinese(size,FontStyle.Regular),ForeColor=color};Controls.Add(label);return label;}
    Button Row(string text,int y){Button row=new CenterActionButton{Text=text,Location=new Point(24,y),Size=new Size(352,46)};Controls.Add(row);return row;}
    void UpdatePosition(){position.Text=isUnlocked()?"位置已解锁 · 点击锁定":"位置已锁定 · 点击解锁";}
    void TryAction(Action action){try{action();error.Text="";}catch(Exception ex){error.Text=ex.Message;}}
    bool SaveTransparency(){debounce.Stop();if(slider.Value==committed)return true;try{if(!save(slider.Value))throw new Exception("保存失败，请重试。");committed=slider.Value;error.Text="";return true;}catch(Exception ex){preview(committed);error.Text=ex.Message;return false;}}
    public static void PreviewAndCheck(string directory){System.IO.Directory.CreateDirectory(directory);bool unlocked=false,allow=true;int saved=50,shown=50;using(PersonalCenter f=new PersonalCenter(50,delegate{return unlocked;},delegate(bool value){unlocked=value;},delegate(int value){if(!allow)return false;saved=value;return true;},delegate(int value){shown=value;},delegate{},delegate{},AutoStart.ForTests())){f.Show();Application.DoEvents();foreach(Control c in f.Controls)if(c.Right>f.ClientSize.Width||c.Bottom>f.ClientSize.Height)throw new Exception("Personal center clipped");using(Bitmap image=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(image,f.ClientRectangle);image.Save(System.IO.Path.Combine(directory,"个人中心预览.png"),System.Drawing.Imaging.ImageFormat.Png);}f.startupToggle.PerformClick();if(!f.startup.Enabled||!f.startupToggle.IsOn)throw new Exception("Startup enable failed");f.startupToggle.PerformClick();if(f.startup.Enabled||f.startupToggle.IsOn)throw new Exception("Startup disable failed");f.position.PerformClick();if(!unlocked)throw new Exception("Unlock failed");f.position.PerformClick();if(unlocked)throw new Exception("Lock failed");f.slider.Value=80;if(shown!=80||!f.SaveTransparency()||saved!=80)throw new Exception("Transparency save failed");allow=false;f.slider.Value=70;if(f.SaveTransparency()||shown!=80||saved!=80)throw new Exception("Transparency rollback failed");f.Close();if(f.IsDisposed)throw new Exception("Failed save closed window");allow=true;f.Close();if(saved!=70||!f.IsDisposed)throw new Exception("Retry failed");}System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"个人中心检查.txt"),"PASS: layout, lock toggle, live transparency, persistence, failure rollback, failed-close retention, retry. Folder opening is user-triggered and was not launched in tests.");}
}

public static class AppIcons {
    public static readonly Icon Main=Load(32);public static readonly Icon Tray=Load(16);
    static Icon Load(int size){using(System.IO.Stream stream=typeof(AppIcons).Assembly.GetManifestResourceStream("DesktopNote.AppIcon")){if(stream==null)throw new InvalidOperationException("缺少应用图标资源。");using(Icon icon=new Icon(stream,size,size))return (Icon)icon.Clone();}}
}
