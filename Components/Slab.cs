using System.Windows.Controls;

namespace ChessWPF {
    public class Slab : UserControl {
        //public static Regex regx = new Regex(@"^(\d),(\d),(\d),(\d)$");
        public SlabType type;
        public Coord loc;
        public int player {
            get { return loc.player; }
            set { loc.player = value; }
        }

        public Slab() { }
        public Slab(int x, int y, int pl = 0, SlabType ty = SlabType.Null) {
            loc = new Coord(x, y, pl, true);
            type = ty;
        }
        //public static Slab Parse(string txt) {
        //var grps = regx.Match(txt).Groups;
        //return new Slab(int.Parse(grps[1].Value), int.Parse(grps[2].Value), int.Parse(grps[3].Value), (SlabType)int.Parse(grps[4].Value));
        //}

        //public override string ToString() {
        //return string.Format("{0},{1},{2},{3}", loc.relX, loc.relY, player, (int)type);
        //}
    }
}
