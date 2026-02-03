using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class ArcLine2D : Line2D
{
    [ExportGroup("Arc Settings")]
    // 关键点：X代表横向位置（轨道），Y代表纵向位置（时间/距离）
    [Export]
    public Godot.Collections.Array<Vector2> KeyPoints
    {
        get => _keyPoints;
        set { _keyPoints = value; RefreshLine(); }
    }
    private Godot.Collections.Array<Vector2> _keyPoints = new();

    // 平滑度：0 = 直线，0.5 = 标准 S 曲线
    [Export(PropertyHint.Range, "0,1")]
    public float SmoothFactor
    {
        get => _smoothFactor;
        set { _smoothFactor = value; RefreshLine(); }
    }
    private float _smoothFactor = 0.5f;

    [Export]
    public float BakeInterval
    {
        get => _bakeInterval;
        set { _bakeInterval = Mathf.Max(1.0f, value); RefreshLine(); } // 2D 像素单位，通常 5-10 足够
    }
    private float _bakeInterval = 5.0f;

    private Curve2D _curve = new();

    public override void _Process(double delta)
    {
        Points = [];
        RefreshLine();
    }

    public void RefreshLine()
    {
        if (_keyPoints == null || _keyPoints.Count < 2)
        {
            Points = [];
            return;
        }

        _curve.ClearPoints();

        for (var i = 0; i < _keyPoints.Count; i++)
        {
            var currentPos = _keyPoints[i];
            var inHandle = Vector2.Zero;
            var outHandle = Vector2.Zero;

            if (_smoothFactor > 0.001f)
            {
                // 核心逻辑：控制柄只在 Y 轴（时间轴）上延伸
                // 这样能造出音游特有的“先纵走，再横移”的 S 曲线
                if (i < _keyPoints.Count - 1)
                {
                    var distNextY = _keyPoints[i + 1].Y - currentPos.Y;
                    outHandle = new Vector2(0, distNextY * _smoothFactor);
                }

                if (i > 0)
                {
                    var distPrevY = currentPos.Y - _keyPoints[i - 1].Y;
                    inHandle = new Vector2(0, -distPrevY * _smoothFactor);
                }
            }

            _curve.AddPoint(currentPos, inHandle, outHandle);
        }

        _curve.BakeInterval = _bakeInterval;
        
        // 直接赋值给 Line2D 的 Points 属性
        Points = _curve.GetBakedPoints();
    }

    /// <summary>
    /// 给定一个 Y 坐标，找到曲线上对应的 X 坐标
    /// </summary>
    public Vector2 GetPositionFromY(float targetY)
    {
        var baked = Points;
        // 假设 Y 是单调递增的（音游通常是从上到下或从下到上）
        
        for (var i = 0; i < baked.Length - 1; i++)
        {
            var y1 = baked[i].Y;
            var y2 = baked[i + 1].Y;

            // 检查 targetY 是否在当前线段区间内
            // 兼容 Y 轴向下(正) 或 向上(负) 的情况
            var inRange = (y1 <= targetY && targetY <= y2) || (y1 >= targetY && targetY >= y2);

            if (inRange)
            {
                var range = y2 - y1;
                var t = (Mathf.Abs(range) > 0.00001f) ? (targetY - y1) / range : 0;
                
                return new Vector2(
                    Mathf.Lerp(baked[i].X, baked[i + 1].X, t),
                    targetY // 直接返回目标 Y，避免浮点误差
                );
            }
        }
        return Vector2.Zero;
    }
}