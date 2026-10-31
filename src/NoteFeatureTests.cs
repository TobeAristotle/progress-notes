using System;using System.Collections.Generic;using System.Drawing;using System.IO;using System.Windows.Forms;
public static class NoteFeatureTests {
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void WriteRaw(string file,NoteState state){using(FileStream stream=File.Create(file))new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(WorkspaceEnvelope)).WriteObject(stream,new WorkspaceEnvelope{Content=state.Content,Device=state.Device});}
    sealed class Probe:PaperRecordLabel {public string Copied;protected override void PublishCopy(string value){Copied=value;}public void Drag(int first,int last){Point a=TestPoint(first,false),b=TestPoint(last,true);OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,a.X,a.Y,0));OnMouseMove(new MouseEventArgs(MouseButtons.Left,0,b.X,b.Y,0));OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,b.X,b.Y,0));}}
    sealed class FailingStore:IWorkspaceRepository {
        public string Folder {get;set;}public NoteState Load(){throw new NotSupportedException();}public void Save(NoteState state){throw new IOException("Test failure");}public ContentDocument LoadContent(){throw new NotSupportedException();}public DeviceSettings LoadDeviceSettings(){throw new NotSupportedException();}public void SaveContent(ContentDocument state){throw new NotSupportedException();}public void SaveDeviceSettings(DeviceSettings state){throw new NotSupportedException();}
    }
    public static void Run(string directory){
        Directory.CreateDirectory(directory);LocalJsonRepository store=new LocalJsonRepository(Path.Combine(directory,"data"));NoteState state=new NoteState();state.EnsureCategories();state.Note="重点 ABC\n下一行内容";store.Save(state);string original=state.Note;
        using(NoteEditor editor=new NoteEditor(original,new Rectangle(0,0,344,520),delegate{return true;})){
            editor.Show();Application.DoEvents();MixedRichTextBox box=editor.EditingBox;
            editor.PersistRich=delegate(string text,List<NoteFormat> styles){state.Note=text;state.Categories[0].Note.Formats=styles;store.Save(state);return true;};
            box.Select(0,6);editor.ToggleFormat(false);editor.ToggleFormat(true);
            Check(NativeCharacter.IsBold(box)&&box.SelectionBackColor.ToArgb()==NoteFormatting.Yellow.ToArgb(),"Format buttons not applied");
            box.ApplyFonts();Check(NativeCharacter.IsBold(box),"Mixed-font normalization erased bold");
            box.Select(box.TextLength,0);box.SelectedText=" 新增";box.Select(0,6);Check(NativeCharacter.IsBold(box),"Typing erased earlier bold");
            editor.Close();Check(editor.IsDisposed,"Formatted editor did not save/close");
        }
        state=store.Load();Check(state.Categories[0].Note.Formats.Count>0&&state.Categories[0].Note.Formats[0].Bold&&state.Categories[0].Note.Formats[0].Highlight,"Formats not persisted");
        using(NoteEditor editor=new NoteEditor(state.Note,new Rectangle(0,0,344,520),delegate{return true;},state.Categories[0].Note.Formats)){
            editor.Show();Application.DoEvents();editor.EditingBox.Select(0,6);Check(NativeCharacter.IsBold(editor.EditingBox)&&editor.EditingBox.SelectionBackColor.ToArgb()==NoteFormatting.Yellow.ToArgb(),"Reopen lost formatting");
            editor.ToggleFormat(false);editor.ToggleFormat(true);Check(!NativeCharacter.IsBold(editor.EditingBox)&&editor.EditingBox.SelectionBackColor.ToArgb()!=NoteFormatting.Yellow.ToArgb(),"Toggling formats off failed");editor.Close();
        }
        string theme=BlueTheme.CurrentId;try{BlueTheme.Apply("midnight");using(NoteEditor editor=new NoteEditor(state.Note,new Rectangle(0,0,344,520),delegate{return true;},state.Categories[0].Note.Formats)){
            editor.Show();Application.DoEvents();editor.EditingBox.Select(0,6);Check(editor.EditingBox.SelectionColor.ToArgb()==NoteFormatting.HighlightInk.ToArgb(),"Dark theme highlight text lacks contrast");editor.SaveEditorPreview(Path.Combine(directory,"编辑格式预览.png"));editor.Close();
        }}finally{BlueTheme.Apply(theme);}
        bool allow=false;using(NoteEditor editor=new NoteEditor(original,new Rectangle(0,0,344,520),delegate{return true;})){
            editor.Show();Application.DoEvents();editor.PersistRich=delegate(string text,List<NoteFormat> spans){return allow;};editor.EditingBox.Select(0,2);editor.ToggleFormat(false);editor.ToggleFormat(true);editor.Close();Check(!editor.IsDisposed&&NativeCharacter.IsBold(editor.EditingBox),"Failed formatted save discarded the editor/styles");allow=true;editor.Close();Check(editor.IsDisposed,"Formatted save retry failed");
        }
        using(Form host=new Form{ClientSize=new Size(344,520)})using(Probe note=new Probe{Dock=DockStyle.Fill,ForeColor=BlueTheme.Ink,Font=Typography.Chinese(17,FontStyle.Regular)}){
            string longText="";for(int i=0;i<80;i++)longText+="第"+i+"行 ABC 内容\n";note.Text=longText;note.Formats=new List<NoteFormat>{new NoteFormat{Start=0,Length=8,Bold=true,Highlight=true}};host.Controls.Add(note);host.Show();Application.DoEvents();
            using(Bitmap image=new Bitmap(note.Width,note.Height)){note.DrawToBitmap(image,note.ClientRectangle);image.Save(Path.Combine(directory,"格式预览.png"));}
            note.Drag(0,5);Check(note.SelectedText==longText.Substring(0,6),"Bold glyph selection incorrect");
            note.ScrollWheel(-120);Check(note.ScrollOffset>0,"Read-only wheel did not scroll");int first=longText.IndexOf("第5行"),last=first+5;note.Drag(first,last);Check(note.SelectedText==longText.Substring(first,6),"Scrolled selection hit testing incorrect");note.CopySelection();Check(note.Copied==longText.Substring(first,6)&&note.Text==longText,"Scrolled copy changed text");
            note.ScrollWheel(-120000);float bottom=note.ScrollOffset;note.ScrollWheel(-120);Check(note.ScrollOffset==bottom,"Scrolled beyond bottom");note.ScrollWheel(120000);Check(note.ScrollOffset==0,"Cannot return to top");note.ScrollWheel(-120);note.CategoryName="另一个分类";Check(note.ScrollOffset==0&&!note.HasTextSelection,"Category switch retained scroll or selection");host.Close();
        }
        string deleting=state.Categories[0].Id;state.ActiveId=deleting;store.Save(state);int count=state.Categories.Count;string other=state.Categories[1].Id;Check(CategoryDeletion.Delete(state,deleting,store)&&state.Categories.Count==count-1&&state.ActiveId==other,"Active category removal failed");
        string[] archives=Directory.GetDirectories(Path.Combine(store.Folder,"backups"),"deleted-category-*");Check(archives.Length==1,"Deletion snapshot missing");NoteState before=new LocalJsonRepository(archives[0]).Load();Check(before.Categories[0].Id==deleting&&before.Categories[0].Note.Formats.Count>0,"Deletion backup lost content/style");
        string active=state.ActiveId;count=state.Categories.Count;bool failed=false;try{CategoryDeletion.Delete(state,active,new FailingStore{Folder=Path.Combine(directory,"failed")});}catch(IOException){failed=true;}Check(failed&&state.Categories.Count==count&&state.ActiveId==active&&state.Categories[0].Id==active,"Failed deletion did not roll back");
        while(state.Categories.Count>1)CategoryDeletion.Delete(state,state.Categories[state.Categories.Count-1].Id,store);Check(!CategoryDeletion.Delete(state,state.ActiveId,store)&&state.Categories.Count==1,"Last category could be removed");
        using(UtilityForm form=new UtilityForm()){form.Show();Application.DoEvents();int style=Native.GetWindowLong(form.Handle,-20);Check((style&0x80)!=0&&(style&0x40000)==0,"Alt+Tab exclusion style missing");form.Close();}
        // A v1 content file must retain plain text and upgrade before formatted writes.
        string originalText=state.Note;state.Content.SchemaVersion=1;foreach(NoteCategory c in state.Categories)c.Note.Formats=null;WriteRaw(store.PathName,state);string legacyHash=SharedData.Hash(store.PathName);
        SharedData.Prepare(directory,store);state=store.Load();Check(state.Content.SchemaVersion==2&&state.Note==originalText&&state.Categories[0].Note.Formats.Count==0,"Plain content migration failed");
        bool snapshotFound=false;foreach(string folder in Directory.GetDirectories(Path.Combine(store.Folder,"backups"),"upgrade-*"))if(SharedData.Hash(Path.Combine(folder,"workspace.json"))==legacyHash)snapshotFound=true;Check(snapshotFound,"Migration did not preserve the original bytes");
        state.Content.SchemaVersion=3;WriteRaw(store.PathName,state);string futureHash=SharedData.Hash(store.PathName);bool refused=false;try{store.Load();}catch(UnsupportedDataVersionException){refused=true;}Check(refused&&futureHash==SharedData.Hash(store.PathName),"Future content silently fell back to older data");
        File.WriteAllText(Path.Combine(directory,"features-test-result.txt"),"PASS: persisted bold/yellow highlight, typing and reopen/toggle preservation; readonly wheel limits and scrolled copy; category deletion snapshot, save-failure rollback and last-category guard; Alt+Tab native toolwindow style; isolated content migration. Actual shell switching still requires manual verification.");
    }
}
