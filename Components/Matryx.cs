using System;
using System.Collections;
using System.Collections.Generic;
using static ChessWPF.Core;

namespace ChessWPF {
    public struct MatryxP : IEnumerable<Piece>, IEnumerator {
        //public static Regex regx = new Regex(@"^(\d+),(\d+)\[(.+)\]$");
        List<Piece[,]> multiverse;
        int posx, posy;
        int _abstractionLevel;
        public readonly int W, H;

        public int realityLevel { get { return _abstractionLevel; } }

        public MatryxP(int w, int h) {
            W = w;
            H = h;
            _abstractionLevel = 0;
            multiverse = new List<Piece[,]>();
            multiverse.Add(new Piece[W, H]);
            posx = 0;
            posy = 0;
        }
        //public static MatryxP Parse(string txt) {
            //MatryxP ret;
            //int w = 0, h = 0, wp = 0, hp = 0;
            //var grps = regx.Match(txt).Groups;
            //w = int.Parse(grps[1].Value);
            //h = int.Parse(grps[2].Value);
            //ret = new MatryxP(w, h);
            //var pcs = new Piece[w, h];
            //var slbs = grps[3].Value.Split('|');
            //foreach (var slb in slbs) {
                //pcs[wp, hp] = new Piece(sBoard.BoardWPF, Slab.Parse(slb));
                //if (++wp >= w) {
                    //wp = 0;
                    //++hp;
                //}
            //}
            //return ret;
        //}

        public Piece this[int x, int y] {
            get { return multiverse[realityLevel][x, y]; }
            set { multiverse[realityLevel][x, y] = value; }
        }
        public Piece this[Coord c] {
            get { return this[c.absX, c.absY]; }
            set { this[c.absX, c.absY] = value; }
        }
        public Piece this[Guid id] {
            get { return Find(p => p.ID == id); }
        }

        public bool isVirtualMode() {
            return _abstractionLevel > 0;
        }
        public void increaseVirtualization() {
            var mat = (Piece[,])(multiverse[_abstractionLevel]).Clone();
            _abstractionLevel++;
            multiverse.Add(mat);
            writeLine(string.Format("Increased virtualization ({0})", _abstractionLevel));
        }
        public void decreaseVirtualization() {
            if (_abstractionLevel > 0) {
                multiverse.RemoveAt(_abstractionLevel);
                _abstractionLevel--;
                foreach (var p in multiverse[0])
                    p.cleanLocs();
                writeLine(string.Format("Decreased virtualization ({0})", _abstractionLevel));
            }
        }

        public IEnumerator<Piece> GetEnumerator() {
            foreach (var p in multiverse[realityLevel]) {
                yield return p;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        public bool MoveNext() {
            posx++;
            if (posx >= W) {
                posx = 0;
                posy++;
            }
            return (posx < W && posy < H);
        }
        public void Reset() {
            posx = 0;
            posy = 0;
        }
        public object Current {
            get { return this[posx, posy]; }
        }
        public List<Piece> ToList() {
            var ret = new List<Piece>(W * H);
            ret.AddRange(this);
            return ret;
        }
        public Piece Find(Predicate<Piece> match) {
            return ToList().Find(match);
        }

        public void PrintDebug() {
            string[] str = new string[H];
            int i = -1;
            for (int y = H-1; y > -1; y--) {
                i++;
                for (int x = 0; x < W; x++) {
                    var sy = this[x, y].symbol;
                    str[i] += sy != "" ? sy : "\u2002 ";
                }
            }
            foreach (var s in str)
                Console.WriteLine(s);
        }

        //public override string ToString() {
            //string mat = "";
            //var last = this[W, H];
            //foreach (Slab sl in this) {
                //mat += sl;
                //if (!sl.Equals(last))
                    //mat += "|";
            //}
            //return string.Format("{1},{2}[{3}]", W, H, mat);
        //}
    }
    public struct MatryxT : IEnumerable, IEnumerator {
        public readonly int W, H;
        Trait[,] traits;
        int posx, posy;

        public MatryxT(int w, int h) {
            W = w;
            H = h;
            traits = new Trait[w, h];
            posx = 0;
            posy = 0;
        }

        public Trait this[int x, int y] {
            get { return traits[x, y]; }
            set { traits[x, y] = value; }
        }
        public Trait this[Coord c] {
            get { return traits[c.absX, c.absY]; }
            set { traits[c.absX, c.absY] = value; }
        }
        public IEnumerator<Trait> GetEnumerator() {
            foreach (var p in traits) {
                yield return p;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        public bool MoveNext() {
            posx++;
            if (posx >= W) {
                posx = 0;
                posy++;
            }
            return (posx < W && posy < H);
        }
        public void Reset() {
            posx = 0;
            posy = 0;
        }
        public object Current {
            get { return traits[posx, posy]; }
        }
    }
}