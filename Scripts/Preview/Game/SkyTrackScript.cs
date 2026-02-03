using System.Collections.Generic;
using Godot;
using System.Threading.Tasks;

public partial class SkyTrackScript : Node3D
{
    public int track;
    public List<SkyNote> notes = [];
    public List<TrackPoint> points = [];
    public bool isReady;
    
    private List<float> notesDurations = [];
    private List<SpeedEvent> noteSpeedEvents = [];
    private Sprite3D indic;

    private Line3D line;

    public override async void _Ready()
    {
        while (!isReady)
        {
            await Task.Delay(1);
        }

        indic = new Sprite3D();
        indic.Texture = NoteSettings.controller.skyTrackIndic;
        indic.Scale = new Vector3(0.15f, 0.15f, 0.15f);
        indic.RenderPriority = 21;
        AddChild(indic);
        
        for (var index = 0; index < points.Count; index++)
        {
            var point = points[index];
            var speedEvent = new SpeedEvent();

            speedEvent.startTime = point.time;
            if (index != points.Count - 1) speedEvent.endTime = points[index + 1].time;
            else speedEvent.endTime = (float)NoteSettings.controller.musicPlayer.Stream.GetLength();
            speedEvent.speed = point.speed;
            
            noteSpeedEvents.Add(speedEvent);
        }

        for (var index = 0; index < notes.Count; index++)
        {
            var note = notes[index];
            var spawnAdvanceTime = NoteSettings.controller.CalculateTravelTime(
                noteSpeedEvents, 
                note.time, 
                50f,
                NoteSettings.noteSpeed);
            notesDurations.Add(spawnAdvanceTime);
        }

        line = NoteSettings.controller.skyTrackObj.Instantiate<Line3D>();
        
        line.Points.Add(new Vector3(points[0].x, points[0].y, 0));
        for (var index = 0; index < points.Count; index++)
        {
            var point = points[index];
            if (index != points.Count - 1)
            {
                line.Points.Add(new Vector3(2 * point.x, 1 + point.y, 
                    line.Points[^1].Z - (points[index + 1].time - point.time) * point.speed * NoteSettings.noteSpeed));
            }
            else
            {
                line.Points.Add(new Vector3(2 * point.x, 1 + point.y, 
                    line.Points[^1].Z - ((float)NoteSettings.controller.musicPlayer.Stream.GetLength() - point.time) * point.speed * NoteSettings.noteSpeed));
            }
        }
        
        AddChild(line);
    }

    public override void _Process(double delta)
    {
        if(NoteSettings.controller.isPause) return;
        
        var noteSpeed = NoteSettings.noteSpeed;
        if (points.Count > 0)
        {
            var e = points[0];

            if (points.Count != 1)
            {
                if (NoteSettings.controller.time >= points[1].time)
                {
                    points.RemoveAt(0);
                }
                else if (NoteSettings.controller.time >= e.time)
                    noteSpeed *= e.speed;
            }
            else
            {
                if (NoteSettings.controller.time >= (float)NoteSettings.controller.musicPlayer.Stream.GetLength())
                {
                    points.RemoveAt(0);
                }
                else if (NoteSettings.controller.time >= e.time)
                    noteSpeed *= e.speed;
            }
        }
        Position += Vector3.Back * noteSpeed * (float)delta;

        var p = line.GetPositionFromZ(-Position.Z);
        var pos = Vector2.Zero;
        if (p.HasValue) pos = p.Value;
        indic.Position = new Vector3(pos.X, pos.Y, -Position.Z);
        
        CheckSkyNote();
    }
    
    private void CheckSkyNote()
	{
        if (notes.Count <= 0 || notes == null) return;
    
        var currentTime = notes[0].time;
        
		var skyNoteDuration = notesDurations[0];

        if (NoteSettings.controller.time >= currentTime - skyNoteDuration)
        {
            Node3D skyNote;
            switch (notes[0].type)
            {
                case 0:
                    skyNote = NoteSettings.controller.skyTapObj.Instantiate<Node3D>();
                    if (skyNote is SkyTapNote t)
                    {
                        t.hitTime = notes[0].time;
                        t.speedEvents = noteSpeedEvents;
                        t.speed = 1;
                        t.isReady = true;
                    }

                    break;
                case 1:
                    skyNote = NoteSettings.controller.skyFlickObj.Instantiate<Node3D>();
                    if (skyNote is SkyFlickNote f)
                    {
                        f.hitTime = notes[0].time;
                        f.speedEvents = noteSpeedEvents;
                        f.speed = 1;
                        f.isReady = true;
                    }

                    break;
                default:
                    skyNote = new Node3D();
                    break;
            }
            AddChild(skyNote);
            
            var xy = line.GetPositionFromZ(-(Position.Z + NoteSettings.controller.GetDistanceToJudgeLine(NoteSettings.controller.time, notes[0].time, NoteSettings.noteSpeed, noteSpeedEvents)));
            if(xy is not { } pos) return;
            skyNote.TopLevel = true;
            skyNote.GlobalPosition = line.ToGlobal(new Vector3(pos.X, pos.Y, 0)) with
            {
                Z = -NoteSettings.controller.GetDistanceToJudgeLine(NoteSettings.controller.time, notes[0].time, NoteSettings.noteSpeed, noteSpeedEvents)
            };
            
            notes.RemoveAt(0);
            notesDurations.RemoveAt(0);
        }
    }
}
