using System;using System.Drawing;using System.IO;using System.Windows.Forms;

public static class CategoryOrdering {
    // Reorder the existing category objects; IDs, content, styles and active ID stay intact.
    public static bool Move(NoteState state,string id,int index,IWorkspaceRepository store){
        int previous=state.Categories.FindIndex(delegate(NoteCategory c){return c.Id==id;});
        if(previous<0||index<0||index>=state.Categories.Count||previous==index)return false;
        NoteCategory moved=state.Categories[previous];state.Categories.RemoveAt(previous);state.Categories.Insert(index,moved);
        try{store.Save(state);}catch{state.Categories.RemoveAt(index);state.Categories.Insert(previous,moved);throw;}
        return true;
    }
}
public class CategoryRail:SideRailPanel {
    public int DropY=-1;
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(DropY>=0)using(Pen line=new Pen(BlueTheme.Accent,2))e.Graphics.DrawLine(line,2,DropY,Width-3,DropY);}
}
public class SortableSideTab:SideTab {
    Point start;bool dragging,suppressClick;
    public Action<Point> DragMoved,Dropped;public Action DragCancelled;
    protected override void OnMouseDown(MouseEventArgs e){if(e.Button==MouseButtons.Left){start=e.Location;dragging=false;}base.OnMouseDown(e);}
    protected override void OnMouseMove(MouseEventArgs e){
        if(e.Button==MouseButtons.Left){Size threshold=SystemInformation.DragSize;if(!dragging&&(Math.Abs(e.X-start.X)>threshold.Width/2||Math.Abs(e.Y-start.Y)>threshold.Height/2)){dragging=true;Capture=true;Cursor=Cursors.SizeNS;}
            if(dragging&&DragMoved!=null)DragMoved(PointToScreen(e.Location));}
        base.OnMouseMove(e);
    }
    protected override void OnMouseUp(MouseEventArgs e){
        bool drop=dragging&&e.Button==MouseButtons.Left;Point point=PointToScreen(e.Location);suppressClick=drop;
        try{base.OnMouseUp(e);}finally{suppressClick=false;dragging=false;Capture=false;Cursor=Cursors.Hand;}
        if(drop&&Dropped!=null)Dropped(point);
    }
    protected override void OnClick(EventArgs e){if(!suppressClick&&!dragging)base.OnClick(e);}
    protected override void OnMouseCaptureChanged(EventArgs e){if(!Capture){dragging=false;Cursor=Cursors.Hand;if(DragCancelled!=null)DragCancelled();}base.OnMouseCaptureChanged(e);}
}
