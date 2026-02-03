using System.Collections.Generic;

/*
 注：谱面中出现的位置坐标范围为-1到1，XZ轴移动长度在任何地方都相同，Y轴移动范围中，
    天空note：-1为地面轨道平面，0为天空轨道默认高度，1为摄像机对应高度；
    摄像机：-1为地面轨道平面，0为摄像机默认高度；
    轨道略
*/

public class ChartJson
{
    public float BPM;//完成
    public float offset;//完成
    public int version = 1;//完成
    
    public List<GroundTrack> groundTracks = [];//完成
    public List<SkyTrack> skyTracks = [];//完成
    
    public List<BPMEvent> BPMEvents = [];//完成
    public List<SpeedGroup> speedGroups = [];
    
    public List<CameraMoveEvent> cameraMoveEvents = [];
    public List<CameraRotateEvent> cameraRotateEvents = [];
    public List<CameraBrightnessEvent> cameraBrightnessEvents = [];
}

public class Note
{
    public float time;
    public float duration;
    public int type;// tap=0,hold=1
    public float speed = 1;//x
    public int speedGroupID;//x
}

public class SkyNote
{
    public float time;
    public int type;// tap=0,flick=1,catch=2
}

public class GroundTrack
{
    public int track;// 在 0-3 范围外为装饰轨道
    public List<Note> notes = [];
    public List<TrackMoveEvent> trackMoveEvents = [];//x
    public List<TrackRotateEvent> trackRotateEvents = [];//x
    public List<TrackTransparencyEvent> trackTransparencyEvents = [];//x
}

public class SkyTrack
{
    public int track;
    public List<TrackPoint> points = [];
    public List<SkyNote> notes = [];
}

public class TrackPoint
{
    public float time;
    public float x, y;//y-x
    public int speed = 1;//x
}

public class SpeedGroup
{
    public int id;
    public List<SpeedEvent> events = [];
}

public class SpeedEvent
{
    public float startTime;
    public float endTime;
    public float speed = 1;
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
