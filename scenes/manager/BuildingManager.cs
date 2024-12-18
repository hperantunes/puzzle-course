﻿using System.Linq;
using Game.Building;
using Game.Component;
using Game.Resources.Building;
using Game.UI;
using Godot;

namespace Game.Manager;

public partial class BuildingManager : Node
{
    private readonly StringName ActionLeftClick = "left_click";
    private readonly StringName ActionRightClick = "right_click";
    private readonly StringName ActionCancel = "cancel";

    [Signal]
    public delegate void AvailableResourceCountChangedEventHandler(int availableResourceCount);

    [Export]
    private int startingResourceCount = 4;
    [Export]
    private GridManager gridManager;
    [Export]
    private GameUI gameUI;
    [Export]
    private Node2D ySortRoot;
    [Export]
    private PackedScene buildingGhostScene;

    private enum State
    {
        Normal,
        PlacingBuilding
    }

    private State currentState;

    private BuildingResource toPlaceBuildingResource;
    private Rect2I hoveredGridArea = new(Vector2I.Zero, Vector2I.One);
    private BuildingGhost buildingGhost;

    private int currentResourceCount;
    private int currentlyUsedResourceCount;

    private int AvailableResourceCount => startingResourceCount + currentResourceCount - currentlyUsedResourceCount;

    public override void _Ready()
    {
        gridManager.ResourceTilesUpdated += OnResourceTilesUpdated;
        gameUI.BuildingResourceSelected += OnBuildingResourceSelected;

        Callable.From(() => EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount)).CallDeferred();
    }

    public override void _Process(double delta)
    {
        var mouseGridPosition = gridManager.GetMouseGridCellPosition();
        var rootCell = hoveredGridArea.Position;
        if (rootCell != mouseGridPosition)
        {
            hoveredGridArea.Position = mouseGridPosition;
            UpdateHoveredGridArea();
        }

        switch (currentState)
        {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                buildingGhost.GlobalPosition = mouseGridPosition * 64;
                break;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (currentState)
        {
            case State.Normal:
                if (@event.IsActionPressed(ActionRightClick))
                {
                    DestroyBuildingAtHoveredCellPosition();
                }
                break;
            case State.PlacingBuilding:
                if (@event.IsActionPressed(ActionCancel))
                {
                    ChangeState(State.Normal);
                }
                else if (@event.IsActionPressed(ActionLeftClick)
                    && toPlaceBuildingResource != null
                    && IsBuildingPlaceableAtArea(hoveredGridArea))
                {
                    PlaceBuildingAtHoveredCellPosition();
                }
                break;
            default:
                break;
        }
    }

    private void UpdateGridDisplay()
    {
        gridManager.ClearHighlightedTiles();
        gridManager.HighlightBuildableTiles();
        if (IsBuildingPlaceableAtArea(hoveredGridArea))
        {
            gridManager.HighlightExpandedBuildableTiles(hoveredGridArea, toPlaceBuildingResource.BuildableRadius);
            gridManager.HighlightResourceTiles(hoveredGridArea, toPlaceBuildingResource.ResourceRadius);
            buildingGhost.SetValid();
        }
        else
        {
            buildingGhost.SetInvalid();
        }
    }

    private void PlaceBuildingAtHoveredCellPosition()
    {
        var building = toPlaceBuildingResource.BuildingScene.Instantiate<Node2D>();
        ySortRoot.AddChild(building);

        building.GlobalPosition = hoveredGridArea.Position * 64;

        currentlyUsedResourceCount += toPlaceBuildingResource.ResourceCost;

        ChangeState(State.Normal);
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void DestroyBuildingAtHoveredCellPosition()
    {
        var rootCell = hoveredGridArea.Position;
        var buildingComponent = GetTree()
            .GetNodesInGroup(nameof(BuildingComponent))
            .Cast<BuildingComponent>()
            .FirstOrDefault(buildingComponent =>
            {
                return buildingComponent.BuildingResource.IsDeletable
                    && buildingComponent.IsTileInBuildingArea(rootCell);
            });

        if (buildingComponent == null)
        {
            return;
        }

        currentlyUsedResourceCount -= buildingComponent.BuildingResource.ResourceCost;
        buildingComponent.Destroy();
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void ClearBuildingGhost()
    {
        gridManager.ClearHighlightedTiles();

        if (IsInstanceValid(buildingGhost))
        {
            buildingGhost.QueueFree();
        }
        buildingGhost = null;
    }

    private bool IsBuildingPlaceableAtArea(Rect2I tileArea)
    {
        var allBuildableTiles = gridManager.IsTileAreaBuildable(tileArea);
        return allBuildableTiles && AvailableResourceCount >= toPlaceBuildingResource.ResourceCost;
    }

    private void UpdateHoveredGridArea()
    {
        switch (currentState)
        {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                UpdateGridDisplay();
                break;
        }
    }

    private void ChangeState(State toState)
    {
        switch (currentState)
        {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                ClearBuildingGhost();
                toPlaceBuildingResource = null;
                break;
        }

        currentState = toState;

        switch (currentState)
        {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                buildingGhost = buildingGhostScene.Instantiate<BuildingGhost>();
                ySortRoot.AddChild(buildingGhost);
                break;
        }
    }

    private void OnResourceTilesUpdated(int resourceCount)
    {
        currentResourceCount = resourceCount;
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void OnBuildingResourceSelected(BuildingResource buildingResource)
    {
        ChangeState(State.PlacingBuilding);
        hoveredGridArea.Size = buildingResource.Dimensions;

        var buildingSprite = buildingResource.SpriteScene.Instantiate<Sprite2D>();
        buildingGhost.AddChild(buildingSprite);

        toPlaceBuildingResource = buildingResource;
        UpdateGridDisplay();
    }
}
