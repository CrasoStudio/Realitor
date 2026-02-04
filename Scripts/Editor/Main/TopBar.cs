using Godot;
using System;

public partial class TopBar : ColorRect
{
	[ExportGroup("Music Control Area")] 
	[Export] public HSlider musicTimeSlider;
	[Export] public Button playButton, pauseButton;
	[Export] public Label musicTimeLabel;

	[ExportGroup("Chart Settings")] 
	[Export] public SpinBox bpmEdit, offsetEdit, beatSegmentEdit, beatScaleEdit, musicSpeedEdit, noteEditSegEdit;

	[ExportGroup("Type Selector")] 
	[Export] public ButtonGroup selectButtonGroup;
	[Export] public Label currentlySelectedType;

	[ExportGroup("Control Panel")] 
	[Export] public Button loadChartButton, loadMusicButton, saveChartButton, previewChartButton,
		utilitiesButton, settingsButton, quitButton;

	[ExportGroup("Utilities Panel")] 
	[Export] public Control utilitiesPanel;
	[Export] public Button utilitiesCloseButton;
	[Export] public Button bpmEditToolButton, guideToolButton, bpmToSpeedGroupToolButton;

	public bool a = true;
	
	public override void _Ready()
	{
		playButton.Pressed += () =>
		{
			EditorController.instance.musicPlayer.Play(EditorController.instance.songTime);
			EditorController.instance.isPlaying = true;
		};
		pauseButton.Pressed += () =>
		{
			EditorController.instance.musicPlayer.Stop();
			EditorController.instance.isPlaying = false;
		};
		musicTimeSlider.ValueChanged += value =>
		{
			EditorController.instance.songTime = (int)value;
		}; 
		
		bpmEdit.ValueChanged += value =>
		{
			EditorController.instance.bpm = (float)value;
			if (a) EditorController.instance.bpmEvents[0] = new BPMEvent { BPM = (float)value, startTime = 0 };
			EditorController.instance.editArea.ReloadEditArea();
		};
		offsetEdit.ValueChanged += value =>
		{
			EditorController.instance.offset = (float)value;
			EditorController.instance.editArea.ReloadEditArea();
		};
		beatSegmentEdit.ValueChanged += value =>
		{
			EditorController.instance.beatSeg = (int)value;
			EditorController.instance.editArea.ReloadEditArea();
		};
		beatScaleEdit.ValueChanged += value =>
		{
			EditorController.instance.beatScale = (float)value;
			EditorController.instance.editArea.ReloadEditArea();
		};
		musicSpeedEdit.ValueChanged += value =>
		{
			EditorController.instance.musicSpeed = (float)value;
			EditorController.instance.musicPlayer.PitchScale = (float)value;
			var musicBusIndex = AudioServer.GetBusIndex("Music");
			var pitchEffect = (AudioEffectPitchShift)AudioServer.GetBusEffect(musicBusIndex, 0);
			pitchEffect.PitchScale = 1f / (float)value;
		};
		noteEditSegEdit.ValueChanged += value =>
		{
			EditorController.instance.noteEditSeg = (int)value;
			EditorController.instance.editArea.ReloadEditArea();
		};
        
        foreach (var button in selectButtonGroup.GetButtons())
        {
            button.Pressed += () => CheckSelectType(button);
        }
		selectButtonGroup.GetButtons()[0].ButtonPressed = true;

		loadChartButton.Pressed += EditorController.instance.LoadChart;
		loadMusicButton.Pressed += EditorController.instance.LoadMusic;
		saveChartButton.Pressed += EditorController.instance.SaveChart;
		previewChartButton.Pressed += EditorController.instance.PreviewChart;
		utilitiesButton.Pressed += utilitiesPanel.Show;
		settingsButton.Pressed += () => { };
		quitButton.Pressed += () => GetTree().Quit();
		
		utilitiesCloseButton.Pressed += utilitiesPanel.Hide;
		bpmEditToolButton.Pressed += EditorController.instance.windowController.OpenBPMWindow;
	}

	public override void _Process(double delta)
	{
		if(!EditorController.instance.isLoaded) return;                     
		
		musicTimeSlider.Editable = !EditorController.instance.isPlaying;
		playButton.Disabled = EditorController.instance.isPlaying;
		pauseButton.Disabled = !EditorController.instance.isPlaying;

        musicTimeLabel.Text = SecondsToMMSS((int)EditorController.instance.songTime)
                              + "/" + SecondsToMMSS((int)EditorController.instance.musicPlayer.GetStream().GetLength());

        EditorController.instance.editArea.placeable = selectButtonGroup.GetPressedButton() != null;
	}

    public void SyncEditText()
    {
        bpmEdit.Value = EditorController.instance.bpm;
        if (EditorController.instance.bpmEvents.Count > 1) a = false;
        offsetEdit.Value = EditorController.instance.offset;
    }
    
	private void CheckSelectType(BaseButton button)
	{
		EditorController.instance.currentlySelectedType = (string)button.Name switch
		{
			"Tap" => EditorController.Types.TapNote,
			"Hold" => EditorController.Types.HoldNote,
			"SkyTrack" => EditorController.Types.SkyTrack,
			"SkyTap" => EditorController.Types.SkyTapNote,
			"SkyFlick" => EditorController.Types.SkyFlickNote,
			"SkyCatch" => EditorController.Types.SkyCatchNote,
			_ => EditorController.Types.TapNote
		};
		currentlySelectedType.Text = "放置类型：" + ((Button)button).Text;
	}
	
	private string SecondsToMMSS(int totalSeconds)
	{
		var minutes = totalSeconds / 60;
		var seconds = totalSeconds % 60;
		return $"{minutes:D2}:{seconds:D2}";
	}
}
