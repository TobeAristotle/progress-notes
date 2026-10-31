using System;using System.Drawing;using System.IO;using System.Windows.Forms;
public static class CategoryOrderingTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    sealed class FailingStore:IWorkspaceRepository {
        public string Folder{get;set;}public NoteState Load(){throw new NotSupportedException();}public ContentDocument LoadContent(){throw new NotSupportedException();}public DeviceSettings LoadDeviceSettings(){throw new NotSupportedException();}public void Save(NoteState value){throw new IOException("Expected test failure");}public void SaveContent(ContentDocument value){throw new NotSupportedException();}public void SaveDeviceSettings(DeviceSettings value){throw new NotSupportedException();}
    }
    sealed class TabProbe:SortableSideTab {
        public void Drag(){OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,10,10,0));OnMouseMove(new MouseEventArgs(MouseButtons.Left,0,10,70,0));OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,10,70,0));}
    }
    public static void Run(string directory){
        Directory.CreateDirectory(directory);LocalJsonRepository store=new LocalJsonRepository(Path.Combine(directory,"data"));NoteState state=new NoteState();state.EnsureCategories();
        state.Note="保留原文 ABC";state.Categories[0].Note.Formats.Add(new NoteFormat{Start=0,Length=4,Bold=true,Highlight=true});state.Device.BackgroundTransparencyPercent=63;
        string first=state.Categories[0].Id,last=state.Categories[state.Categories.Count-1].Id,active=state.ActiveId;store.Save(state);
        Check(CategoryOrdering.Move(state,last,0,store)&&store.Load().Categories[0].Id==last,"Move to first/persistence failed");
        Check(CategoryOrdering.Move(state,last,state.Categories.Count-1,store)&&store.Load().Categories[0].Id==first,"Move to last/persistence failed");
        Check(state.ActiveId==active&&state.Categories[0].Content=="保留原文 ABC"&&state.Categories[0].Note.Formats[0].Highlight&&state.Device.BackgroundTransparencyPercent==63,"Reorder changed content/styles/settings");
        string before=SharedData.StateHash(state);bool failed=false;try{CategoryOrdering.Move(state,first,2,new FailingStore());}catch(IOException){failed=true;}Check(failed&&SharedData.StateHash(state)==before,"Failed save did not restore exact original state");
        string disk=SharedData.Hash(store.PathName);Check(!CategoryOrdering.Move(state,first,-1,store)&&!CategoryOrdering.Move(state,first,state.Categories.Count,store)&&!CategoryOrdering.Move(state,"unknown",0,store)&&!CategoryOrdering.Move(state,first,0,store)&&SharedData.Hash(store.PathName)==disk,"Invalid/no-op reorder wrote data");
        using(PanelForm panel=new PanelForm(store,state,true)){panel.Show();Application.DoEvents();Check(panel.MoveCategory(last,0)&&store.Load().Categories[0].Id==last&&state.ActiveId==active,"Panel reorder integration failed");panel.Close();}
        using(Form host=new Form())using(TabProbe tab=new TabProbe()){
            host.Controls.Add(tab);host.Show();Application.DoEvents();int moved=0,dropped=0,clicked=0;tab.DragMoved=delegate{moved++;};tab.Dropped=delegate{dropped++;};tab.Click+=delegate{clicked++;};tab.Drag();Check(moved>0&&dropped==1&&clicked==0,"Drag failed or also selected the category");host.Close();
        }
        using(NoteEditor editor=new NoteEditor("格式测试 ABC",new Rectangle(0,0,344,520),delegate{return true;})){
            editor.Show();Application.DoEvents();int buttons=0;foreach(Control c in editor.Controls)if(c is Button&&c.Visible)buttons++;Check(buttons==1,"Formatting toolbar still visible");
            editor.EditingBox.Select(0,4);KeyEventArgs bold=new KeyEventArgs(Keys.Control|Keys.B);editor.HandleEditorKey(bold);Check(bold.SuppressKeyPress&&NativeCharacter.IsBold(editor.EditingBox),"Ctrl+B failed");
            KeyEventArgs highlight=new KeyEventArgs(Keys.Control|Keys.H);editor.HandleEditorKey(highlight);Check(highlight.SuppressKeyPress&&editor.EditingBox.SelectionBackColor.ToArgb()==NoteFormatting.Yellow.ToArgb(),"Ctrl+H failed");editor.HandleEditorKey(new KeyEventArgs(Keys.Control|Keys.H));Check(editor.EditingBox.SelectionBackColor.ToArgb()!=NoteFormatting.Yellow.ToArgb(),"Ctrl+H did not toggle off");
            editor.SaveEditorPreview(Path.Combine(directory,"简洁编辑框预览.png"));editor.Close();
        }
        File.WriteAllText(Path.Combine(directory,"sort-test-result.txt"),"PASS: move to first/last persisted, active ID/content/styles/settings retained, save-failure exact rollback, invalid/no-op moves; panel integration, drag/drop without click; hidden toolbar and Ctrl+B/Ctrl+H toggle. Isolated data only.");
    }
}
