using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class NoteObj : ColorRect
{
    [Export] public Dictionary<EditorController.Types, Color> colors;
    public EditorController.Types thisNoteType = EditorController.Types.TapNote;
    
    public int track;
    [Export] public float time, duration;

    private bool isReady;

    private Line2D line;

    public void Init(EditorController.Types noteType)
    {
        thisNoteType = noteType;

        if(colors.TryGetValue(thisNoteType, out var color)) Color = color;

        isReady = true;
    }

    public override async void _Ready()
    {
        while (!isReady)
        {
            await Task.Delay(1);
        }

        if (thisNoteType == EditorController.Types.HoldNote)
        {
            line = new Line2D();
            line.Width = 100f;
            line.DefaultColor = Color;
            AddChild(line);
        }
    }

    public override void _Process(double delta)
    {
        if (thisNoteType == EditorController.Types.HoldNote && duration == 0)
        {
            EditorController.instance.editArea.placeable = false;
            
            var nearestBeatLine = EditorController.GetNearestObj(GetGlobalMousePosition(),
                EditorController.instance.editArea._poolN);
            
            if(nearestBeatLine.Position.Y > Position.Y) return;

            line.Points = [line.ToLocal(new Vector2(GlobalPosition.X + Size.X / 2, GlobalPosition.Y + Size.Y / 2)),
                line.ToLocal(new Vector2(GlobalPosition.X + Size.X / 2, nearestBeatLine.GlobalPosition.Y + nearestBeatLine.Size.Y / 2))];

            if (Input.IsActionJustPressed("ui_mouse_press"))
            {
                EditorController.instance.editArea.placeable = true;
                duration = EditorController.GetBeatFromTime(((BeatLine)nearestBeatLine).timeSec,
                    EditorController.instance.bpmEvents) - time;
            }
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Right })
        {
            EditorController.instance.editArea.notes.Remove(this);
            QueueFree();
        }
    }

    public void Update()
    {
        // 1. 更新 Y 轴位置 (基于时间)
        float timeInSeconds = EditorController.GetTimeFromBeat(time, EditorController.instance.bpmEvents);
        float yPos = EditorController.instance.editArea.judgeLine.GlobalPosition.Y - 
                    (EditorController.instance.offset / 1000 + timeInSeconds) * EditorController.instance.editArea.PixelsPerSecond *
                    EditorController.instance.beatScale;

        // 2. 更新 X 轴位置 (基于轨道类型)
        float xPos = GlobalPosition.X; // 默认保持当前 X

        if (thisNoteType == EditorController.Types.TapNote || thisNoteType == EditorController.Types.HoldNote)
        {
            // 地面音符：对齐到对应的 SnapLine
            var snapLines = EditorController.instance.editArea.groundNotesSnapLineParent.GetChildren();
            if (track >= 0 && track < snapLines.Count)
            {
                var targetSnap = (Control)snapLines[track];
                xPos = targetSnap.GlobalPosition.X - Size.X / 2;
            }
        }
        else
        {
            // 天空音符：根据 ArcLine2D 获取曲线上的 X 坐标
            if (EditorController.instance.editArea.skyTrackObjs.Count > track)
            {
                var stObj = EditorController.instance.editArea.skyTrackObjs[track];
                var xline = stObj.line;
                
                // 将全局 Y 转换为 Line2D 的本地坐标系
                Vector2 localPos = xline.ToLocal(new Vector2(0, yPos));
                // 调用你提到的根据 Y 获取位置的功能
                Vector2 curveLocalPos = xline.GetPositionFromY(localPos.Y);
                // 转回全局坐标并应用到 X
                xPos = xline.ToGlobal(curveLocalPos).X - Size.X / 2;
            }
        }

        // 应用更新后的位置
        GlobalPosition = new Vector2(xPos, yPos - Size.Y / 2);
        
        // 3. 重新计算时间与长度（防止因 BPM 变更导致的数值偏移）
        time = EditorController.GetBeatFromTime(timeInSeconds, EditorController.instance.bpmEvents);
        
        if (thisNoteType == EditorController.Types.HoldNote)
        {
            float endTime = EditorController.GetTimeFromBeat(time + duration, EditorController.instance.bpmEvents);
            duration = EditorController.GetBeatFromTime(endTime, EditorController.instance.bpmEvents) - time;
            
            // 更新 Hold 线的显示
            UpdateHoldLine(yPos);
        }
    }

    // 提取 Hold 线更新逻辑
    private void UpdateHoldLine(float currentY)
    {
        if (line == null) return;

        float endSeconds = EditorController.GetTimeFromBeat(time + duration, EditorController.instance.bpmEvents);
        float endY = EditorController.instance.editArea.judgeLine.GlobalPosition.Y - 
                    (EditorController.instance.offset / 1000 + endSeconds) * EditorController.instance.editArea.PixelsPerSecond *
                    EditorController.instance.beatScale;

        Vector2 startPoint = line.ToLocal(new Vector2(GlobalPosition.X + Size.X / 2, currentY));
        Vector2 endPoint = line.ToLocal(new Vector2(GlobalPosition.X + Size.X / 2, endY));
        
        line.Points = [startPoint, endPoint];
    }
}
