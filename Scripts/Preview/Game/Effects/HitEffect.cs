using Godot;

public partial class HitEffect : Node3D
{
    [Export] public AnimationPlayer animPlayer;
    [Export] public Label3D judgementLabel;

    public void Init(int judgeIndex)
    {
        switch (judgeIndex)
        {
            case 0:
                judgementLabel.Text = "OPTIMUM+";
                break;
            case 1:
                judgementLabel.Text = "OPTIMUM";
                break;
            case 2:
                judgementLabel.Text = "PASS";
                break;
        }

        judgementLabel.Modulate = NoteSettings.judgementColors[judgeIndex];
        
        animPlayer.Play("HitAnimation");
        animPlayer.AnimationFinished += _ => QueueFree();
    }
}
