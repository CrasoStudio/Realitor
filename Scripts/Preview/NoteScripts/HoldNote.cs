using Godot;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

public partial class HoldNote : Node3D
{
    public int track;
    public List<SpeedEvent> speedEvents;
    public bool isReady, isFake;
    public float hitTime, holdTime;
    private float releaseTime;
    public float speed;
    public float bpm;

    private bool a = true, b = true;
    private float time, judgeTime, effectTime;
    private bool isHolding;

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

        Scale = Scale with { Z = speedEvents[0].speed * NoteSettings.noteSpeed * speed * (holdTime - time) };
        effectTime = NoteSettings.controller.time;

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
        if(time < 0) Position += Vector3.Back * noteSpeed * (float)delta;
        else Position = Vector3.Zero;
        
        if (time >= 0) Scale = Scale with { Z = noteSpeed * (holdTime - time) };
        if (holdTime - time <= 0) QueueFree();

        if (!isFake)
        {
            if (time >= 0 & !isHolding) Judge();

            if (!isHolding) return;
            
            if (time >= holdTime)
            {
                NoteSettings.controller.JudgeNote(judgeTime, true,
                    new Vector2(GlobalPosition.X, GlobalPosition.Y));
                isHolding = false;
                QueueFree();
            }

            if (NoteSettings.controller.time - effectTime >= 60 / bpm)
            {
                effectTime = NoteSettings.controller.time;
                NoteSettings.controller.GenEffect(new Vector2(GlobalPosition.X, GlobalPosition.Y), 0, false);
            }
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
        judgeTime = time;
        isHolding = true;
        NoteSettings.controller.GenEffect(new Vector2(GlobalPosition.X, GlobalPosition.Y), 0, true);
    }
}