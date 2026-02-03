using Godot;
using System;

public partial class SkyTrackNodeObj : TextureRect
{
    [Export] public int track;
    public float x;
    public float time;
    
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                EditorController.instance.editArea.currentlySelectedSkyTrackNode = this;
            }
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                if (EditorController.instance.editArea.currentlySelectedSkyTrackNode == this)
                    EditorController.instance.editArea.currentlySelectedSkyTrackNode = null;
                EditorController.instance.editArea.skyTracks[track].Remove(this);
                QueueFree();
            }
        }
    }
    
    public void Update()
    {
        // 1. 获取当前时间对应的全局 Y 坐标
        float timeInSeconds = EditorController.GetTimeFromBeat(time, EditorController.instance.bpmEvents);
        float yPos = EditorController.instance.editArea.judgeLine.GlobalPosition.Y - 
                     (EditorController.instance.offset / 1000 + timeInSeconds) * EditorController.instance.editArea.PixelsPerSecond *
                     EditorController.instance.beatScale;

        // 2. 根据 x 变量计算全局 X 坐标
        // 逻辑：将 -1 到 1 的 x 映射到编辑区域的像素宽度（参考 EditArea.cs 中的计算逻辑）
        // 这里的 862 应该是你定义的轨道区域宽度
        float xPos = (x + 1) * 862 / 2f - Size.X / 2;

        // 3. 应用位置
        GlobalPosition = new Vector2(xPos, yPos - Size.Y / 2);
    
        // 4. 更新时间（同步 BPM 变化）
        time = EditorController.GetBeatFromTime(timeInSeconds, EditorController.instance.bpmEvents);
    }
}
