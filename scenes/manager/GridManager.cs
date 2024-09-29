using System.Collections.Generic;
using System.Linq;
using Game.Autoload;
using Game.Component;
using Godot;

namespace Game.Manager;

public partial class GridManager : Node
{
    private HashSet<Vector2I> validBuildableTiles = new();

    [Export]
    private TileMapLayer highlightTileMapLayer;
    [Export]
    private TileMapLayer baseTerrainTileMapLayer;

    public override void _Ready()
    {
        GameEvents.Instance.BuildingPlaced += OnBuildingPlaced;
    }

    public bool IsTilePositionValid(Vector2I tilePosition)
    {
        var customData = baseTerrainTileMapLayer.GetCellTileData(tilePosition);
        if (customData == null)
        {
            return false;
        }
        return (bool)customData.GetCustomData("buildable");
    }

    public bool IsTilePositionBuildable(Vector2I tilePosition)
    {
        return validBuildableTiles.Contains(tilePosition);
    }

    public void HighlightBuildableTiles()
    {
        foreach (var tilePosition in validBuildableTiles)
        {
            highlightTileMapLayer.SetCell(tilePosition, 0, Vector2I.Zero);
        }
    }

    public void ClearHighlightedTiles()
    {
        highlightTileMapLayer.Clear();
    }

    public Vector2I GetMouseGridCellPosition()
    {
        var mousePosition = highlightTileMapLayer.GetGlobalMousePosition();
        var gridPosition = mousePosition / 64;
        gridPosition = gridPosition.Floor();
        return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
    }

    private void UpdateValidBuildableTiles(BuildingComponent buildingComponent)
    {
        var rootCell = buildingComponent.GetGridCellPosition();
        for (var x = rootCell.X - buildingComponent.BuildableRadius; x <= rootCell.X + buildingComponent.BuildableRadius; x++)
        {
            for (var y = rootCell.Y - buildingComponent.BuildableRadius; y <= rootCell.Y + buildingComponent.BuildableRadius; y++)
            {
                var tilePosition = new Vector2I(x, y);
                if (!IsTilePositionValid(tilePosition))
                {
                    continue;
                }
                validBuildableTiles.Add(tilePosition);
            }
        }
        validBuildableTiles.Remove(buildingComponent.GetGridCellPosition());
    }

    private void OnBuildingPlaced(BuildingComponent buildingComponent)
    {
        UpdateValidBuildableTiles(buildingComponent);
    }
}
