using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

public partial class GameController : Node3D
{
	[Export] public PackedScene tapNoteObj, holdNoteObj, skyTapObj, skyFlickObj, skyCatchObj, groundTrackObj, skyTrackObj;
    [Export] public PackedScene noteKeyEffectObj;
	[Export] public Node[] mainGroundTracks;
    [Export] public Node skyTracksParent, groundTracksParent;
    [Export] public Texture2D skyTrackIndic;
    [Export] public Camera3D camera;
	[Export] public WorldEnvironment environment;
    [Export] public GpuParticles3D gameParticles;
	[Export] public AudioStreamPlayer musicPlayer;
	[Export] public AudioStreamPlayer keySoundsPlayer;
	[Export] public AudioStream keySound0;
	
    public List<SpeedGroup> speedGroups = [];
    private List<CameraMoveEvent> cameraMoveEvents = [];
    private List<CameraRotateEvent> cameraRotateEvents = [];
    private List<CameraBrightnessEvent> cameraBrightnessEvents = [];
    private List<BPMEvent> BPMEvents = [];

    public float time, BPM;
    private float baseBPM, chartOffset;
    
    public bool isPause, isGameStart;

	public int noteNum, judgedNum;
	
	public override void _Ready()
    {
        Load();
        
        keySoundsPlayer.Stream = keySound0;
    }
    
	public override void _Process(double delta)
    {
        if (!isGameStart) return;
        if (isPause) return;

        if (judgedNum >= noteNum)
        {
            ChangePauseState();
        }
            
        time = musicPlayer.GetPlaybackPosition() - NoteSettings.offset - chartOffset;

        CheckEvents();
            
        if (BPM > 0) gameParticles.SpeedScale = NoteSettings.noteSpeed / 5;
    }
    
    private void CheckEvents()
    {
        if (cameraMoveEvents.Count > 0)
        {
            var moveEvent = cameraMoveEvents[0];
            if (time >= moveEvent.startTime)
            {
                camera.Position = new Vector3(2 * moveEvent.startXPos, 2 * moveEvent.startYPos, 2 * moveEvent.startZPos);
                var tween = camera.CreateTween();
                tween.TweenProperty(camera, "position", new Vector3(2 * moveEvent.endXPos, 2 * moveEvent.endYPos, 2 * moveEvent.endZPos),
                    moveEvent.endTime - moveEvent.startTime).SetEase(Enum.Parse<Tween.EaseType>(moveEvent.easing.easeType.ToString()))
                    .SetTrans(Enum.Parse<Tween.TransitionType>(moveEvent.easing.transType.ToString()));
                cameraMoveEvents.RemoveAt(0);
            }        
        }        
        if (cameraRotateEvents.Count > 0)
        {
            var rotateEvent = cameraRotateEvents[0];
            if (time >= rotateEvent.startTime)
            {
                camera.RotationDegrees = new Vector3(rotateEvent.startXRotate, rotateEvent.startYRotate, rotateEvent.startZRotate);
                var tween = camera.CreateTween();
                tween.TweenProperty(camera, "rotation_degrees", new Vector3(rotateEvent.endXRotate, rotateEvent.endYRotate, rotateEvent.endZRotate),
                        rotateEvent.endTime - rotateEvent.startTime).SetEase(Enum.Parse<Tween.EaseType>(rotateEvent.easing.easeType.ToString()))
                    .SetTrans(Enum.Parse<Tween.TransitionType>(rotateEvent.easing.transType.ToString()));
                cameraRotateEvents.RemoveAt(0);
            }        
        }        
        if (cameraBrightnessEvents.Count > 0)
        {
            var brightnessEvent = cameraBrightnessEvents[0];
            if (time >= brightnessEvent.startTime)
            {
                environment.Environment.AdjustmentBrightness = brightnessEvent.startValue;
                var tween = environment.CreateTween();
                tween.TweenProperty(environment, "environment:adjustment_brightness", brightnessEvent.endValue,
                        brightnessEvent.endTime - brightnessEvent.startTime).SetEase(Enum.Parse<Tween.EaseType>(brightnessEvent.easing.easeType.ToString()))
                    .SetTrans(Enum.Parse<Tween.TransitionType>(brightnessEvent.easing.transType.ToString()));
                cameraBrightnessEvents.RemoveAt(0);
            }
        }        

        if (BPMEvents.Count > 0)
        {
            if (time >= BPMEvents[0].startTime)
            {
                BPM = BPMEvents[0].BPM;
                BPMEvents.RemoveAt(0);
            }
        }
    }
    
    public float CalculateTravelTime(
        List<SpeedEvent> speedEvents,
        float hitTime,
        float distance,
        float baseSpeed // 这里传入 NoteSettings.noteSpeed * note.speed
    )
    {
        float remainingDistance = distance;
        float timeCursor = hitTime;

        // 从后往前倒推
        for (var i = speedEvents.Count - 1; i >= 0; i--)
        {
            var e = speedEvents[i];

            // 如果事件在 hitTime 之后，跳过
            if (e.startTime >= timeCursor) continue;

            // 1. 处理【事件结束】到【当前时间指针】之间的空窗期 (Gap)
            // 倒推时，如果当前指针比事件结束时间晚，说明中间有空窗
            if (timeCursor > e.endTime)
            {
                float gapDuration = timeCursor - e.endTime;
                float gapDistance = gapDuration * baseSpeed * 1.0f;

                if (remainingDistance <= gapDistance)
                {
                    return hitTime - (timeCursor - (remainingDistance / baseSpeed));
                }

                remainingDistance -= gapDistance;
                timeCursor = e.endTime;
            }

            // 2. 处理【事件内部】
            float segStart = e.startTime;
        
            float eventDuration = timeCursor - segStart;
            float eventSpeed = baseSpeed * e.speed;
            float maxDistance = eventDuration * eventSpeed;

            if (remainingDistance <= maxDistance)
            {
                float dt = remainingDistance / eventSpeed;
                return hitTime - (timeCursor - dt);
            }

            remainingDistance -= maxDistance;
            timeCursor = segStart;
        }

        // 3. 处理剩余距离（最早一个事件之前的空窗期）
        float fallbackDt = remainingDistance / baseSpeed;
        return hitTime - (timeCursor - fallbackDt);
    }
    
    public float GetDistanceToJudgeLine(
        float currentTime,
        float hitTime,
        float baseSpeed, 
        List<SpeedEvent> speedEvents
    )
    {
        if (currentTime >= hitTime) return 0f;

        float distance = 0f;
        float timeCursor = currentTime;

        for (var index = 0; index < speedEvents.Count; index++)
        {
            var e = speedEvents[index];

            // 1. 如果事件完全在当前时间之前，跳过
            if (e.endTime <= timeCursor) continue;

            // 2. 如果事件开始时间在 hitTime 之后，说明中间有一段空窗期，处理完空窗期就可以结束了
            if (e.startTime >= hitTime) break;

            // 3. 处理【当前时间指针】到【事件开始】之间的空窗期 (Gap)
            // 如果图谱中这段时间没写事件，默认按 1.0 倍速计算距离
            if (timeCursor < e.startTime)
            {
                float gapEnd = Mathf.Min(hitTime, e.startTime);
                distance += (gapEnd - timeCursor) * 1.0f; 
                timeCursor = gapEnd;
            }

            // 如果填补空窗期后已经到了判定时间，退出
            if (timeCursor >= hitTime) break;

            // 4. 处理【事件内部】的距离
            float segEnd = Mathf.Min(hitTime, e.endTime);
            distance += (segEnd - timeCursor) * e.speed;
            timeCursor = segEnd;
        }

        // 5. 处理【最后一个事件结束】到【hitTime】之间的剩余空窗期
        if (timeCursor < hitTime)
        {
            distance += (hitTime - timeCursor) * 1.0f;
        }

        return distance * baseSpeed;
    }

    public void ChangePauseState()
    {
        isPause = !isPause;
        musicPlayer.StreamPaused = !musicPlayer.StreamPaused;
        gameParticles.SpeedScale = isPause ? 0 : 1;
        NoteSettings.uiController.pausePanel.Visible = !NoteSettings.uiController.pausePanel.Visible;
    }
    
    public void JudgeNote(float hitTime, bool isGreatJudgement, Vector2 pos)
    {
        if (isGreatJudgement)
        {
            GenEffect(pos, 0, true);
        }
        else
        {
            hitTime = Mathf.Max(0, hitTime);
            
            var timer = GetTree().CreateTimer(hitTime);
            timer.Timeout += () => GenEffect(pos, 0, true);
        }

        judgedNum++;
    }
    
    public void MissNote()
    {
        judgedNum++;
    }
    
    public void GenEffect(Vector2 pos, int judgeIndex, bool isPlaySound)
    {
        var key = noteKeyEffectObj.Instantiate<Node3D>();
        GetTree().CurrentScene.AddChild(key);
        key.Position = new Vector3(pos.X, pos.Y, 0);
        ((HitEffect)key).Init(judgeIndex);
        if(isPlaySound) keySoundsPlayer.Play();
    }
    
	private async void Load()
	{
        var chart = NoteSettings.chartContent;
        
        musicPlayer.Stream = NoteSettings.music;
        
		var chartContent = JsonConvert.DeserializeObject<ChartJson>(chart);

        cameraMoveEvents = chartContent.cameraMoveEvents;
        cameraRotateEvents = chartContent.cameraRotateEvents;
        cameraBrightnessEvents = chartContent.cameraBrightnessEvents;
        BPMEvents = chartContent.BPMEvents;

        if (chartContent.speedGroups.Count <= 0)
        {
            var e = new SpeedEvent
            {
                startTime = 0,
                endTime = (float)musicPlayer.Stream.GetLength(),
                speed = 1
            };
            
            chartContent.speedGroups.Add(new SpeedGroup
            {
                id = 0,
            });
            chartContent.speedGroups[0].events.Add(e);
        }

        for (var index = 0; index <= chartContent.speedGroups[^1].id; index++)
        {
            speedGroups.Add( new SpeedGroup { id = index } );
        }

        for (var index = 0; index < chartContent.speedGroups.Count; index++)
        {
            var speedGroup = chartContent.speedGroups[index];
            if (index == speedGroups[index].id)
            {
                speedGroups[index] = speedGroup;
            }
        }

        if (chartContent.groundTracks.Count > 1) chartContent.groundTracks.Sort((t0, t1) => t0.track.CompareTo(t1.track));
        for (var index = 0; index < chartContent.groundTracks.Count; index++)
        {
            var track = chartContent.groundTracks[index];
            track.notes.Sort((note, note1) => note.time.CompareTo(note1.time));
            track.trackTransparencyEvents.Sort((event0, event1) => event0.startTime.CompareTo(event1.startTime));
            track.trackMoveEvents.Sort((event0, event1) => event0.startTime.CompareTo(event1.startTime));
            track.trackRotateEvents.Sort((event0, event1) => event0.startTime.CompareTo(event1.startTime));
                
            if (track.track > 3)
            {
                var trackObj = groundTrackObj.Instantiate<GroundTrackScript>();
                trackObj.notes = track.notes;
                trackObj.track = track.track;
                trackObj.trackMoveEvents = track.trackMoveEvents;
                trackObj.trackRotateEvents = track.trackRotateEvents;
                trackObj.trackTransparencyEvents = track.trackTransparencyEvents;
                trackObj.isReady = true;
                trackObj.isFake = true;
                groundTracksParent.AddChild(trackObj);
            }
            else
            {
                var trackObj = (GroundTrackScript)mainGroundTracks[track.track];
                trackObj.notes = track.notes;
                trackObj.track = track.track;
                trackObj.trackMoveEvents = track.trackMoveEvents;
                trackObj.trackRotateEvents = track.trackRotateEvents;
                trackObj.trackTransparencyEvents = track.trackTransparencyEvents;
                trackObj.isReady = true;
                trackObj.isFake = false;
                noteNum += track.notes.Count;
            }
        }
        
        if (chartContent.skyTracks.Count > 1) chartContent.skyTracks.Sort((t0, t1) => t0.track.CompareTo(t1.track));
        for (var index = 0; index < chartContent.skyTracks.Count; index++)
        {
            var track = chartContent.skyTracks[index];
            track.notes.Sort((note, note1) => note.time.CompareTo(note1.time));
            noteNum += track.notes.Count;

            var trackObj = new SkyTrackScript();
            trackObj.notes = track.notes;
            trackObj.points = track.points;
            trackObj.track = track.track;
            trackObj.Position = Vector3.Zero;
            trackObj.isReady = true;
            skyTracksParent.AddChild(trackObj);
        }

        if (cameraBrightnessEvents.Count > 1) cameraBrightnessEvents.Sort((event0, event1) => event0.startTime.CompareTo(event1.startTime));
        if (cameraMoveEvents.Count > 1) cameraMoveEvents.Sort((event0, event1) => event0.startTime.CompareTo(event1.startTime));
        if (cameraRotateEvents.Count > 1) cameraRotateEvents.Sort((event0, event1) => event0.startTime.CompareTo(event1.startTime)); 
        
        if (BPMEvents.Count > 1) BPMEvents.Sort((event0, event1) => event0.startTime.CompareTo(event1.startTime));

        BPM = chartContent.BPM;
        baseBPM = chartContent.BPM;
        chartOffset = chartContent.offset;

        await Task.Delay(1000);
        isGameStart = true;
        musicPlayer.Play();
    }
}