namespace Apogee.Bot
{
    public struct Unit : System.IEquatable<Unit>
    {
        public static readonly Unit Empty = new Unit();

        public bool Equals(Unit other) => true;
        public override bool Equals(object? obj) => obj is Unit;
        public static bool operator ==(Unit a, Unit b) => true;
        public static bool operator !=(Unit a, Unit b) => false;
        public override int GetHashCode() => 0;

        public override string ToString() => "()";

        public static explicit operator Unit(bool obj) => Empty;
    }
}
