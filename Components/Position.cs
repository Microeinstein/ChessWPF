using System.Windows;
using static System.Math;

namespace ChessWPF {
    public struct Position {
        public double x, y;

        public Position(double x, double y) {
            this.x = x;
            this.y = y;
        }
        public void rotateClockwise(double deg, double centerX, double centerY) {
            double rad = deg * (PI / 180);
            double cosTheta = Cos(rad);
            double sinTheta = Sin(rad);
            x = Round(cosTheta * (x - centerX) - sinTheta * (y - centerY) + centerY);
            y = Round(sinTheta * (x - centerX) + cosTheta * (y - centerY) + centerY);
        }
        public static Position fromRad(double rad) {
            return new Position(Cos(rad), Sin(rad));
        }

        public static implicit operator Thickness(Position pos) {
            return new Thickness(pos.x, 0, 0, pos.y);
        }
        public static implicit operator Position(Thickness pos) {
            return new Position(pos.Left, pos.Bottom);
        }
        public static implicit operator Coord(Position pos) {
            return new Coord((int)pos.x, (int)pos.y);
        }
        public static implicit operator Position(Coord pos) {
            return new Position(pos.absX, pos.absY);
        }
        public static implicit operator Point(Position pos) {
            return new Point(pos.x, pos.y);
        }
        public static implicit operator Position(Point pos) {
            return new Position(pos.X, pos.Y);
        }
        public static Position operator +(Position a, Position b) {
            return new Position(a.x + b.x, a.y + b.y);
        }
        public static Position operator -(Position a, Position b) {
            return new Position(a.x - b.x, a.y - b.y);
        }
        public static Position operator *(Position a, double rad) {
            return new Position(a.x * Cos(rad), a.y * Sin(rad));
        }
        public static Position operator +(Position a, double[] b) {
            return new Position(a.x + b[0], a.y + b[1]);
        }
        public static Position operator -(Position a, double[] b) {
            return new Position(a.x - b[0], a.y - b[1]);
        }
    }
}