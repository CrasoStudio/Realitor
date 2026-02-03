using Godot;
using System;
using System.Threading.Tasks;

public partial class GameUI : CanvasLayer
{
    [Export] public Label infoLabel;
    
    [Export] public Control pausePanel;
    [Export] public Button playButton, restartButton, exitButton;
    [Export] public Button pauseButton;

    public override void _Ready()
    {
        pauseButton.Pressed += NoteSettings.controller.ChangePauseState;
        playButton.Pressed += NoteSettings.controller.ChangePauseState;
        restartButton.Pressed += () =>
        {
            EditorController.instance.previewSceneParent.GetChild(0).QueueFree();
            EditorController.instance.PreviewChart();
        };
        exitButton.Pressed += () =>
        {
            EditorController.instance.previewSceneParent.GetChild(0).QueueFree();
        };
        
        pausePanel.Hide();
    }

    public override void _Process(double delta)
    {
        infoLabel.Text = "Num " + NoteSettings.controller.judgedNum + "/" + NoteSettings.controller.noteNum;
        
        if (Input.IsActionJustPressed("GamePause"))
        {
            NoteSettings.controller.ChangePauseState();
        }
    }
}
