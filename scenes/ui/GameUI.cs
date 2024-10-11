using Game.Resources.Building;
using Godot;

namespace Game.UI;

public partial class GameUI : CanvasLayer
{
    private HBoxContainer hBoxContainer;

    [Signal]
    public delegate void BuildingResourceSelectedEventHandler(BuildingResource buildingResource);

    [Export]
    private BuildingResource[] buildingResources;

    public override void _Ready()
    {
        hBoxContainer = GetNode<HBoxContainer>("MarginContainer/HBoxContainer");
        CreateBuildingButtons();
    }

    private void CreateBuildingButtons()
    {
        foreach (var buildingResource in buildingResources)
        {
            var buildingButton = new Button
            {
                Text = $"Place {buildingResource.DisplayName}"
            };
            hBoxContainer.AddChild(buildingButton);
            buildingButton.Pressed += () => EmitSignal(SignalName.BuildingResourceSelected, buildingResource);
        }
    }
}
