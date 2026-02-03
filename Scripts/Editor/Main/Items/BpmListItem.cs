using Godot;
using System;

public partial class BpmListItem : Control
{
    [Export] public SpinBox startTimeEdit, BPMValueEdit;
    [Export] public Button delectButton;

    public override void _Ready()
    {
        delectButton.Pressed += () => EditorController.instance.windowController.bpmListItems.Remove(this);
    }
}
