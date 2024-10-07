﻿using System;
using System.Collections.Generic;
using System.Linq;
using Game.Autoload;
using Game.Component;
using Godot;

namespace Game.Manager;

public partial class GridManager : Node
{
    private const string IsBuildable = "is_buildable";
    private const string IsWood = "is_wood";

    [Signal]
    public delegate void ResourceTilesUpdatedEventHandler(int collectedTiles);

    private HashSet<Vector2I> validBuildableTiles = new();
    private HashSet<Vector2I> collectedResourceTiles = new();

    private List<TileMapLayer> allTileMapLayers = new();

    [Export]
    private TileMapLayer highlightTileMapLayer;
    [Export]
    private TileMapLayer baseTerrainTileMapLayer;

    public override void _Ready()
    {
        GameEvents.Instance.BuildingPlaced += OnBuildingPlaced;
        allTileMapLayers = GetAllTileMapLayers(baseTerrainTileMapLayer);
    }

    public bool TileHasCustomData(Vector2I tilePosition, string dataName)
    {
        foreach (var layer in allTileMapLayers)
        {
            var customData = layer.GetCellTileData(tilePosition);
            if (customData == null)
            {
                continue;
            }
            return (bool)customData.GetCustomData(dataName);
        }
        return false;
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

    public void HighlightExpandedBuildableTiles(Vector2I rootCell, int radius)
    {
        var validTiles = GetValidTilesInRadius(rootCell, radius).ToHashSet();
        var expandedTiles = validTiles
            .Except(validBuildableTiles)
            .Except(GetOccupiedTiles());
        var atlasCoordinates = new Vector2I(1, 0);
        foreach (var tilePosition in expandedTiles)
        {
            highlightTileMapLayer.SetCell(tilePosition, 0, atlasCoordinates);
        }
    }

    public void HighlightResourceTiles(Vector2I rootCell, int radius)
    {
        var resourceTiles = GetResourceTilesInRadius(rootCell, radius);
        var atlasCoordinates = new Vector2I(1, 0);
        foreach (var tilePosition in resourceTiles)
        {
            highlightTileMapLayer.SetCell(tilePosition, 0, atlasCoordinates);
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

    private List<TileMapLayer> GetAllTileMapLayers(TileMapLayer rootTileMapLayer)
    {
        var result = new List<TileMapLayer>();
        var children = rootTileMapLayer.GetChildren();
        children.Reverse();
        foreach (var child in children)
        {
            if (child is TileMapLayer childLayer)
            {
                result.AddRange(GetAllTileMapLayers(childLayer));
            }
        }
        result.Add(rootTileMapLayer);
        return result;
    }

    private void UpdateValidBuildableTiles(BuildingComponent buildingComponent)
    {
        var rootCell = buildingComponent.GetGridCellPosition();
        var validTiles = GetValidTilesInRadius(rootCell, buildingComponent.BuildingResource.BuildableRadius);
        validBuildableTiles.UnionWith(validTiles);

        var occupiedTiles = GetOccupiedTiles();
        validBuildableTiles.ExceptWith(occupiedTiles);
    }

    private void UpdateCollectedResourceTiles(BuildingComponent buildingComponent)
    {
        var rootCell = buildingComponent.GetGridCellPosition();
        var resourceTiles = GetResourceTilesInRadius(rootCell, buildingComponent.BuildingResource.ResourceRadius);

        var oldResourceTileCount = collectedResourceTiles.Count;
        collectedResourceTiles.UnionWith(resourceTiles);

        if (oldResourceTileCount != collectedResourceTiles.Count)
        {
            EmitSignal(SignalName.ResourceTilesUpdated, collectedResourceTiles.Count);
        }
    }

    private List<Vector2I> GetTilesInRadius(Vector2I rootCell, int radius, Func<Vector2I, bool> filter)
    {
        var result = new List<Vector2I>();
        for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
        {
            for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++)
            {
                var tilePosition = new Vector2I(x, y);
                if (!filter(tilePosition))
                {
                    continue;
                }
                result.Add(tilePosition);
            }
        }
        return result;
    }

    private List<Vector2I> GetValidTilesInRadius(Vector2I rootCell, int radius)
    {
        return GetTilesInRadius(rootCell, radius, (tilePosition) => TileHasCustomData(tilePosition, IsBuildable));
    }

    private List<Vector2I> GetResourceTilesInRadius(Vector2I rootCell, int radius)
    {
        return GetTilesInRadius(rootCell, radius, (tilePosition) => TileHasCustomData(tilePosition, IsWood));
    }

    private IEnumerable<Vector2I> GetOccupiedTiles()
    {
        var buildingComponents = GetTree().GetNodesInGroup(nameof(BuildingComponent)).Cast<BuildingComponent>();
        var occupiedTiles = buildingComponents.Select(x => x.GetGridCellPosition());
        return occupiedTiles;
    }

    private void OnBuildingPlaced(BuildingComponent buildingComponent)
    {
        UpdateValidBuildableTiles(buildingComponent);
        UpdateCollectedResourceTiles(buildingComponent);
    }
}
