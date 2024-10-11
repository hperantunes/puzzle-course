using Godot;

namespace Game;

public partial class GameCamera : Camera2D
{
    private const int tileSize = 64;
    private const float panSpeed = 500;

    private readonly StringName actionPanLeft = "pan_left";
    private readonly StringName actionPanRight = "pan_right";
    private readonly StringName actionPanUp = "pan_up";
    private readonly StringName actionPanDown = "pan_down";

    public override void _Process(double delta)
    {
        GlobalPosition = GetScreenCenterPosition();

        var movementVector = Input.GetVector(actionPanLeft, actionPanRight, actionPanUp, actionPanDown);
        GlobalPosition += movementVector * panSpeed * (float)delta;
    }

    public void SetBoundingRect(Rect2I boundingRect)
    {
        LimitLeft = boundingRect.Position.X * tileSize;
        LimitRight = boundingRect.End.X * tileSize;
        LimitTop = boundingRect.Position.Y * tileSize;
        LimitBottom = boundingRect.End.Y * tileSize;
    }
}
