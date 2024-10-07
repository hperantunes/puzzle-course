﻿using Game.Building;
using Game.Resources.Building;
using Game.UI;
using Godot;

namespace Game.Manager;

public partial class BuildingManager : Node
{
    [Export]
    private GridManager gridManager;
    [Export]
    private GameUI gameUI;
    [Export]
    private Node2D ySortRoot;
    [Export]
    private PackedScene buildingGhostScene;

    private BuildingResource toPlaceBuildingResource;
    private Vector2I? hoveredGridCell;
    private BuildingGhost buildingGhost;

    private int currentResourceCount;
    private int startingResourceCount = 4;
    private int currentlyUsedResourceCount;

    private int availableResourceCount => startingResourceCount + currentResourceCount - currentlyUsedResourceCount;

    public override void _Ready()
    {
        gridManager.ResourceTilesUpdated += OnResourceTilesUpdated;
        gameUI.BuildingResourceSelected += OnBuildingResourceSelected;
    }

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(buildingGhost))
        {
            return;
        }

        var gridPosition = gridManager.GetMouseGridCellPosition();
        buildingGhost.GlobalPosition = gridPosition * 64;
        if (toPlaceBuildingResource != null
            && (!hoveredGridCell.HasValue || hoveredGridCell.Value != gridPosition))
        {
            hoveredGridCell = gridPosition;
            UpdateGridDisplay();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (hoveredGridCell.HasValue
            && toPlaceBuildingResource != null
            && @event.IsActionPressed("left_click")
            && IsBuildingPlaceableAtTile(hoveredGridCell.Value))
        {
            PlaceBuildingAtHoveredCellPosition();
        }
    }

    private void UpdateGridDisplay()
    {
        if (hoveredGridCell == null)
        {
            return;
        }

        gridManager.ClearHighlightedTiles();
        gridManager.HighlightBuildableTiles();
        if (IsBuildingPlaceableAtTile(hoveredGridCell.Value))
        {
            gridManager.HighlightExpandedBuildableTiles(hoveredGridCell.Value, toPlaceBuildingResource.BuildableRadius);
            gridManager.HighlightResourceTiles(hoveredGridCell.Value, toPlaceBuildingResource.ResourceRadius);
            buildingGhost.SetValid();
        }
        else
        {
            buildingGhost.SetInvalid();
        }
    }

    private void PlaceBuildingAtHoveredCellPosition()
    {
        if (!hoveredGridCell.HasValue)
        {
            return;
        }

        var building = toPlaceBuildingResource.BuildingScene.Instantiate<Node2D>();
        ySortRoot.AddChild(building);

        building.GlobalPosition = hoveredGridCell.Value * 64;

        hoveredGridCell = null;
        gridManager.ClearHighlightedTiles();

        currentlyUsedResourceCount += toPlaceBuildingResource.ResourceCost;
        buildingGhost.QueueFree();
        buildingGhost = null;
    }

    private bool IsBuildingPlaceableAtTile(Vector2I tilePosition)
    {
        return gridManager.IsTilePositionBuildable(tilePosition)
            && availableResourceCount >= toPlaceBuildingResource.ResourceCost;
    }

    private void OnResourceTilesUpdated(int resourceCount)
    {
        currentResourceCount = resourceCount;
    }

    private void OnBuildingResourceSelected(BuildingResource buildingResource)
    {
        if (IsInstanceValid(buildingGhost))
        {
            buildingGhost.QueueFree();
        }

        buildingGhost = buildingGhostScene.Instantiate<BuildingGhost>();
        ySortRoot.AddChild(buildingGhost);

        var buildingSprite = buildingResource.SpriteScene.Instantiate<Sprite2D>();
        buildingGhost.AddChild(buildingSprite);

        toPlaceBuildingResource = buildingResource;
        UpdateGridDisplay();
    }
}

