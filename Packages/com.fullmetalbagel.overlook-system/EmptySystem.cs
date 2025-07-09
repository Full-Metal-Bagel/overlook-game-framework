#nullable enable
namespace Game;

public sealed class EmptySystem : IGameSystem
{
    public static EmptySystem Instance { get; } = new();
    public void Tick(GameData data) { }
}
