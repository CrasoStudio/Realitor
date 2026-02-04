using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;

public partial class SkyTrackObj : Control
{
    public int track;
    public ArcLine2D line;

    public float startPosY;

    public List<SkyTrackNodeObj> nodes = [];

    public override void _Ready()
    {
        line = new ArcLine2D();
        AddChild(line);

        startPosY = EditorController.instance.editArea.startPos.Y +
                    EditorController.instance.editArea.skyTracks[track][0].Size.Y / 2;
    }

    public override void _Process(double delta)
    {
        nodes = EditorController.instance.editArea.skyTracks[track];
        if(nodes.Count < 1)
        {
            Hide();
            return;
        }
        else
        {
            Show();
        }
            
        var width = nodes[0].Size.X / 2;
        var height = nodes[0].Size.Y / 2;
        
        line.KeyPoints.Clear();

        line.KeyPoints.Add(new Vector2(862 / 2f + width, startPosY));
        foreach (var node in nodes)
        {
            line.KeyPoints.Add(new Vector2(line.ToLocal(node.GlobalPosition).X + width, line.ToLocal(node.GlobalPosition).Y + height));
        }

        line.KeyPoints.Add(new Vector2(line.KeyPoints[^1].X + width,
            startPosY -
            (EditorController.instance.offset / 1000 + (float)EditorController.instance.music.GetLength()) *
            EditorController.instance.editArea.PixelsPerSecond *
            EditorController.instance.beatScale
        ));

        line.KeyPoints = new Array<Vector2>(line.KeyPoints.OrderBy(point => point.Y).Reverse());
    }
}