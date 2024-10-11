﻿using System.Collections.Generic;
using Godot;

namespace Game;

public static class Rect2IExtensions
{
    public static List<Vector2I> ToTiles(this Rect2I rect)
    {
        var tiles = new List<Vector2I>();
        for (var x = rect.Position.X; x < rect.End.X; x++)
        {
            for (var y = rect.Position.Y; y < rect.End.Y; y++)
            {
                tiles.Add(new Vector2I(x, y));
            }
        }
        return tiles;
    }
}