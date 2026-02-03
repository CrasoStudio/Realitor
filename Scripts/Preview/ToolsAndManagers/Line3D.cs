using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class Line3D : MeshInstance3D
{
    // 公开属性：控制点的列表
    [Export]
    public Godot.Collections.Array<Vector3> Points
    {
        get => _points;
        set
        {
            _points = value;
            RefreshLine();
        }
    }
    private Godot.Collections.Array<Vector3> _points = [];

    [Export(PropertyHint.Range, "0,1")]
    public float SmoothFactor
    {
        get => _smoothFactor;
        set { _smoothFactor = value; RefreshLine(); }
    }
    private float _smoothFactor = 0.5f;
    
    // 线条宽度
    [Export]
    public float LineWidth
    {
        get => _lineWidth;
        set
        {
            _lineWidth = value;
            RefreshLine();
        }
    }
    private float _lineWidth = 0.1f;

    // 曲线烘焙的密度（数值越小越平滑）
    [Export]
    public float BakeInterval
    {
        get => _bakeInterval;
        set
        {
            _bakeInterval = Mathf.Max(0.01f, value);
            RefreshLine();
        }
    }
    private float _bakeInterval = 0.1f;

    // 内部使用的曲线对象，负责处理三次插值
    private Curve3D _curve = new Curve3D();
    
    // 缓存烘焙后的点，用于Z轴查询
    private Vector3[] _bakedPoints;

    public override void _Ready()
    {
        RefreshLine();
    }

    /// <summary>
    /// 刷新曲线和网格
    /// </summary>
    public void RefreshLine()
    {
        // 1. 设置 Curve3D 数据
        _curve.ClearPoints();
        for (var i = 0; i < _points.Count; i++)
        {
            var currentPos = _points[i];
            var inHandle = Vector3.Zero;
            var outHandle = Vector3.Zero;

            // 只有当平滑度 > 0 时才计算贝塞尔手柄
            if (_smoothFactor > 0.001f)
            {
                if (i < _points.Count - 1)
                {
                    var distNextZ = _points[i + 1].Z - currentPos.Z;
                    // 控制杆只在 Z 轴延伸，形成 "S" 型缓动
                    outHandle = new Vector3(0, 0, distNextZ * _smoothFactor);
                }

                if (i > 0)
                {
                    var distPrevZ = currentPos.Z - _points[i - 1].Z;
                    // 入切线是负方向
                    inHandle = new Vector3(0, 0, -distPrevZ * _smoothFactor);
                }
            }

            // 添加带有控制杆的点
            _curve.AddPoint(currentPos, inHandle, outHandle);
        }

        // 设置烘焙精度
        _curve.BakeInterval = _bakeInterval;
        
        // 获取插值后的高密度点集
        _bakedPoints = _curve.GetBakedPoints();
        
        if (_bakedPoints.Length < 2) return;

        // 2. 生成网格
        GenerateMesh(_bakedPoints);
    }

    private void GenerateMesh(Vector3[] curvePoints)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        
        var upVector = Vector3.Up; 

        for (var i = 0; i < curvePoints.Length; i++)
        {
            var currentPos = curvePoints[i];
            Vector3 forwardDir;

            // 计算切线方向
            if (i < curvePoints.Length - 1)
            {
                forwardDir = (curvePoints[i + 1] - currentPos).Normalized();
            }
            else
            {
                forwardDir = (currentPos - curvePoints[i - 1]).Normalized();
            }

            // 计算右侧向量 (叉乘)
            var rightDir = forwardDir.Cross(upVector).Normalized();

            // 生成左右两个顶点
            var leftVert = currentPos - rightDir * (_lineWidth / 2.0f);
            var rightVert = currentPos + rightDir * (_lineWidth / 2.0f);

            // UV 坐标 (用于贴图)
            var t = (float)i / (curvePoints.Length - 1);

            st.SetNormal(upVector);
            st.SetUV(new Vector2(0, t));
            st.AddVertex(leftVert);

            st.SetNormal(upVector);
            st.SetUV(new Vector2(1, t));
            st.AddVertex(rightVert);
        }

        // 生成索引 (构建三角形)
        for (var i = 0; i < curvePoints.Length - 1; i++)
        {
            var baseIndex = i * 2;
            // Triangle 1
            st.AddIndex(baseIndex);
            st.AddIndex(baseIndex + 2);
            st.AddIndex(baseIndex + 1);
            // Triangle 2
            st.AddIndex(baseIndex + 1);
            st.AddIndex(baseIndex + 2);
            st.AddIndex(baseIndex + 3);
        }
        
        Mesh = st.Commit();
    }

    /// <summary>
    /// 核心功能：通过 Z 坐标获取对应的 X, Y 坐标
    /// </summary>
    /// <param name="targetZ">目标 Z 坐标</param>
    /// <returns>返回对应的 Vector2(x, y)，如果找不到则返回 null</returns>
    public Vector2? GetPositionFromZ(float targetZ)
    {
        if (_bakedPoints == null || _bakedPoints.Length < 2) return null;

        // 遍历所有线段寻找 Z 区间
        // 注意：这里假设曲线在 Z 轴上是单调的或者你只需要第一个交点
        // 如果曲线是螺旋形（同一个 Z 对应多个点），这个函数只返回第一个找到的点
        for (var i = 0; i < _bakedPoints.Length - 1; i++)
        {
            var p1 = _bakedPoints[i];
            var p2 = _bakedPoints[i + 1];

            var minZ = Mathf.Min(p1.Z, p2.Z);
            var maxZ = Mathf.Max(p1.Z, p2.Z);

            // 检查 targetZ 是否在这两个点之间
            if (targetZ >= minZ && targetZ <= maxZ)
            {
                // 计算插值比例 t
                // 防止除以零
                var range = p2.Z - p1.Z;
                float t = 0;
                
                if (Mathf.Abs(range) > 0.00001f)
                {
                    t = (targetZ - p1.Z) / range;
                }

                // 线性插值获取 X 和 Y
                var x = Mathf.Lerp(p1.X, p2.X, t);
                var y = Mathf.Lerp(p1.Y, p2.Y, t);

                return new Vector2(x, y);
            }
        }

        return null; // Z 坐标超出曲线范围
    }
}