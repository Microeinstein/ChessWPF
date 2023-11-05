using Micro;
using System;
using System.Text.RegularExpressions;
using Micro.NetLib;
using static ChessWPF.Core;

namespace ChessWPF {
    public class GameSet {
        public static Regex regx = new Regex(string.Format(@"^{0},{1},{2},{3},{4},{5},{6},{7}$",
                                                           @"[(\[{]?([-0-9a-f]{36})[)\]}]?",
                                                           @"[(\[{]?([-0-9a-f]{36})[)\]}]?",
                                                           @"[(\[{]?([-0-9a-f]{36})[)\]}]?",
                                                           @"(\d)",
                                                           @"((?:\d+\.)?\d{2}:\d{2}:\d{2}(?:\.\d+)?)",
                                                           @"(\d)", @"(\d)", @"(\d)"));
        public User white, black, admin;
        public Timing timeMode;
        public TimeSpan timeLimit;
        public bool undo, redo, draw;

        public User notMe => isWhite ? black : isBlack ? white : null;
        public bool isWhite => net.myID == (white?.id ?? Guid.Empty);
        public bool isBlack => net.myID == (black?.id ?? Guid.Empty);
        public bool isAdmin => net.myID == (admin?.id ?? Guid.Empty);
        public bool canPlay => remote && (isWhite || isBlack);
        public bool isEmpty => white == null || black == null;
        public int myTurn => net.connect.myself == white ? 0 :
                             net.connect.myself == black ? 1 : -1;

        public GameSet() {
            timeMode = lastTimeMode;
            timeLimit = lastTimeLimit;
        }
        public static GameSet Parse(string txt) {
            var grps = regx.Match(txt).Groups;
            return new GameSet() {
                white = net.connect.getUser(Guid.Parse(grps[1].Value)),
                black = net.connect.getUser(Guid.Parse(grps[2].Value)),
                admin = net.connect.getUser(Guid.Parse(grps[3].Value)),
                timeMode = (Timing)int.Parse(grps[4].Value),
                timeLimit = TimeSpan.Parse(grps[5].Value),
                undo = grps[6].Value == "1",
                redo = grps[7].Value == "1",
                draw = grps[8].Value == "1"
            };
        }
        public string getAttr(int index) {
            switch (index) {
                case -1: return ToString();
                case 0:  return (int)timeMode + "";
                case 1:  return timeLimit.ToString();
                case 3:  return undo ? "1" : "0";
                case 4:  return redo ? "1" : "0";
                case 5:  return draw ? "1" : "0";
                default: return "";
            }
        }
        public void setAttr(int index, string text) {
            switch (index) {
                case 0: timeMode = (Timing)int.Parse(text); break;
                case 1: timeLimit = TimeSpan.Parse(text);   break;
                case 2: swapPlayers();                      break;
                case 3: undo = text == "1";                 break;
                case 4: redo = text == "1";                 break;
                case 5: draw = text == "1";                 break;
            }
        }
        public void save() {
            if (!remote || (remote && net.connect.isServer)) {
                lastTimeMode = timeMode;
                lastTimeLimit = timeLimit;
            }
        }
        public void swapPlayers() {
            var pl1 = white;
            white = black;
            black = pl1;
        }
        public void passAdmin() {
            if (admin == white)
                admin = black;
            else
                admin = white;
        }
        public bool playing(User user) {
            return white == user || black == user;
        }

        public override string ToString() {
            return string.Join(",", white.id, black.id, admin.id,
                                    (int)timeMode, timeLimit,
                                    undo ? "1" : "0",
                                    redo ? "1" : "0",
                                    draw ? "1" : "0");
        }
    }
}
