using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static System.Math;

namespace ChessWPF {
    public static class Core {
        #region Consts
        public const string logDir = "logs",
                            repDir = "replays",
                            replayExt = ".chessrep";
        public static readonly Regex regdig = new Regex(@"\d+"),
                                     regadr = new Regex(@"[\w-.]+");
        public static readonly double CellPadding = 4;
        public const int W = 8,
                         H = 8,
                         CellSize = 64,
                         players = 2,
                         rainbowSec = 10;
        #endregion
        #region Settings
        public static int FPS {
            get {
                var str = settings.read("Settings", "fps");
                if (!str.isDigit()) {
                    FPS = 60;
                    return 60;
                } else {
                    return int.Parse(str);
                }
            }
            set { settings.write("Settings", "fps", value.ToString()); }
        }
        public static string Nickname {
            get {
                var str = settings.read("Settings", "nickname");
                if (string.IsNullOrWhiteSpace(str)) {
                    Nickname = "Player";
                    return "Player";
                } else {
                    return str;
                }
            }
            set { settings.write("Settings", "nickname", value); }
        }
        public static bool Logs {
            get {
                var str = settings.read("Settings", "logs");
                if (string.IsNullOrWhiteSpace(str)) {
                    Logs = true;
                    return true;
                } else {
                    return int.Parse(str) == 1 ? true : false;
                }
            }
            set { settings.write("Settings", "logs", (value ? 1 : 0).ToString()); }
        }
        public static bool Replays {
            get {
                var str = settings.read("Settings", "replays");
                if (string.IsNullOrWhiteSpace(str)) {
                    Replays = true;
                    return true;
                } else {
                    return int.Parse(str) == 1 ? true : false;
                }
            }
            set { settings.write("Settings", "replays", (value ? 1 : 0).ToString()); }
        }
        public static Timing lastTimeMode {
            get {
                var str = settings.read("Last", "timemode");
                if (!str.isDigit()) {
                    lastTimeMode = 0;
                    return 0;
                } else {
                    return (Timing)int.Parse(str);
                }
            }
            set { settings.write("Last", "timemode", value.ToString()); }
        }
        public static TimeSpan lastTimeLimit {
            get {
                var str = settings.read("Last", "countdown");
                if (string.IsNullOrWhiteSpace(str)) {
                    var n = new TimeSpan(1, 0, 0);
                    lastTimeLimit = n;
                    return n;
                } else {
                    return TimeSpan.Parse(str);
                }
            }
            set { settings.write("Last", "countdown", value.ToString()); }
        }
        public static string lastHostname {
            get {
                var str = settings.read("Last", "hostname");
                if (string.IsNullOrWhiteSpace(str)) {
                    lastHostname = "127.0.0.1";
                    return "127.0.0.1";
                } else {
                    return str;
                }
            }
            set { settings.write("Last", "hostname", value); }
        }
        public static int lastPort {
            get {
                var str = settings.read("Last", "port");
                if (!str.isDigit()) {
                    lastPort = 4826;
                    return 4826;
                } else {
                    return int.Parse(str);
                }
            }
            set { settings.write("Last", "port", value.ToString()); }
        }
        #endregion

        #region Definitions ~ Shortcuts
        public static bool noTurns = false,
                           deletePieces = false,
                           debugMoves = false,
                           debugSeries = false,
                           debugSpecial = false,
                           remote = false;
                           //inTransition = false;
        public static bool inGame {
            get { return GState.HasFlag(GameState.Game); }
        }
        public static bool logsExists {
            get { return Directory.Exists(logDir); }
        }
        public static bool replExists {
            get { return Directory.Exists(repDir); }
        }
        public static int realW { get { return W * CellSize; } }
        public static int realH { get { return H * CellSize; } }

        public static StreamWriter logFile, replayFile;
        public static Random rnd;
        public static SoundPlayer media;
        public static Network net;
        public static IniFile settings;
        public static GamePlay match { get; set; }
        public static Tuple<List<ReplayNode>, List<Move>> replay;
        public static Gamemodes gamemode;
        public static GameState GState;
        public static Timing timeMode;
        public static Guid eating, tempID;

        public static Base sBase;
        public static Back sBack;
        public static Debug sDebug;
        public static PlayFrame sPlayFrame;
        public static RemoteFrame sRemoteFrame;
        public static ChessBoard sBoard;
        public static PlayMenu sPlayMenu;
        public static ReplayMenu sReplayMenu;
        public static Promotion sPromotion;
        public static Win sWin;
        public static Frame fNow, fShown;
        public static FrameworkPropertyMetadata fpm;
        #endregion
        #region Functions
        public static void init() {
            Environment.CurrentDirectory = Directory.GetParent(Environment.GetCommandLineArgs()[0]).FullName;
            settings = new IniFile("settings.cfg");
            rnd = new Random();
            net = new Network();
            match = new GamePlay();
            gamemode = Gamemodes.Local;
            GState = GameState.Menu;
            eating = Guid.Empty;
            fpm = new FrameworkPropertyMetadata() { DefaultValue = FPS };
        }

        public static List<double> makeCircled(Piece p, ref List<Piece> moves) {
            int px = p.loc.absX,
                py = p.loc.absY;

            List<double> order = new List<double>();
            foreach (var m in moves) {
                int mx = m.loc.absX,
                    my = m.loc.absY;
                bool distx = py == my,
                     disty = px == mx;
                double md;

                if (distx)
                    md = mx - px;
                else if (disty)
                    md = my - py;
                else
                    md = Sqrt(Pow(mx - px, 2) + Pow(my - py, 2));
                md = Abs(md);
                order.Add(Ceiling(md));
            }
            bubbleSort(ref moves, ref order);

            return order;
        }
        public static List<double> makeCircled(Piece p, ref List<Move> moves) {
            int px = p.loc.absX,
                py = p.loc.absY;

            List<double> order = new List<double>();
            foreach (var m in moves) {
                int mx = m.loc.absX,
                    my = m.loc.absY;
                bool distx = py == my,
                     disty = px == mx;
                double md;

                if (distx)      md = mx - px;
                else if (disty) md = my - py;
                else            md = Sqrt(Pow(mx - px, 2) + Pow(my - py, 2));
                md = Abs(md);
                order.Add(Ceiling(md));
            }
            bubbleSort(ref moves, ref order);

            return order;
        }
        public static void bubbleSort<T1, T2>(ref List<T1> list, ref List<T2> by) {
            double _test;
            if (list.Count == by.Count && list.Count > 0 && double.TryParse(by[0].ToString(), out _test)) {
                bool changes = true;
                while (changes) {
                    changes = false;
                    for (int i = 0; i < list.Count - 1; i++) {
                        T1 o1 = list[i],
                           o2 = list[i + 1];
                        T2 b1 = by[i],
                           b2 = by[i + 1];
                        double n1 = double.Parse(by[i].ToString()),
                               n2 = double.Parse(by[i + 1].ToString());

                        if (n1 > n2) {
                            changes = true;
                            list[i] = o2;
                            list[i + 1] = o1;
                            by[i] = b2;
                            by[i + 1] = b1;
                        }
                    }
                }
            }
        }
        public static double DegToRad(double angle) {
            return angle * (PI / 180);
        }
        public static TimeSpan now() {
            return new TimeSpan(DateTime.Now.Ticks);
        }
        public static Color ColorFromHSL(double h, double s, double l) {
            double r = 0,
                   g = 0,
                   b = 0;
            if (l != 0) {
                if (s == 0)
                    r = g = b = l;
                else {
                    double temp2;
                    if (l < 0.5)
                        temp2 = l * (1.0 + s);
                    else
                        temp2 = l + s - (l * s);

                    double temp1 = 2.0 * l - temp2;

                    r = GetColorComponent(temp1, temp2, h + 1.0 / 3.0);
                    g = GetColorComponent(temp1, temp2, h);
                    b = GetColorComponent(temp1, temp2, h - 1.0 / 3.0);
                }
            }
            return Color.FromRgb((byte)(255 * r), (byte)(255 * g), (byte)(255 * b));
        }
        private static double GetColorComponent(double temp1, double temp2, double temp3) {
            if (temp3 < 0.0)
                temp3 += 1.0;
            else if (temp3 > 1.0)
                temp3 -= 1.0;

            if (temp3 < 1.0 / 6.0)
                return temp1 + (temp2 - temp1) * 6.0 * temp3;
            else if (temp3 < 0.5)
                return temp2;
            else if (temp3 < 2.0 / 3.0)
                return temp1 + ((temp2 - temp1) * ((2.0 / 3.0) - temp3) * 6.0);
            else
                return temp1;
        }
        public static SolidColorBrush rainbowColor() {
            var now = DateTime.Now;
            return new SolidColorBrush(ColorFromHSL((loopMinMax(now.Second, rainbowSec) * 1000 + now.Millisecond) / (1000d * rainbowSec), 1d, 0.425d));
        }
        public static double loopMinMax(double n, double max) {
            return n - (max * Truncate(n / max));
        }
        public static bool In<T>(this T obj, params T[] args) {
            return args.Contains(obj);
        }
        public static void Invoke(Action method) {
            sBase.Dispatcher.Invoke(method);
        }
        public static void Invoke<T1>(Action<T1> method, T1 arg1) {
            sBase.Dispatcher.Invoke(method, arg1);
        }
        public static void Invoke<T1, T2>(Action<T1, T2> method, T1 arg1, T2 arg2) {
            sBase.Dispatcher.Invoke(method, arg1, arg2);
        }
        public static void Invoke<T1, T2, T3>(Action<T1, T2, T3> method, T1 arg1, T2 arg2, T3 arg3) {
            sBase.Dispatcher.Invoke(method, arg1, arg2, arg3);
        }
        public static void Invoke<T1, T2, T3, T4>(Action<T1, T2, T3, T4> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
            sBase.Dispatcher.Invoke(method, arg1, arg2, arg3, arg4);
        }

        public static bool isDigit(this string str) {
            return (!string.IsNullOrWhiteSpace(str)) && (regdig.Match(str).Value == str);
        }
        public static bool isAddress(this string str) {
            return (!string.IsNullOrWhiteSpace(str)) && (regadr.Match(str).Value == str);
        }
        public static bool isFirst(this int player) {
            return player == 0;
        }
        public static void split(this Tuple<bool, Move> combo, out bool canMake, out Move theMove) {
            canMake = combo.Item1;
            theMove = combo.Item2;
        }
        public static bool specialMove(this MoveType type, bool making = false) {
            return (making && type.HasFlag(MoveType.PawnFirstMove)) ||
                   type.HasFlag(MoveType.Passant) ||
                   type.HasFlag(MoveType.Promotion) ||
                   type.HasFlag(MoveType.Castling);
        }
        public static void throwEvent(this Action eevent) {
            if (eevent != null)
                eevent.Invoke();
        }
        public static void throwEvent<T>(this Action<T> eevent, T arg) {
            if (eevent != null)
                eevent.Invoke(arg);
        }
        public static void beginAnimation(this FrameworkElement uc, string storyboard) {
            var sb = (Storyboard)uc.FindResource(storyboard);
            sb.Begin();
        }
        public static void stopAnimation(this FrameworkElement uc, string storyboard) {
            var sb = (Storyboard)uc.FindResource(storyboard);
            sb.Stop();
        }
        public static void setFPS(int fps) {
            FPS = fps;
            //fpm.DefaultValue = fps;
        }

        public static async Task playSound() {
            await Task.Run(delegate {
                var path = Path.GetFullPath(string.Format(@"Resources\fx_{0}.wav", rnd.Next(1, 4)));
                media = new SoundPlayer(path);
                media.Play();
            });
        }
        #endregion

        #region Log ~ Replay
        public static void prepareLog() {
            if (Logs) {
                if (!logsExists)
                    Directory.CreateDirectory(logDir);
                logFile = File.CreateText(Path.Combine(logDir, getNextLog()));
            }
        }
        public static string getNextLog() {
            if (logsExists) {
                var logs = Directory.GetFiles(logDir, "log_*.txt", SearchOption.TopDirectoryOnly);
                return "log_" + logs.Length + ".txt";
            } else
                return "";
        }
        public static void write(string text) {
            if (Logs)
                logFile.Write(text);
            Console.Write(text);
        }
        public static void writeLine(string text) {
            if (Logs)
                logFile.Write(text + Environment.NewLine);
            Console.WriteLine(text + Environment.NewLine);
        }

        public static void prepareReplay() {
            if (Replays) {
                if (!replExists)
                    Directory.CreateDirectory(repDir);
                replayFile = File.CreateText(Path.Combine(repDir, getNextReplay()));
            }
        }
        public static string getNextReplay() {
            if (replExists) {
                var replays = Directory.GetFiles(repDir, "replay_*" + replayExt, SearchOption.TopDirectoryOnly);
                return "replay_" + replays.Length + replayExt;
            } else
                return "";
        }
        public static void replayMove(Move m) {
            if (Replays)
                replayFile.WriteLine((int)ReplayNode.Move + " " + m.ToString());
        }
        public static void replayUndo() {
            if (Replays)
                replayFile.WriteLine((int)ReplayNode.Undo);
        }
        public static void replayRedo() {
            if (Replays)
                replayFile.WriteLine((int)ReplayNode.Redo);
        }
        public static void replayEnd() {
            if (Replays)
                replayFile.Close();
        }
        public static void replayRead(string path) {
            var retM = new List<Move>();
            var retR = new List<ReplayNode>();
            string[] content = File.ReadAllLines(path),
                     spl;
            ReplayNode action;

            try {
                foreach (var line in content) {
                    if (!string.IsNullOrEmpty(line)) {
                        spl = line.Split(' ');
                        action = (ReplayNode)int.Parse(spl[0]);
                        if (action == ReplayNode.Move)
                            retM.Add(Move.Parse(spl[1]));
                        retR.Add(action);
                    }
                }
            } catch (Exception) {
                MessageBox.Show(
                    "Unable to read " + Path.GetFileName(path) + " (Invalid content).",
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                replay = null;
            }
            replay = new Tuple<List<ReplayNode>, List<Move>>(retR, retM);
        }
        #endregion
    }
}
