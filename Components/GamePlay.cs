using Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Micro.NetLib;
using static ChessWPF.Core;

namespace ChessWPF {
    public class GamePlay {
        public static Regex regx = new Regex(string.Format(@"^{0}\|{1}\|{2}\|\[{3}\]\|\[{4}\]\|{5}$",
                                                           @"(.+)",
                                                           @"[(\[{]?([-0-9a-f]{36})[)\]}]?",
                                                           @"(.+)", @"(.*)",
                                                           @"(.*)", @"(\d*)"));
        public GameSet gameSet;
        public Guid id;
        bool _playing;
        public bool playing {
            get { return _playing; }
            set {
                if (_playing = value) {
                    if (net.connect.isServer)
                        clock?.start();
                } else if (net.connect.isServer)
                    clock?.stop();
            }
        }
        public WinRequest won;
        public List<Move> chrono;
        public int movCount;
        public List<User> spectators;
        public int turn {
            get { return movCount % players == 0 ? 0 : 1; }
        }
        public Clock clock;

        public GamePlay() {
            gameSet = new GameSet();
            id = Guid.NewGuid();
            playing = false;
            chrono = new List<Move>();
            movCount = 0;
            spectators = new List<User>();
            clock = new Clock();
        }
        public static GamePlay Parse(string txt) {
            var grps = regx.Match(txt).Groups;
            string g4 = grps[4].Value,
                   g5 = grps[5].Value,
                   g6 = grps[6].Value;
            var gp = new GamePlay() {
                    gameSet = GameSet.Parse(grps[1].Value),
                    id = Guid.Parse(grps[2].Value),
                    playing = grps[3].Value == "1" };
            if (!string.IsNullOrEmpty(g4)) gp.won = WinRequest.Parse(g4);
            if (!string.IsNullOrEmpty(g5)) gp.chrono = g5.Split('|').Select(s => Move.Parse(s)).ToList();
            if (!string.IsNullOrEmpty(g6)) gp.movCount = int.Parse(g6);
            return gp;
        }

        public void addChrono(Move m) {
            if (chrono.Count > movCount)
                chrono.RemoveRange(movCount, chrono.Count - movCount);
            if (chrono.Count == 0 || chrono[movCount - 1] != m)
                chrono.Add(m);
        }
        public void Merge(GamePlay b) {
            if (id == b.id) {
                gameSet = b.gameSet;
                playing = b.playing;
                chrono = b.chrono;
                movCount = b.movCount;
                spectators = b.spectators;
                won = b.won;
            }
        }
        public bool wonIs(WinType t) {
            return (match.won?.type ?? 0) == t;
        }
        
        public override string ToString() {
            return string.Join("|", gameSet, id, playing ? "1" : "0", "[" + won + "]",
                                    "[" + string.Join("|", chrono.Select(m => m.ToString())) + "]",
                                    movCount);
        }
        public static explicit operator GamePlay(GameID a) {
            return new GamePlay() { gameSet = a.gameSet, id = a.id, playing = a.playing };
        }
    }

    public class GameID {
        public GameSet gameSet;
        public Guid id;
        public bool playing;
        public WinRequest won;

        public override string ToString() {
            return string.Join("|", gameSet, id, playing ? "1" : "0", "[" + won + "]", "[]", "");
        }
        public static explicit operator GameID(GamePlay a) {
            return new GameID() { gameSet = a.gameSet, id = a.id, playing = a.playing, won = a.won };
        }
    }
}
