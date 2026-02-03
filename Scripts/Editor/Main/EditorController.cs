using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;

public partial class EditorController : Control
{
    [ExportGroup("Controllers")] 
    [Export] public TopBar topBar;
    [Export] public ObjEditPanel objEditPanel;
    [Export] public EditArea editArea;
    [Export] public WindowController windowController;

    [ExportGroup("Objects")] 
    [Export] public AudioStreamPlayer musicPlayer;
    [Export] public Node3D previewSceneParent;

    [ExportGroup("Editor Parameters")] 
    [Export] public int editorVersion;
    [Export] public string scenesPath;
    [Export] public string previewSceneName;

    public Types currentlySelectedType;

    public float songTime = 0;
    public float bpm = 120, offset = 0, beatScale = 1, musicSpeed = 1;
    public int beatSeg = 4, noteEditSeg = 4;
    public bool isPlaying, isLoaded;
    public List<BPMEvent> bpmEvents = [];
    public AudioStream music;
    
    public List<float> cachedBeatTimes = [];

    public static EditorController instance;

    public override void _EnterTree()
    {
        instance = this;
    }

    public override void _Ready()
    {
        bpmEvents.Add(new BPMEvent { BPM = bpm, startTime = 0 });

        musicPlayer.Finished += () =>
        {
            musicPlayer.Stop();
            isPlaying = false;
        };
    }

    public override void _Process(double delta)
    {
        if (!isLoaded) return;

        if (isPlaying)
        {
            topBar.musicTimeSlider.SetValueNoSignal(musicPlayer.GetPlaybackPosition());
            songTime = musicPlayer.GetPlaybackPosition();
        }
    }
    
    public void LoadMusic()
    {
        var fileDialog = new FileDialog();
        AddChild(fileDialog);
        fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        fileDialog.Access = FileDialog.AccessEnum.Filesystem;
        fileDialog.UseNativeDialog = true;
        fileDialog.AddFilter("*.wav;音频文件");
        fileDialog.Title = "加载音频文件";
        fileDialog.FileSelected += OnMusicSelected;
        fileDialog.PopupCentered();

        void OnMusicSelected(string path)
        {
            music = AudioStreamWav.LoadFromFile(path);
            musicPlayer.Stream = music;
            topBar.musicTimeSlider.MaxValue = music.GetLength();
            topBar.loadMusicButton.Disabled = true;
            editArea.ReloadEditArea();
            fileDialog.QueueFree();
            isLoaded = true;
        }
    }
    
    public void LoadChart()
    {
        var fileDialog = new FileDialog();
        AddChild(fileDialog);
        fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        fileDialog.Access = FileDialog.AccessEnum.Filesystem;
        fileDialog.UseNativeDialog = true;
        fileDialog.AddFilter("*.json;谱面文件");
        fileDialog.Title = "加载谱面文件";
        fileDialog.FileSelected += OnChartSelected;
        fileDialog.PopupCentered();
        void OnChartSelected(string path)
        {
            var chartContent = File.OpenText(path).ReadToEnd();
            var chart = JsonConvert.DeserializeObject<ChartJson>(chartContent);

            // 1. 基本参数还原
            bpm = chart.BPM;
            offset = chart.offset;
            bpmEvents = chart.BPMEvents;

            // 2. 清理当前编辑器内容
            ClearEditor();

            // 3. 还原地面轨道音符 (Ground Notes)
            foreach (var groundTrack in chart.groundTracks)
            {
                foreach (var noteData in groundTrack.notes)
                {
                    var noteObj = editArea.noteObj.Instantiate<NoteObj>();
                    // 注意：这里需要将秒(Time)转回绝对拍数(Beat)供编辑器显示
                    noteObj.time = GetBeatFromTime(noteData.time, bpmEvents);
                    noteObj.duration = GetBeatFromTime(noteData.time + noteData.duration, bpmEvents) - noteObj.time;
                    noteObj.track = groundTrack.track;
                
                    // 初始化类型
                    noteObj.Init((Types)noteData.type);
                
                    editArea.notes.Add(noteObj);
                    editArea.notesContent.AddChild(noteObj);

                    if (!editArea.groundTrackIDs.Contains(noteObj.track))
                        editArea.groundTrackIDs.Add(noteObj.track);
                }
            }

            // 4. 还原天空轨道 (Sky Tracks)
            foreach (var skyTrackData in chart.skyTracks)
            {
                var trackID = skyTrackData.track;
                var trackObj = new SkyTrackObj { track = trackID };
                editArea.skyTrackObjs.Add(trackObj);
                editArea.skyTracks.Add(trackID, new List<SkyTrackNodeObj>());

                // 还原轨道上的节点 (Nodes)
                foreach (var point in skyTrackData.points)
                {
                    var node = editArea.skyTrackNodeObj.Instantiate<SkyTrackNodeObj>();
                    node.time = GetBeatFromTime(point.time, bpmEvents);
                    node.x = point.x;
                    node.track = trackID;
                    
                    editArea.skyTracks[trackID].Add(node);
                    trackObj.AddChild(node);
                }

                // 还原轨道上的天空音符 (Sky Notes)
                foreach (var skyNoteData in skyTrackData.notes)
                {
                    var noteObj = editArea.noteObj.Instantiate<NoteObj>();
                    noteObj.time = GetBeatFromTime(skyNoteData.time, bpmEvents);
                    noteObj.track = trackID;
                
                    // 根据 JSON 定义还原类型 (JSON 中 SkyTap=0, 但 Types 枚举中 SkyTap=3)
                    noteObj.Init((Types)(skyNoteData.type + 3)); 
                
                    editArea.notes.Add(noteObj);
                    editArea.notesContent.AddChild(noteObj);
                }
                
                editArea.notesContent.AddChild(trackObj);
            }

            // 5. 刷新 UI 和显示
            topBar.musicTimeSlider.MaxValue = music?.GetLength() ?? 100;
            topBar.SyncEditText();
            editArea.ReloadEditArea();
        
            isLoaded = true;
            fileDialog.QueueFree();
        }
    }

    private void ClearEditor()
    {
        foreach (var note in editArea.notes) note.QueueFree();
        editArea.notes.Clear();

        foreach (var stObj in editArea.skyTrackObjs) stObj.QueueFree();
        editArea.skyTrackObjs.Clear();
        editArea.skyTracks.Clear();
    
        editArea.groundTrackIDs.Clear();
        editArea.currentlySelectedSkyTrackNode = null;
    }

    public void SaveChart()
    {
        var fileDialog = new FileDialog();
        AddChild(fileDialog);
        fileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
        fileDialog.Access = FileDialog.AccessEnum.Filesystem;
        fileDialog.UseNativeDialog = true;
        fileDialog.AddFilter("*.json;谱面文件");
        fileDialog.Title = "保存谱面文件";
        fileDialog.FileSelected += path =>
        {
            var chart = GetChartContent();
            var chartContent = JsonConvert.SerializeObject(chart);
            File.WriteAllText(path, chartContent);
            fileDialog.QueueFree();
        };
        fileDialog.PopupCentered();
    }

    private ChartJson GetChartContent()
    {
        var chart = new ChartJson();
        chart.BPM = bpm;
        chart.offset = offset;
        chart.version = editorVersion;
        chart.BPMEvents = bpmEvents;
            
        
        foreach (var id in editArea.groundTrackIDs)
        {
            var groundTrack = new GroundTrack();
            groundTrack.track = id;
            foreach (var note in editArea.notes)
            {
                if (note.track != -1 && note.track == id)
                {
                    var notec = new Note
                    {
                        time = GetTimeFromBeat(note.time, bpmEvents),
                        duration = GetTimeFromBeat(note.duration, bpmEvents),
                        type = (int)note.thisNoteType
                    };
                    groundTrack.notes.Add(notec);
                }
            }
            chart.groundTracks.Add(groundTrack);
        }

        foreach (var skyTrack in editArea.skyTrackObjs)
        {
            var skyTrackC = new SkyTrack();
            skyTrackC.track = skyTrack.track;
            foreach (var node in skyTrack.nodes)
            {
                skyTrackC.points.Add(new TrackPoint
                {
                    time = GetTimeFromBeat(node.time, bpmEvents),
                    x = node.x
                });
            }
            foreach (var note in editArea.notes)
            {
                if (note.thisNoteType != Types.TapNote && note.thisNoteType != Types.HoldNote &&
                    note.track == skyTrack.track)
                {
                    var noteC = new SkyNote
                    {
                        time = GetTimeFromBeat(note.time, bpmEvents),
                        type = (int)note.thisNoteType - 4
                    };
                    skyTrackC.notes.Add(noteC);
                }
            }
            chart.skyTracks.Add(skyTrackC);
        }
        return chart;
    }
    
    public void PreviewChart()
    {
        var chart = GetChartContent();
        var chartContent = JsonConvert.SerializeObject(chart);
        NoteSettings.chartContent = chartContent;
        NoteSettings.music = music;
        var scene = ResourceLoader.Load<PackedScene>($"{scenesPath}/{previewSceneName}.tscn").Instantiate();
        previewSceneParent.AddChild(scene);
    }
    
    public static Control GetNearestObj(Vector2 pos, IEnumerable<Node> targets)
    {
        Control nearest = null;
        var minDistSq = float.MaxValue;

        foreach (var t in targets)
        {
            if (!IsInstanceValid(t)) continue;

            if (t is Control c)
            {
                var d = pos.DistanceSquaredTo(c.GlobalPosition);
                if (d < minDistSq)
                {
                    minDistSq = d;
                    nearest = c;
                }
            }
        }
        return nearest;
    }
    
    public static List<float> GenerateBeatTimes(List<BPMEvent> bpmEvents, float songLength, int segment)
    {
        var beatTimes = new List<float>();
        
        // 确保BPM列表按时间排序
        bpmEvents.Sort((a, b) => a.startTime.CompareTo(b.startTime));

        // 遍历每一个BPM区间
        for (var i = 0; i < bpmEvents.Count; i++)
        {
            var currentEvent = bpmEvents[i];
            
            // 确定当前BPM区间的结束时间
            // 如果是最后一个事件，结束时间就是歌曲总长度
            // 否则，结束时间是下一个BPM事件的开始时间
            var endTime = (i == bpmEvents.Count - 1) ? songLength : bpmEvents[i + 1].startTime;

            // 计算当前BPM下的单个节拍间隔（秒）
            // 公式：(60 / BPM) * (4 / 细分)
            var secondsPerBeat = (60.0f / currentEvent.BPM) * (1f / segment);

            // 从当前事件的开始时间起，累加时间生成线条
            var currentTime = currentEvent.startTime;

            // 这里的 Epsilon 用于防止浮点数精度问题导致的重叠或遗漏
            while (currentTime < endTime - 0.0001) 
            {
                beatTimes.Add(currentTime);
                currentTime += secondsPerBeat;
            }
        }

        return beatTimes;
    }
    
    /// <summary>
    /// 根据当前时间计算绝对拍数 (Beat Index)
    /// </summary>
    /// <param name="currTime">当前播放时间（秒）</param>
    /// <param name="bpmEvents">BPM事件列表</param>
    /// <returns>返回节拍数（例如 2.25 代表第2拍又过了0.25拍）</returns>
    public static float GetBeatFromTime(float currTime, List<BPMEvent> bpmEvents)
    {
        if (bpmEvents == null || bpmEvents.Count == 0) return 0;
 
        bpmEvents.Sort((a, b) => a.startTime.CompareTo(b.startTime)); 

        float totalBeats = 0;

        for (var i = 0; i < bpmEvents.Count; i++)
        {
            var currentEvent = bpmEvents[i];
        
            // 判断下一条 BPM 事件的时间
            // 如果没有下一个事件（即当前是最后一个），则下一条时间视为无穷大（或当前时间）
            var nextTime = (i == bpmEvents.Count - 1) ? float.MaxValue : bpmEvents[i + 1].startTime;

            // 如果当前时间已经超过了这一段的结束时间，说明这一整段都过完了
            if (currTime >= nextTime)
            {
                // 这一段的总时长
                var duration = nextTime - currentEvent.startTime;
                // 累加这一段产生的拍数：时长 * (BPM / 60)
                totalBeats += duration * (currentEvent.BPM / 60.0f);
            }
            else
            {
                // 说明当前时间落在了这一段 BPM 区间内
                // 计算从这一段开始到当前时间经过了多久
                var duration = currTime - currentEvent.startTime;
                // 累加这部分的拍数并直接返回
                // 注意：要处理 duration < 0 的情况（比如时间在第一条BPM之前，虽然理论上要有0时刻的BPM）
                if (duration > 0)
                {
                    totalBeats += duration * (currentEvent.BPM / 60.0f);
                }
                break; // 找到了当前时间点，循环结束
            }
        }

        return totalBeats + 1;
    }
    
    /// <summary>
    /// 根据绝对拍数计算对应的时间（秒）
    /// </summary>
    /// <param name="targetBeat">目标拍数（例如 4.0 代表第4拍，4.5 代表第4拍半）</param>
    /// <param name="bpmEvents">BPM事件列表</param>
    /// <returns>时间（秒）</returns>
    public static float GetTimeFromBeat(float targetBeat, List<BPMEvent> bpmEvents)
    {
        targetBeat -= 1;
        
        if (bpmEvents == null || bpmEvents.Count == 0) return 0;
        if (targetBeat <= 0) return 0; // 负数拍直接归零

        // 1. 确保按时间排序
        bpmEvents.Sort((a, b) => a.startTime.CompareTo(b.startTime));

        float accumulatedBeats = 0; // 当前累积经过了多少拍

        for (var i = 0; i < bpmEvents.Count; i++)
        {
            var currentEvent = bpmEvents[i];
        
            // 获取下一段BPM的时间点，如果是最后一个事件，则设为无穷大
            var nextTime = (i == bpmEvents.Count - 1) ? float.MaxValue : bpmEvents[i + 1].startTime;
        
            // 计算当前这段 BPM 持续了多久（秒）
            var duration = nextTime - currentEvent.startTime;

            // 计算这段时间内包含了多少拍
            // 公式：时间 * (BPM / 60)
            // 注意：如果是最后一段（duration是无穷大），beatsInThisSection 也是无穷大
            var beatsInThisSection = (i == bpmEvents.Count - 1) 
                ? float.MaxValue 
                : duration * (currentEvent.BPM / 60.0f);

            // 检查：目标拍数是否在这一段区间内？
            // 如果 (累积拍数 + 这一段的拍数) 超过了 目标拍数，说明目标就在这儿
            if (accumulatedBeats + beatsInThisSection >= targetBeat)
            {
                // 算出还需要多少拍（目标 - 已累积）
                var remainingBeats = targetBeat - accumulatedBeats;
            
                // 将剩余拍数转为时间
                // 公式：拍数 * (60 / BPM)
                var timeInThisSection = remainingBeats * (60.0f / currentEvent.BPM);

                // 结果 = 这段BPM的开始时间 + 偏移时间
                return currentEvent.startTime + timeInThisSection;
            }

            // 如果还没到目标拍数，就累加拍数，继续下一段
            accumulatedBeats += beatsInThisSection;
        }

        return 0;
    }
    
    public enum Types
    {
        TapNote,
        HoldNote,
        SkyTrack,
        SkyTapNote,
        SkyFlickNote,
        SkyCatchNote
    }
}