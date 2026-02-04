using Godot;
using System;

public partial class SkyTrackNodeObj : TextureRect
{
    [Export] public int track;
    public float x, y;
    public float time;
    public float speed = 1;

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (Input.IsActionJustPressed("ui_mouse_middle_button_press"))
            {
                EditorController.instance.editArea.currentlySelectedSkyTrackNode = this;
                EditorController.instance.objEditPanel.SelectSkyTrackNode();
            }
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                if (EditorController.instance.editArea.currentlySelectedSkyTrackNode == this)
                {
                    EditorController.instance.editArea.currentlySelectedSkyTrackNode = null;
                    EditorController.instance.objEditPanel.ResetPanel();
                }
                EditorController.instance.editArea.skyTracks[track].Remove(this);
                QueueFree();
            }
        }
    }

    public void Update()
    {
        float yPos = EditorController.instance.editArea.startPos.Y -
                     (EditorController.instance.offset / 1000 +
                      EditorController.GetTimeFromBeat(time, EditorController.instance.bpmEvents)) *
                     EditorController.instance.editArea.PixelsPerSecond *
                     EditorController.instance.beatScale -
                     Size.Y / 2;

        float xPos = (x + 1) * 862 / 2f;
        GlobalPosition = GlobalPosition with { X = xPos };

        Position = Position with { Y = yPos };

        time = EditorController.GetBeatFromTime(
                EditorController.GetTimeFromBeat(time, EditorController.instance.bpmEvents), 
                EditorController.instance.bpmEvents);
    }
}
