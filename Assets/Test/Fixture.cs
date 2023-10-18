namespace RelEcs.Tests
{
    public interface I {}
    public interface II {}
    public class C : I {}
    public class CC : C, II {}
    public struct S : I {}

    public struct Position
    {
        public int X { get; }
        public int Y { get; }
        public Position(int x, int y) { X = x; Y = y; }
    }

    public struct Velocity
    {
        public int X { get; }
        public int Y { get; }
        public Velocity(int x, int y) { X = x; Y = y; }
    }

    public struct SomeNonExistentComponent { }

    public struct Health
    {
        public int Value;
        public Health(int value) { Value = value; }
    }

    public struct UnmanagedComponent
    {
        public float X;
        public float Y;
        public float Z;
    }
}
