namespace Overlook.Ecs.Tests
{
    public interface I {}
    public interface II {}
    public class C : I {}
    public class CC : C, II {}
    public struct S : I {}

    public readonly record struct Position(int X = 0, int Y = 0);
    public readonly record struct Velocity(int X = 0, int Y = 0);

    public struct SomeNonExistentComponent { }

    public readonly record struct Health(int Value = 0);

    public struct UnmanagedComponent
    {
        public float X;
        public float Y;
        public float Z;
    }
}
