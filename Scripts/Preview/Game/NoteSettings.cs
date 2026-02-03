using Godot;
using System;

public partial class NoteSettings : Node3D
{
    [Export] public GameController controllerEx;
    [Export] public GameUI uiControllerEx;
	[Export] public float noteSpeedEx;
    [Export] public float offsetEx;
    [Export] public Color[] judgementColorsEx = new Color[3]; //OPTIMUM, EXACT, PASS

    public static GameController controller;
    public static GameUI uiController;
    public static float noteSpeed = 2;
    public static float offset;
    public static Color[] judgementColors;

    public static AudioStream music;
    public static string chartContent;

	public override void _EnterTree()
    {
        controller = controllerEx;
        uiController = uiControllerEx;
		noteSpeed = noteSpeedEx * 5;
        offset = offsetEx;
        judgementColors = judgementColorsEx;
    }
}
