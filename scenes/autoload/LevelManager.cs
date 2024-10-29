using Godot;

namespace Game.Autoload;

public partial class LevelManager : Node
{
    public static LevelManager Instance { get; private set; }

    [Export]
    private PackedScene[] levelScenes;

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
        {
            Instance = this;
        }
    }

    public void ChangeToLevel(int levelIndex)
    {
        if (levelIndex >= levelScenes.Length || levelIndex < 0)
        {
            return;
        }

        var levelScene = levelScenes[levelIndex];
        GetTree().ChangeSceneToPacked(levelScene);
    }
}
