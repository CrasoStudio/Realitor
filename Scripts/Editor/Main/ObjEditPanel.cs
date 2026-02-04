using Godot;
using System;

public partial class ObjEditPanel : ColorRect
{
    [ExportGroup("Note Edit")] 
    [Export] public Control noteEditPanel;
    [Export] public SpinBox noteEditTime, noteEditDuration, noteEditTrack, noteEditSpeed;
    [Export] public OptionButton noteEditType, noteEditSpeedGroup;

    [ExportGroup("Sky Note Edit")] 
    [Export] public Control skyNoteEditPanel;
    [Export] public SpinBox skyNoteEditTime;
    [Export] public OptionButton skyNoteEditType;

    [ExportGroup("Sky Track Node Edit")]
    [Export] public Control skyTrackNodeEditPanel;
    [Export] public SpinBox skyTrackNodeEditTime, skyTrackNodeEditXPos, skyTrackNodeEditYPos, skyTrackNodeEditSpeed;

    [ExportGroup("Move Or Rotate Event Edit")] 
    [Export] public Control MREditPanel;
    [Export] public SpinBox MREditStartTime, MREditEndTime, MREditStartX, MREditStartY, MREditStartZ,
        MREditEndX, MREditEndY, MREditEndZ;
    [Export] public OptionButton MREditTransType, MREditEaseType;

    [ExportGroup("Single Event Edit With Easing Edit")] 
    [Export] public Control VEEditPanel;
    [Export] public SpinBox VEEditStartTime, VEEditEndTime, VEEditStartValue, VEEditEndValue;
    [Export] public OptionButton VEEditTransType, VEEditEaseType;

    [ExportGroup("Speed Event Edit")] 
    [Export] public Control SVEditPanel;
    [Export] public SpinBox SVEditStartTime, SVEditEndTime, SVEditValue;

    public override void _Ready()
    {
        ResetPanel();

        ConnectNoteEditSignals();
        ConnectSkyNoteEditSignals();
        ConnectSkyTrackNoteEditSignals();
    }
    
    public void SelectNote()
    {
        ResetPanel();
        
        var note = EditorController.instance.editArea.currentlySelectedNote;
        if ((int)note.thisNoteType > 2)
        {
            SelectSkyNote();
            return;
        }
        noteEditPanel.Show();
        noteEditTime.Value = note.time;
        noteEditDuration.Value = note.duration;
        noteEditTrack.Value = note.track;
        noteEditSpeed.Value = note.speed;
        noteEditType.Selected = (int)note.thisNoteType;
        //noteEditSpeedGroup.Selected = 
    }

    private void SelectSkyNote()
    {
        var note = EditorController.instance.editArea.currentlySelectedNote;
        skyNoteEditPanel.Show();
        skyNoteEditTime.Value = note.time;
        skyNoteEditType.Selected = (int)note.thisNoteType - 3;
    }
    
    public void SelectSkyTrackNode()
    {
        ResetPanel();
        
        var node = EditorController.instance.editArea.currentlySelectedSkyTrackNode;
        skyTrackNodeEditPanel.Show();
        skyTrackNodeEditTime.Value = node.time;
        skyTrackNodeEditXPos.Value = node.x;
        skyTrackNodeEditYPos.Value = node.y;
        skyTrackNodeEditSpeed.Value = node.speed;
    }
    
    public void ResetPanel()
    {
        noteEditPanel.Hide();
        skyNoteEditPanel.Hide();
        skyTrackNodeEditPanel.Hide();
        MREditPanel.Hide();
        VEEditPanel.Hide();
        SVEditPanel.Hide();
    }
    
    private void ConnectNoteEditSignals()
    { 
        noteEditTime.ValueChanged += value =>
        {
            if (EditorController.instance.editArea.currentlySelectedNote == null) return;
            EditorController.instance.editArea.currentlySelectedNote.time = (float)value;
            EditorController.instance.editArea.currentlySelectedNote.Init(EditorController.instance.editArea
                .currentlySelectedNote.thisNoteType);
            EditorController.instance.editArea.currentlySelectedNote.Update();
        };
        noteEditDuration.ValueChanged += value =>
        {
            if (EditorController.instance.editArea.currentlySelectedNote == null) return;
            EditorController.instance.editArea.currentlySelectedNote.duration = (float)value;
            EditorController.instance.editArea.currentlySelectedNote.Init(EditorController.instance.editArea
                .currentlySelectedNote.thisNoteType);
            EditorController.instance.editArea.currentlySelectedNote.Update();
        };
        noteEditTrack.ValueChanged += value =>
        {
            if (EditorController.instance.editArea.currentlySelectedNote == null) return;
            EditorController.instance.editArea.currentlySelectedNote.track = (int)value;
            EditorController.instance.editArea.currentlySelectedNote.Init(EditorController.instance.editArea
                .currentlySelectedNote.thisNoteType);
            EditorController.instance.editArea.currentlySelectedNote.Update();
        };
        noteEditSpeed.ValueChanged += value =>
        {
            if (EditorController.instance.editArea.currentlySelectedNote == null) return;
            EditorController.instance.editArea.currentlySelectedNote.speed = (float)value;
            EditorController.instance.editArea.currentlySelectedNote.Init(EditorController.instance.editArea
                .currentlySelectedNote.thisNoteType);
            EditorController.instance.editArea.currentlySelectedNote.Update();
        };
        noteEditType.ItemSelected += index =>
        {
            if (EditorController.instance.editArea.currentlySelectedNote == null) return;
            EditorController.instance.editArea.currentlySelectedNote.thisNoteType = (EditorController.Types)index;
            EditorController.instance.editArea.currentlySelectedNote.Init(EditorController.instance.editArea
                .currentlySelectedNote.thisNoteType);
            EditorController.instance.editArea.currentlySelectedNote.Update();
        };        
        noteEditSpeedGroup.ItemSelected += index =>
        { };
    }    
    
    private void ConnectSkyNoteEditSignals()
    {
        skyNoteEditTime.ValueChanged += value =>
        {
            if (EditorController.instance.editArea.currentlySelectedNote == null) return;
            EditorController.instance.editArea.currentlySelectedNote.time = (float)value;
            EditorController.instance.editArea.currentlySelectedNote.Init(EditorController.instance.editArea
                .currentlySelectedNote.thisNoteType);
            EditorController.instance.editArea.currentlySelectedNote.Update();
        };
        skyNoteEditType.ItemSelected += index =>
        {
            if (EditorController.instance.editArea.currentlySelectedNote == null) return;
            EditorController.instance.editArea.currentlySelectedNote.thisNoteType = (EditorController.Types)index;
            EditorController.instance.editArea.currentlySelectedNote.Init(EditorController.instance.editArea
                .currentlySelectedNote.thisNoteType);
            EditorController.instance.editArea.currentlySelectedNote.Update();
        };      
    }

    private void ConnectSkyTrackNoteEditSignals()
    {
        skyTrackNodeEditTime.ValueChanged += value =>
        {
            if (EditorController.instance.editArea.currentlySelectedSkyTrackNode == null) return;
            EditorController.instance.editArea.currentlySelectedSkyTrackNode.time = (float)value;
            EditorController.instance.editArea.currentlySelectedSkyTrackNode.Update();
        };        
        skyTrackNodeEditXPos.ValueChanged += value =>
        {
            if (EditorController.instance.editArea.currentlySelectedSkyTrackNode == null) return;
            EditorController.instance.editArea.currentlySelectedSkyTrackNode.x = (float)value;
            EditorController.instance.editArea.currentlySelectedSkyTrackNode.Update();
        };        
        skyTrackNodeEditYPos.ValueChanged += value =>
        {
            if (EditorController.instance.editArea.currentlySelectedSkyTrackNode == null) return;
            EditorController.instance.editArea.currentlySelectedSkyTrackNode.y = (float)value;
            EditorController.instance.editArea.currentlySelectedSkyTrackNode.Update();
        };        
        skyTrackNodeEditSpeed.ValueChanged += value =>
        {
            if (EditorController.instance.editArea.currentlySelectedSkyTrackNode == null) return;
            EditorController.instance.editArea.currentlySelectedSkyTrackNode.speed = (float)value;
            EditorController.instance.editArea.currentlySelectedSkyTrackNode.Update();
        };
    }
}