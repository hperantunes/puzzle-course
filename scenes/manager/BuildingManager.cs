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
    private Sprite2D cursor;

    private BuildingResource toPlaceBuildingResource;
    private Vector2I? hoveredGridCell;

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
        var gridPosition = gridManager.GetMouseGridCellPosition();
        cursor.GlobalPosition = gridPosition * 64;
        if (toPlaceBuildingResource != null
            && cursor.Visible
            && (!hoveredGridCell.HasValue || hoveredGridCell.Value != gridPosition))
        {
            hoveredGridCell = gridPosition;
            gridManager.ClearHighlightedTiles();
            gridManager.HighlightExpandedBuildableTiles(hoveredGridCell.Value, toPlaceBuildingResource.BuildableRadius);
            gridManager.HighlightResourceTiles(hoveredGridCell.Value, toPlaceBuildingResource.ResourceRadius);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (hoveredGridCell.HasValue
            && toPlaceBuildingResource != null
            && @event.IsActionPressed("left_click")
            && gridManager.IsTilePositionBuildable(hoveredGridCell.Value)
            && availableResourceCount >= toPlaceBuildingResource.ResourceCost)
        {
            PlaceBuildingAtHoveredCellPosition();
            cursor.Visible = false;
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
        GD.Print(availableResourceCount);
    }

    private void OnResourceTilesUpdated(int resourceCount)
    {
        currentResourceCount = resourceCount;
    }

    private void OnBuildingResourceSelected(BuildingResource buildingResource)
    {
        toPlaceBuildingResource = buildingResource;
        cursor.Visible = true;
        gridManager.HighlightBuildableTiles();
    }
}

