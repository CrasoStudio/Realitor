using System.Collections.Generic;

public class ChartJson
{
    public float BPM;
    public float offset;
    public int version = 1;
    
    public List<GroundTrack> groundTracks = new();
    public List<SkyTrack> skyTracks = new();
    
    public List<BPMEvent> BPMEvents = new();
    public List<SpeedGroup> speedGroups = new();
    
    public List<CameraMoveEvent> cameraMoveEvents = new();
    public List<CameraRotateEvent> cameraRotateEvents = new();
    public List<CameraBrightnessEvent> cameraBrightnessEvents = new();
}

public class Note
{
    public float time;
    public float duration;
    public int type;//tap=0,hold=1
    public float speed = 1;
    public int speedGroupID;
}

public class SkyNote
{
    public float time;
    public int type;//tap=0,flick=1,catch=2
}

public class GroundTrack
{
    public int track;//在 0-3 范围外为装饰轨道
    public List<Note> notes;
    public List<TrackMoveEvent> trackMoveEvents = new();
    public List<TrackRotateEvent> trackRotateEvents = new();
    public List<TrackTransparencyEvent> trackTransparencyEvents = new();
}

public class SkyTrack
{
    public int track;
    public List<TrackPoint> points = new();
    public List<SkyNote> notes = new();
}

public class TrackPoint
{
    public float time;
    public float x, y;
    public int speed;
}

public class SpeedGroup
{
    public int id;
    public List<SpeedEvent> events = new();
}

public class SpeedEvent
{
    public float startTime;
    public float endTime;
    public float speed;
}

public class BPMEvent
{
    public float startTime;
    public float BPM;
}

public class CameraMoveEvent
{
    public float startTime;
    public float endTime;
    public float startXPos;
    public float startYPos;
    public float startZPos;
    public float endXPos;
    public float endYPos;
    public float endZPos;
    public EasingMode easing;
}

public class CameraRotateEvent
{
    public float startTime;
    public float endTime;
    public float startXRotate;
    public float startYRotate;
    public float startZRotate;
    public float endXRotate;
    public float endYRotate;
    public float endZRotate;
    public EasingMode easing;
}

public class CameraBrightnessEvent
{
    public float startTime;
    public float endTime;
    public float startValue;
    public float endValue;
    public EasingMode easing;
}

public class TrackMoveEvent
{
    public float startTime;
    public float endTime;
    public float startXPos;
    public float startYPos;
    public float startZPos;
    public float endXPos;
    public float endYPos;
    public float endZPos;
    public EasingMode easing;
}

public class TrackRotateEvent
{
    public float startTime;
    public float endTime;
    public float startXRotate;
    public float startYRotate;
    public float startZRotate;
    public float endXRotate;
    public float endYRotate;
    public float endZRotate;
    public EasingMode easing;
}

public class TrackTransparencyEvent
{
    public float startTime;
    public float endTime;
    public float startValue;
    public float endValue;
    public EasingMode easing;
}

public class EasingMode
{
    public EaseType easeType;
    public TransitionType transType;
}

public enum EaseType
{
    In,
    Out,
    InOut
}

public enum TransitionType
{
    Linear,
    Sine,
    Quad,
    Cubic,
    Quart,
    Quint,
    Expo,
    Circ,
    Back,
    Elastic,
    Bounce
}
