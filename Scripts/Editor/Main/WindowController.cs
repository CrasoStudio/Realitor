using System.Collections.Generic;
using Godot;

public partial class WindowController : ColorRect
{
    [ExportGroup("BPMWindow")] 
    [Export] public Control bpmEditWindow, bpmListContent;
    [Export] public Button addButton, saveButton;
    [Export] public PackedScene bpmListObj;

    public List<BpmListItem> bpmListItems = [];
    
	public override void _Ready()
    {
        Hide();
        
        addButton.Pressed += AddBPMListItem;
        saveButton.Pressed += CloseBPMWindow;
	}

    public void OpenBPMWindow()
    {
        Show();
        bpmEditWindow.Show();

        foreach (var _event in EditorController.instance.bpmEvents)
        {
            var obj = bpmListObj.Instantiate<BpmListItem>();
            obj.startTimeEdit.Value = _event.startTime;
            obj.BPMValueEdit.Value = _event.BPM;
            bpmListItems.Add(obj);
            bpmListContent.AddChild(obj);
        }
    }
    
    public void CloseBPMWindow()
    {
        Hide();
        bpmEditWindow.Hide();

        EditorController.instance.topBar.a = false;
        
        EditorController.instance.bpmEvents.Clear();
        foreach (var item in bpmListItems)
        {
            var _event = new BPMEvent
            {
                startTime = (float)item.startTimeEdit.Value, 
                BPM = (float)item.BPMValueEdit.Value
            };
            EditorController.instance.bpmEvents.Add(_event);
            item.QueueFree();
        }
        EditorController.instance.bpmEvents.Sort((_event, event1) => _event.startTime.CompareTo(event1.startTime));
        bpmListItems.Clear();
        
        EditorController.instance.editArea.ReloadEditArea();
    }
    
    public void AddBPMListItem()
    {
        var obj = bpmListObj.Instantiate<BpmListItem>();
        bpmListItems.Add(obj);
        bpmListContent.AddChild(obj);
    }
}
