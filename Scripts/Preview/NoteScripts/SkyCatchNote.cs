using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class SkyCatchNote : Node3D
{
    public int track;
    public List<SpeedEvent> speedEvents;
    public bool isReady, isFake;
    public float time, hitTime;
    public float speed;
    
    private bool a = true, b = true;
    private int currentSpeedIndex;

    public override async void _Ready()
    {
        while (!isReady)
        {
            await Task.Delay(1);
        }

        Position = Position with
        {
            Z = -NoteSettings.controller.GetDistanceToJudgeLine(
                NoteSettings.controller.time,
                hitTime,
                NoteSettings.noteSpeed * speed,
                speedEvents
            )
        };
        
        while (speedEvents[0].endTime < NoteSettings.controller.time)
        {
            speedEvents.RemoveAt(0);
        }
    }
    
    public override void _Process(double delta)
    {
        if(NoteSettings.controller.isPause) return;
        
        time = NoteSettings.controller.time - hitTime;
        
        var noteSpeed = NoteSettings.noteSpeed * speed;
        if (speedEvents.Count > 0)
        {
            var e = speedEvents[0];

            if (NoteSettings.controller.time >= e.endTime)
                speedEvents.RemoveAt(0);
            else if (NoteSettings.controller.time >= e.startTime)
                noteSpeed *= e.speed;
        }
        Position += Vector3.Back * noteSpeed * (float)delta;

        if (!isFake)
        {
            if (time >= 0) Judge();
        }
        else
        {
            if (b & time >= 0.16)
            {
                b = false;
                QueueFree();
            }
        }
    }

    public void Judge()
    {
        NoteSettings.controller.JudgeNote(time, true, new Vector2(GlobalPosition.X, GlobalPosition.Y));
        QueueFree();
    }
}