using Godot;
using System;

public partial class BeatLine : ColorRect
{
    [Export] public Label beatIndexLabel;
    public int index;
    public float timeSec;

    public void RefreshState(int newIndex, float newTimeSec, int beatSeg)
    {
        index = newIndex;
        timeSec = newTimeSec;
        
        if (index % beatSeg == 0)
        {
            Color = Colors.White;
            beatIndexLabel.Text = ((index + beatSeg) / beatSeg).ToString();
        }
        else
        {
            Color = Color.Color8(150, 150, 150);
            beatIndexLabel.Text = "";
        }
    }
}
