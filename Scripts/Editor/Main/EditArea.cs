using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class EditArea : ColorRect
{
    [Export] public PackedScene beatLineObj, snapLineObj, noteObj, skyTrackNodeObj;
    [Export] public Control notesContent, eventsContent, groundNotesSnapLineParent,
        skyNotesSnapLineParent, eventsSnapLineParent, judgeLine, judgeLineEvent;
    [Export] public Control notesViewport, eventsViewport;
    [Export] public float PixelsPerSecond = 10000f;
    [Export] public float ScrollSensitivity = 0.25f;

    public bool scrollable = true, placeable = true;
    
    public List<BeatLine> _poolN = [];
    public List<BeatLine> _poolE = [];

    public List<NoteObj> notes = [];
    public List<SkyTrackObj> skyTrackObjs = [];
    public Dictionary<int, List<SkyTrackNodeObj>> skyTracks = [];

    public SkyTrackNodeObj currentlySelectedSkyTrackNode;

    public List<int> groundTrackIDs = [];

    public Vector2 startPos;
    
    private int maxSkyTrackID = -1;
    
    private Vector2 _notesContentOrigin;
    private Vector2 _eventContentOrigin;

    public override void _Ready()
    {
        _notesContentOrigin = notesContent.Position;
        _eventContentOrigin = eventsContent.Position;

        startPos = _notesContentOrigin + new Vector2(0, notesContent.Size.Y);
    }

    public void ReloadEditArea()
    {
        if (EditorController.instance.music != null)
        {
            EditorController.instance.cachedBeatTimes = EditorController.GenerateBeatTimes(
                EditorController.instance.bpmEvents,
                (float)EditorController.instance.music.GetLength() * (1 / EditorController.instance.musicSpeed),
                EditorController.instance.beatSeg
            );
        }

        notesContent.Position = _notesContentOrigin;
        eventsContent.Position = _eventContentOrigin;

        foreach (var child in skyNotesSnapLineParent.GetChildren()) child.QueueFree();

        skyNotesSnapLineParent.AddThemeConstantOverride("separation",
            (int)(skyNotesSnapLineParent.Size.X / (1 + EditorController.instance.noteEditSeg)));
        for (var i = 0; i < EditorController.instance.noteEditSeg; i++)
        {
            var line = snapLineObj.Instantiate<SnapLine>();
            line.index = i;
            skyNotesSnapLineParent.AddChild(line);
        }

        foreach (var note in notes) note.Update();
        
        for (var index = 0; index < skyTracks.Count; index++)
        {
            var track = skyTracks[index];
            foreach (var obj in track)
            {
                obj.Update();
            }
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!EditorController.instance.isLoaded) return;
        
        if (@event is InputEventMouseButton { Pressed: true } mb)
        {
            if(!scrollable) return;
            
            if (mb.ButtonIndex == MouseButton.WheelUp)
                ModifySongTime(ScrollSensitivity); 
            else if (mb.ButtonIndex == MouseButton.WheelDown)
                ModifySongTime(-ScrollSensitivity);
            else if (mb.ButtonIndex == MouseButton.Right)
                currentlySelectedSkyTrackNode = null;
        }
    }

    private void ModifySongTime(float amount)
    {
        var newTime = EditorController.instance.songTime + amount;
        var maxTime = (float)EditorController.instance.music.GetLength();
        EditorController.instance.songTime = Mathf.Clamp(newTime, 0, maxTime);
        
        if (EditorController.instance.isPlaying)
        {
            EditorController.instance.musicPlayer.Seek(EditorController.instance.songTime);
        }
    }

    public override void _Process(double delta)
    {
        if (!EditorController.instance.isLoaded) return;
        
        var currentScrollOffset = EditorController.instance.songTime * (PixelsPerSecond * EditorController.instance.beatScale);
        notesContent.Position = new Vector2(_notesContentOrigin.X, _notesContentOrigin.Y + currentScrollOffset);
        eventsContent.Position = new Vector2(_eventContentOrigin.X, _eventContentOrigin.Y + currentScrollOffset);

        if (EditorController.instance.topBar != null && EditorController.instance.topBar.musicTimeSlider != null)
        {
            EditorController.instance.topBar.musicTimeSlider.SetValueNoSignal(EditorController.instance.songTime);
        }

        UpdateVisibleLines();
        
        if(placeable) CheckEditInput();
    }
    
    private void CheckEditInput()
    {
        if (notesViewport.GetRect().HasPoint(notesViewport.GetLocalMousePosition()) 
            && Input.IsActionJustPressed("ui_mouse_press"))
        {
            if (EditorController.instance.currentlySelectedType != EditorController.Types.SkyTrack)
            {
                var note = noteObj.Instantiate<NoteObj>();
                note.Init(EditorController.instance.currentlySelectedType);
                notes.Add(note);
                notesContent.AddChild(note);

                if (note.thisNoteType == EditorController.Types.TapNote |
                    note.thisNoteType == EditorController.Types.HoldNote)
                {
                    var nearestSnapLine =
                        EditorController.GetNearestObj(GetGlobalMousePosition(),
                            groundNotesSnapLineParent.GetChildren());
                    var nearestBeatLine = EditorController.GetNearestObj(GetGlobalMousePosition(), _poolN);

                    note.GlobalPosition = new Vector2(nearestSnapLine.GlobalPosition.X - note.Size.X / 2,
                        nearestBeatLine.GlobalPosition.Y - note.Size.Y / 2);
                    note.track = ((SnapLine)nearestSnapLine).index;
                    note.time = EditorController.GetBeatFromTime(((BeatLine)nearestBeatLine).timeSec,
                        EditorController.instance.bpmEvents);
                    
                    if(!groundTrackIDs.Contains(note.track)) groundTrackIDs.Add(note.track);
                    if(groundTrackIDs.Count > 1) groundTrackIDs.Sort((t, t1) => t.CompareTo(t1));
                }
                else
                {
                    if(skyTrackObjs.Count == 0) return;
                    List<SkyTrackNodeObj> nodes = [];
                    foreach (var child in skyTrackObjs.SelectMany(track => track.GetChildren()))
                        if (child is SkyTrackNodeObj node) nodes.Add(node);

                    var nearestSkyTrackNode = EditorController.GetNearestObj(GetGlobalMousePosition(),
                        nodes);
                    var nearestBeatLine = EditorController.GetNearestObj(GetGlobalMousePosition(), _poolN);
                    var stLine = skyTrackObjs[((SkyTrackNodeObj)nearestSkyTrackNode).track].line;

                    note.GlobalPosition = new Vector2(
                        stLine.ToGlobal(stLine.GetPositionFromY(stLine
                            .ToLocal(new Vector2(0, nearestBeatLine.GlobalPosition.Y - note.Size.Y / 2)).Y)).X -
                        note.Size.X / 2,
                        nearestBeatLine.GlobalPosition.Y - note.Size.Y / 2);
                    note.track = ((SkyTrackNodeObj)nearestSkyTrackNode).track;
                    note.time = EditorController.GetBeatFromTime(((BeatLine)nearestBeatLine).timeSec,
                        EditorController.instance.bpmEvents);
                }
            }
            else
            {
                var node = skyTrackNodeObj.Instantiate<SkyTrackNodeObj>();

                int id;

                if (currentlySelectedSkyTrackNode != null)
                {
                    id = currentlySelectedSkyTrackNode.track;
                    skyTrackObjs[id].AddChild(node);
                    skyTracks[id].Add(node);
                }
                else
                {
                    id = maxSkyTrackID + 1;
                    skyTracks.Add(id, []);
                    maxSkyTrackID = id;
                    skyTracks[id].Add(node);

                    var trackObj = new SkyTrackObj
                    {
                        track = id,
                    };
                    notesContent.AddChild(trackObj);
                    skyTrackObjs.Add(trackObj);
                    trackObj.AddChild(node);
                }
                
                if (skyTracks[id].Count > 1) skyTracks[id].Sort((a, b) =>
                    b.GlobalPosition.Y.CompareTo(a.GlobalPosition.Y));
                
                if (EditorController.instance.noteEditSeg != 0)
                {
                    var nearestSnapLine = EditorController.GetNearestObj(GetGlobalMousePosition(),
                        skyNotesSnapLineParent.GetChildren());
                    var nearestBeatLine = EditorController.GetNearestObj(GetGlobalMousePosition(), _poolN);

                    node.GlobalPosition = new Vector2(nearestSnapLine.GlobalPosition.X - node.Size.X / 2,
                        nearestBeatLine.GlobalPosition.Y - node.Size.Y / 2);
                    node.track = id;
                    node.x = -1 + 2f / (EditorController.instance.noteEditSeg + 1) *
                        (((SnapLine)nearestSnapLine).index + 1);
                    node.time = EditorController.GetBeatFromTime(((BeatLine)nearestBeatLine).timeSec,
                        EditorController.instance.bpmEvents);
                }
                else
                {
                    var nearestBeatLine = EditorController.GetNearestObj(GetGlobalMousePosition(), _poolN);

                    node.GlobalPosition = new Vector2(GetGlobalMousePosition().X - node.Size.X / 2,
                        nearestBeatLine.GlobalPosition.Y - node.Size.Y / 2);
                    node.track = id;
                    node.x = -1 + (node.GlobalPosition.X + node.Size.X / 2) * 2 / 862;
                    node.time = EditorController.GetBeatFromTime(((BeatLine)nearestBeatLine).timeSec,
                        EditorController.instance.bpmEvents);
                }

                currentlySelectedSkyTrackNode = node;
            }
            notes.Sort((obj, obj1) => obj.time.CompareTo(obj1.time));
            skyTrackObjs.Sort((obj, obj1) => obj.track.CompareTo(obj1.track));
            skyTracks = new Dictionary<int, List<SkyTrackNodeObj>>(skyTracks.OrderBy(pair => pair.Key));
        }
    }
    
    private void UpdateVisibleLines()
    {
        var beatTimes = EditorController.instance.cachedBeatTimes;
        if (beatTimes == null || beatTimes.Count == 0) return;

        var speed = PixelsPerSecond * EditorController.instance.beatScale;
        var offsetTime = EditorController.instance.offset / 1000f;
        
        var viewHeight = Size.Y;
        
        var judgeY = judgeLine.Position.Y;
        
        var maxVisibleTime = EditorController.instance.songTime + (judgeY / speed) + offsetTime;
        
        var minVisibleTime = EditorController.instance.songTime - ((viewHeight - judgeY) / speed) + offsetTime;

        var bufferTime = 1.0f; 
        var renderStart = minVisibleTime - bufferTime;
        var renderEnd = maxVisibleTime + bufferTime;

        var startIndex = beatTimes.BinarySearch(renderStart);
        if (startIndex < 0) startIndex = ~startIndex;

        var poolIndex = 0;
        
        for (var i = startIndex; i < beatTimes.Count; i++)
        {
            var t = beatTimes[i];
            if (t > renderEnd) break; // 超出屏幕上方，停止遍历

            var lineN = GetLineFromPool(_poolN, poolIndex, notesContent);
            var lineE = GetLineFromPool(_poolE, poolIndex, eventsContent);
            
            var yPosLocal = judgeLine.Position.Y - (t + offsetTime) * speed; 
            
            lineN.Position = new Vector2(judgeLine.Position.X, yPosLocal);
            lineE.Position = new Vector2(judgeLineEvent.Position.X, yPosLocal);
            
            lineN.Visible = true;
            lineE.Visible = true;
            
            lineN.index = i;
            lineN.timeSec = t;
            lineE.index = i;
            lineE.timeSec = t;
    
            lineN.RefreshState(i, t, EditorController.instance.beatSeg);
            lineE.RefreshState(i, t, EditorController.instance.beatSeg);

            poolIndex++;
        }

        HideUnusedLines(_poolN, poolIndex);
        HideUnusedLines(_poolE, poolIndex);
    }

    private BeatLine GetLineFromPool(List<BeatLine> pool, int index, Control parent)
    {
        // 如果池子不够大，扩容
        if (index >= pool.Count)
        {
            var newLine = beatLineObj.Instantiate<BeatLine>();
            parent.AddChild(newLine);
            pool.Add(newLine);
            return newLine;
        }
        
        return pool[index];
    }

    private void HideUnusedLines(List<BeatLine> pool, int activeCount)
    {
        for (var i = activeCount; i < pool.Count; i++)
        {
            if (pool[i].Visible) pool[i].Visible = false;
        }
    }
}