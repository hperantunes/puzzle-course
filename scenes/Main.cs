using Godot;
using System;

namespace Game;

public partial class Main : Node2D
{
    private Sprite2D sprite;
    private PackedScene buildingScene;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        buildingScene = GD.Load<PackedScene>("res://scenes/building/Building.tscn");
        sprite = GetNode<Sprite2D>("Cursor");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("left_click"))
        {
            PlaceBuildingAtMousePosition();
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        var gridPosition = GetMouseGridCellPosition();
        sprite.GlobalPosition = gridPosition * 64;
    }

    private Vector2 GetMouseGridCellPosition()
    {
        var mousePosition = GetGlobalMousePosition();
        var gridPosition = mousePosition / 64;
        gridPosition = gridPosition.Floor();
        return gridPosition;
    }

    private void PlaceBuildingAtMousePosition()
    {
        var building = buildingScene.Instantiate<Node2D>();
        AddChild(building);

        var gridPosition = GetMouseGridCellPosition();
        building.GlobalPosition = gridPosition * 64;
    }
}
