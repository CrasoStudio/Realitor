using System;
using System.Collections.Generic;
using Godot;
using System.Threading.Tasks;

public partial class GroundTrackScript : MeshInstance3D
{
    public int track;
    public List<Note> notes = [];
    
    public List<TrackMoveEvent> trackMoveEvents = [];
    public List<TrackRotateEvent> trackRotateEvents = [];
    public List<TrackTransparencyEvent> trackTransparencyEvents = [];
    
    public bool isReady, isFake;

    public override async void _Ready()
    {
        while (!isReady)
        {
            await Task.Delay(1);
        }
    }

    public override void _Process(double delta)
    {
        if(!NoteSettings.controller.isGameStart) return;
        if(NoteSettings.controller.isPause) return;
        
        CheckNote();
        CheckEvent();
    }
    
    private void CheckNote()
	{
        if (notes.Count <= 0 || notes == null) return;

        var noteDuration = NoteSettings.controller.CalculateTravelTime(
            NoteSettings.controller.speedGroups[notes[0].speedGroupID].events, 
            notes[0].time, 
            50f,
            NoteSettings.noteSpeed * notes[0].speed);

        if (NoteSettings.controller.time >= notes[0].time - noteDuration)
        {
            Node3D note;
            switch (notes[0].type)
            {
                case 0:
                    note = NoteSettings.controller.tapNoteObj.Instantiate<Node3D>();
                    if (note is TapNote t)
                    {
                        t.hitTime = notes[0].time;
                        t.speedEvents =
                            new List<SpeedEvent>(NoteSettings.controller.speedGroups[notes[0].speedGroupID].events);
                        t.speed = notes[0].speed;
                        t.track = track;
                        t.isFake = isFake;
                        t.isReady = true;
                    }
                    break;
                case 1:
                    note = NoteSettings.controller.holdNoteObj.Instantiate<Node3D>();
                    if (note is HoldNote h)
                    {
                        h.hitTime = notes[0].time;
                        h.holdTime = notes[0].duration;
                        h.speedEvents =
                            new List<SpeedEvent>(NoteSettings.controller.speedGroups[notes[0].speedGroupID].events);
                        h.speed = notes[0].speed;
                        h.bpm = NoteSettings.controller.BPM;
                        h.track = track;
                        h.isFake = isFake;
                        h.isReady = true;
                    }
                    break;
                default:
                    note = new Node3D();
                    break;
            }

            note.Position = note.Position with { Y = 0.01f, Z = -50 };
            AddChild(note);
            
            notes.RemoveAt(0);
        }
    }

    public void CheckEvent()
    { 
        if (trackMoveEvents.Count > 0)
        {
            var moveEvent = trackMoveEvents[0];
            if (NoteSettings.controller.time >= moveEvent.startTime)
            {
                Position = new Vector3(2 * moveEvent.startXPos, moveEvent.startYPos, 2 * moveEvent.startZPos);
                var tween = CreateTween();
                tween.TweenProperty(this, "position", new Vector3(2 * moveEvent.endXPos, moveEvent.endYPos, 2 * moveEvent.endZPos),
                    moveEvent.endTime - moveEvent.startTime).SetEase(Enum.Parse<Tween.EaseType>(moveEvent.easing.easeType.ToString()))
                    .SetTrans(Enum.Parse<Tween.TransitionType>(moveEvent.easing.transType.ToString()));
                trackMoveEvents.RemoveAt(0);
            }        
        }        
        if (trackRotateEvents.Count > 0)
        {
            var rotateEvent = trackRotateEvents[0];
            if (NoteSettings.controller.time >= rotateEvent.startTime)
            {
                RotationDegrees = new Vector3(rotateEvent.startXRotate, rotateEvent.startYRotate, rotateEvent.startZRotate);
                var tween = CreateTween();
                tween.TweenProperty(this, "rotation_degrees", new Vector3(rotateEvent.endXRotate, rotateEvent.endYRotate, rotateEvent.endZRotate),
                        rotateEvent.endTime - rotateEvent.startTime).SetEase(Enum.Parse<Tween.EaseType>(rotateEvent.easing.easeType.ToString()))
                    .SetTrans(Enum.Parse<Tween.TransitionType>(rotateEvent.easing.transType.ToString()));
                trackRotateEvents.RemoveAt(0);
            }        
        }        
        if (trackTransparencyEvents.Count > 0)
        {
            var transparencyEvent = trackTransparencyEvents[0];
            if (NoteSettings.controller.time >= transparencyEvent.startTime)
            {
                Transparency = transparencyEvent.startValue;
                var tween = CreateTween();
                tween.TweenProperty(this, "transparency", transparencyEvent.endValue,
                        transparencyEvent.endTime - transparencyEvent.startTime).SetEase(Enum.Parse<Tween.EaseType>(transparencyEvent.easing.easeType.ToString()))
                    .SetTrans(Enum.Parse<Tween.TransitionType>(transparencyEvent.easing.transType.ToString()));
                trackTransparencyEvents.RemoveAt(0);
            }
        }
    }
}
