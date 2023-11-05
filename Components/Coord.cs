#pragma warning disable CS0660
#pragma warning disable CS0661
using static ChessWPF.Core;

namespace ChessWPF {
    public struct Coord {
        public int absX { get; set; }
        public int absY { get; set; }
        public int relX {
            get { return player.isFirst() ? absX : W - 1 - absX; }
            set { absX = player.isFirst() ? value : W - 1 - value; }
        }
        public int relY {
            get { return player.isFirst() ? absY : H - 1 - absY; }
            set { absY = player.isFirst() ? value : H - 1 - value; }
        }
        public int player { get; set; }

        public Coord(int x, int y, int p = 0, bool rel = false) {
            player = p;
            if (rel) {
                absX = 0;
                absY = 0;
                relX = x;
                relY = y;
            } else {
                absX = x;
                absY = y;
            }
        }

        public static bool operator ==(Coord a, Coord b) {
            return a.absX == b.absX && a.absY == b.absY;
        }
        public static bool operator !=(Coord a, Coord b) {
            return a.absX != b.absX || a.absY != b.absY;
        }
        public static Coord operator +(Coord a) {
            return new Coord(a.absX, a.absY, a.player);
        }
        public static Coord operator -(Coord a) {
            return new Coord(-a.absX, -a.absY, a.player);
        }
        public static Coord operator +(Coord a, Coord b) {
            return new Coord(a.absX + b.absX, a.absY + b.absY, a.player);
        }
        public static Coord operator -(Coord a, Coord b) {
            return new Coord(a.absX - b.absX, a.absY - b.absY, a.player);
        }

        public static Coord operator +(Coord a, aX b) {
            return new Coord(a.absX + b, a.absY, a.player);
        }
        public static Coord operator +(Coord a, aY b) {
            return new Coord(a.absX, a.absY + b, a.player);
        }
        public static Coord operator -(Coord a, aX b) {
            return new Coord(a.absX - b, a.absY, a.player);
        }
        public static Coord operator -(Coord a, aY b) {
            return new Coord(a.absX, a.absY - b, a.player);
        }
        public static Coord operator +(Coord a, rX b) {
            return new Coord(a.relX + b, a.relY, a.player, true);
        }
        public static Coord operator +(Coord a, rY b) {
            return new Coord(a.relX, a.relY + b, a.player, true);
        }
        public static Coord operator -(Coord a, rX b) {
            return new Coord(a.relX - b, a.relY, a.player, true);
        }
        public static Coord operator -(Coord a, rY b) {
            return new Coord(a.relX, a.relY - b, a.player, true);
        }
        public static Coord operator +(Coord a, cP b) {
            return new Coord(a.absX, a.absY, b);
        }

        public struct aX {
            public int v { get; set; }
            public static explicit operator aX(int a) { return new aX() { v = a }; }
            public static implicit operator int(aX a) { return a.v; }
        }
        public struct aY {
            public int v { get; set; }
            public static explicit operator aY(int a) { return new aY() { v = a }; }
            public static implicit operator int(aY a) { return a.v; }
        }
        public struct rX {
            public int v { get; set; }
            public static explicit operator rX(int a) { return new rX() { v = a }; }
            public static implicit operator int(rX a) { return a.v; }
        }
        public struct rY {
            public int v { get; set; }
            public static explicit operator rY(int a) { return new rY() { v = a }; }
            public static implicit operator int(rY a) { return a.v; }
        }
        public struct cP {
            public int v { get; set; }
            public static explicit operator cP(int a) { return new cP() { v = a }; }
            public static implicit operator int(cP a) { return a.v; }
        }

        public override string ToString() {
            return string.Format("{0}, {1}, {2}", absX, absY, player.isFirst() ? "White" : "Black");
        }
    }
}