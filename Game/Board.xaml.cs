#pragma warning disable CS4014
using Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Micro.NetLib;
using static ChessWPF.Core;

namespace ChessWPF {
    public partial class Board {
        public UIElementCollection scene, grid;
        public Piece top;
        public Trait target;
        public Move promoting;
        public MatryxT traits;
        public MatryxP pieces;
        public List<Move> chrono;
        public List<Piece> graveyard;
        public int movCount, frameTop;
        int _turn, replayCount;
        bool cW, cB, surrender, timesUp, countdownMode;
        Clock clock;
        TimeSpan timeLimit, lastAction = now();
        TimeSpan timeWhite {
            get {
                if (countdownMode) {
                    var sub = timeLimit - clock.timeWhite;
                    return sub < TimeSpan.Zero ? TimeSpan.Zero : sub;
                } else
                    return clock.timeWhite;
            }
        }
        TimeSpan timeBlack {
            get {
                if (countdownMode) {
                    var sub = timeLimit - clock.timeBlack;
                    return sub < TimeSpan.Zero ? TimeSpan.Zero : sub;
                } else
                    return clock.timeBlack;
            }
        }
        Dictionary<Piece, Guid> hlMoves;

        public int turn {
            get { return _turn; }
            set { _turn = fixTurn(value); }
        }
        
        public Board() : this(false) { }
        public Board(bool noClone) {
            InitializeComponent();
            makeBackground();

            Width = realW;
            Height = realH;
            scene = Scene.Children;
            grid = Grid.Children;
            turn = 0;
            hlMoves = new Dictionary<Piece, Guid>();
            chrono = new List<Move>();
            graveyard = new List<Piece>();

            traits = new MatryxT(W, H);
            pieces = new MatryxP(W, H);
            for (int y = 0; y < H; y++) {
                for (int x = 0; x < W; x++) {
                    var t = new Trait(x, y);
                    var p = new Piece(this, x, y);
                    traits[x, y] = t;
                    pieces[x, y] = p;
                    scene.Add(t);
                    grid.Add(p);
                }
            }
            //if (!noClone)
            //    cloneAll(this);
            clock = new Clock();
            clock.Tick += clockTick;
        }

        public void makeBackground() {
            Brush imgBrushA, imgBrushB;
            try {
                imgBrushA = new ImageBrush() { ImageSource = new BitmapImage(new Uri(@"pack://application:,,,/Resources/wood2.bmp")) };
                imgBrushB = new ImageBrush() { ImageSource = new BitmapImage(new Uri(@"pack://application:,,,/Resources/wood1.bmp")) };
            } catch (Exception) {
                imgBrushA = new SolidColorBrush(Colors.Tan);
                imgBrushB = new SolidColorBrush(Colors.Sienna);
            }
            RenderOptions.SetCachingHint(imgBrushA, CachingHint.Cache);
            RenderOptions.SetBitmapScalingMode(imgBrushA, BitmapScalingMode.LowQuality);
            RenderOptions.SetCachingHint(imgBrushB, CachingHint.Cache);
            RenderOptions.SetBitmapScalingMode(imgBrushB, BitmapScalingMode.LowQuality);
            bmp.Viewport = new Rect(0, 0, CellSize * 2, CellSize * 2);
            rect1.Rect = new Rect(0, 0, CellSize, CellSize);
            rect2.Rect = new Rect(CellSize, 0, CellSize, CellSize);
            rect3.Rect = new Rect(0, CellSize, CellSize, CellSize);
            rect4.Rect = new Rect(CellSize, CellSize, CellSize, CellSize);
            bmp1.Brush = imgBrushA;
            bmp2.Brush = imgBrushB;
            bmp3.Brush = imgBrushB;
            bmp4.Brush = imgBrushA;
        }

        public Piece this[int x, int y] {
            get { return pieces[x, y]; }
            set { pieces[x, y] = value; }
        }
        public Piece this[Coord c] {
            get { return pieces[c.absX, c.absY]; }
            set { pieces[c.absX, c.absY] = value; }
        }

        public void clear() {
            writeLine("Board cleared");
            for (int y = 0; y < pieces.H; y++) {
                for (int x = 0; x < pieces.W; x++) {
                    insert(new Piece(this, x, y));
                }
            }
            resetTime();
            chrono.Clear();
            graveyard.Clear();
            movCount = 0;
            turn = 0;
            replayCount = 0;
            cW = false;
            cB = false;
            frameTop = 0;
            hlMoves.Clear();
            top = null;
            target = null;
            surrender = false;
            timesUp = false;
            countdownMode = false;
            timeLimit = TimeSpan.Zero;
            sWin.Hide();
            refreshTraits(null);
            updateInfo();
        }
        public void resetTime() {
            clock.reset();
        }
        //public void tuneMap(MatryxP map) {
            //grid.Clear();
            //pieces = map;
            //foreach (var p in pieces) {
                //grid.Add(p);
            //}
        //}
        public void tune(int side, Gamemodes gm) {
            gamemode = gm;
            if (countdownMode = (match.gameSet.timeMode == Timing.Limit))
                timeLimit = match.gameSet.timeLimit;
            Rotation.Angle = 90 * side;
            foreach (var p in pieces) {
                p.setRot(-90 * side);
            }
        }
        public void start() {
            writeLine("");
            writeLine(string.Format("## Started game ({0}) ##", gamemode));
            GState = GameState.Game;
            if (gamemode != Gamemodes.Replay) {
                prepareReplay();
                clock.start();
            }
            updateInfo();
        }
        public void insert(Piece p) {
            Coord loc = p.loc;
            if (!pieces.isVirtualMode() && grid.Contains(pieces[loc]))
                grid.Remove(pieces[loc]);
            pieces[loc] = p;
            top = p;
            if (!grid.Contains(p))
                grid.Add(p);
        }
        public void swap(Coord loc1, Coord loc2) {
            Piece p1 = pieces[loc1],
                  p2 = pieces[loc2];
            pieces[loc1] = p2;
            pieces[loc2] = p1;
            p1.translate(loc2);
            p2.translate(loc1);
        }
        public void remove(Piece p) {
            if (!pieces.isVirtualMode()) {
                if (grid.Contains(p))
                    grid.Remove(p);
            }
        }
        public void vanish(Piece p) {
            var p_air = new Piece(this, p.loc.absX, p.loc.absY);

            p.delete();
            
            pieces[p.loc] = p_air;
            if (!pieces.isVirtualMode())
                grid.Add(p_air);
        }
        public void eat(Piece p, Coord loc, bool noMove = false) {
            var p_del = pieces[loc];
            var p_air = new Piece(this, p.loc.absX, p.loc.absY);

            p_del.delete();

            pieces[loc] = p;
            pieces[p.loc] = p_air;
            p.translate(loc);
            if (!pieces.isVirtualMode())
                grid.Add(p_air);
        }
        public void restoreLast() {
            if (graveyard.Count > 0) {
                if (eating != Guid.Empty)
                    graveyard.Find(p => p.ID == eating).undelete();
                else {
                    var last = graveyard.Last();
                    insert(last);
                    last.isEaten = false;
                    graveyard.Remove(last);
                }
            }
        }
        public void selectPromote(Move m) {
            promoting = m;
            frameTop++;
            sPromotion.appear();
        }
        public void promote(SlabType type) {
            promoting.p.promote(type);
            promoting.promoted = type;
            if (!pieces.isVirtualMode()) {
                replayMove(promoting);
                addChrono(promoting);
            }
            frameTop--;
            nextTurn();
        }
        public void win(WinType mode, User subject = null) {
            if (gamemode != Gamemodes.Replay) {
                writeLine(string.Format("Game over: {0} {1}", mode,
                                            surrender ? "(Surrender)" :
                                            timesUp ? "(Time's Up)" :
                                            ""));
                if (GState != GameState.Win) {
                    replayEnd();
                    GState = GameState.Win;
                    frameTop++;
                    if (!countdownMode)
                        updateTime();
                    if (gamemode == Gamemodes.Remote && !net.winning && match.wonIs(0)) {
                        subject = net.getSubject(surrender ? WinType.GiveUp : mode);
                        if (!surrender)
                            net.winGame(mode, subject);
                        else
                            net.winGame(WinType.GiveUp, subject);
                    }
                    sWin.Show(mode, subject);
                }
            }
        }
        public void endGame() {
            int prTurn = prevTurn(turn);
            var ck = cKingCheck(turn);
            sKingInCheck(turn, ck.Item1);
            if (cKingCheckMate(turn)) {
                if (ck.Item1)
                    win((WinType)(prTurn + 1));
                else
                    win(WinType.Stale);
            }
            ck.Item2.isInDanger = frameTop > 0 ? false : ck.Item1;
            if (turn == 0 ? cB : cW) {
                int nt = nextTurn(turn);
                var ck2 = cKingCheck(nt);
                sKingInCheck(nt, ck2.Item1);
                ck2.Item2.isInDanger = frameTop > 0 ? false : ck2.Item1;
            }
        }
        public void askGiveUp() {
            frameTop++;
            sBase.DialogGiveUp.IsOpen = true;
        }
        public void answerGiveUp(bool value) {
            frameTop--;
            if (surrender = value) {
                win(turn.isFirst() ? WinType.Black : WinType.White);
            }
        }
        public void updateInfo() {
            if (gamemode == Gamemodes.Local)
                sDebug.updateInfo(turn, chrono.Count, movCount);
            if (gamemode != Gamemodes.Replay) {
                if (gamemode == Gamemodes.Remote ? net.matching : true)
                    sPlayMenu.updateInfo(turn, chrono.Count, movCount, timeWhite, timeBlack);
            } else
                sReplayMenu.update(replay.Item1.Count - 1, replayCount);
        }

        public void addChrono(Move m) {
            if (chrono.Count > movCount)
                chrono.RemoveRange(movCount, chrono.Count - movCount);
            if (chrono.Count == 0 || chrono[movCount - 1] != m)
                chrono.Add(m);
            updateInfo();
        }
        public void undoLast() {
            if (frameTop == 0 && chrono.Count > 0 && movCount > 0) {
                chrono[movCount - 1].undo();
                writeLine(string.Format("Undo performed ({0}h {1}a)", chrono.Count, movCount));
                prevTurn();
            }
        }
        public void redoLast() {
            if (frameTop == 0 && chrono.Count > 0 && chrono.Count >= movCount + 1) {
                chrono[movCount].perform(true);
                writeLine(string.Format("Redo performed ({0}h {1}a)", chrono.Count, movCount));
                nextTurn();
            }
        }
        public bool nextActionIntervall() {
            var n = now();
            var sub = n - lastAction;
            
            if (sub.Milliseconds > 125) {
                lastAction = now();
                return true;
            } else
                return false;
        }
        public void replayPrev() {
            var history = replay.Item1.Count;
            if (history > 0 && replayCount > 0 && nextActionIntervall()) {
                var action = replay.Item1[replayCount - 1];
                replayCount--;
                switch (action) {
                    case ReplayNode.Move:
                        replay.Item2[movCount - 1].undo();
                        prevTurn();
                        break;
                    case ReplayNode.Undo:
                        redoLast();
                        break;
                    case ReplayNode.Redo:
                        undoLast();
                        break;
                }
            }
        }
        public void replayNext() {
            var history = replay.Item1.Count;
            if (history > 0 && replayCount < history && nextActionIntervall()) {
                var action = replay.Item1[replayCount];
                replayCount++;
                switch (action) {
                    case ReplayNode.Move:
                        replay.Item2[movCount].perform();
                        nextTurn();
                        break;
                    case ReplayNode.Undo:
                        undoLast();
                        break;
                    case ReplayNode.Redo:
                        redoLast();
                        break;
                }
            }
        }

        public void prevTurn() {
            clearLastMoves();
            clock.restart();
            turn--;
            clock.setTurn(turn);
            if (!net.moving)
                endGame();
            movCount--;
            refreshTraits(null);
            updateInfo();
            writeLine("### Turn: " + turn.isFirst());
        }
        public void nextTurn() {
            if (gamemode == Gamemodes.Remote && match.gameSet.canPlay)
                net.performMove(chrono.Last());
            clearLastMoves();
            clock.restart();
            turn++;
            clock.setTurn(turn);
            endGame();
            movCount++;
            refreshTraits(null);
            updateInfo();
            writeLine("### Turn: " + turn);

            if (GState == GameState.Game) {
                var prevK = findKing(prevTurn(turn));
                bool strange = cEatable(prevK, new Move(0, 0, prevK));
                if (noTurns) {
                    var nowK = findKing(turn);
                    strange |= cEatable(nowK, new Move(0, 0, nowK));
                }
                if (strange)
                    win(WinType.Strange);
            }
        }

        public int prevTurn(int turn) {
            return fixTurn(_turn - 1);
        }
        public int nextTurn(int turn) {
            return fixTurn(_turn + 1);
        }
        public int fixTurn(int value) {
            if (_turn + value >= players)
                return 0;
            else if (_turn + value < 0)
                return players - 1;
            else
                return value;
        }

        void clockTick() {
            updateTime();

            if (!inGame || timesUp)
                clock.stop();
        }
        void updateTime() {
            if (countdownMode) {
                if (turn == 0 && timeWhite <= TimeSpan.Zero) {
                    timesUp = true;
                    win(WinType.Black);
                } else if (turn == 1 && timeBlack <= TimeSpan.Zero) {
                    timesUp = true;
                    win(WinType.White);
                }
            }
            updateInfo();
        }
        public void setTime(Clock c) {
            clock.Merge(c);
            updateTime();
        }

        public void refreshTraits(Piece p) {
            Move? lastM = null;
            bool mNull = false,
                isLast = false;
            if (chrono.Count > 0 && movCount > 0)
                lastM = chrono[movCount - 1];
            mNull = lastM != null;
            foreach (var t in traits) {
                isLast = false;
                if (mNull) {
                    isLast = t.loc == lastM.Value.oldLoc || t.loc == lastM.Value.loc;
                    if (!isLast && lastM.Value.isSpecial())
                        isLast = t.loc == lastM.Value.sloc;
                }
                t.isLast = isLast;
            }
        }
        public async Task highlightMoves(Piece p, List<Move> muves, bool how) {
            Guid guid = Guid.NewGuid();
            if (!hlMoves.ContainsKey(p))
                hlMoves.Add(p, guid);
            else
                hlMoves[p] = guid;

            foreach (var m in muves) {
                var tm = traits[m.loc];
                tm.fromWhat = p;
            }

            List<Move> moves = muves.Where(x => traits[x.loc].isMove != how).ToList();
            List<double> order = makeCircled(p, ref moves);
            if (!how) {
                moves.Reverse();
                order.Reverse();
            }
            double distance = order[0];

            for (int i = 0; i < moves.Count; i++) {
                var m = moves[i];
                var d = i < moves.Count - 1 ? order[i + 1] : distance;
                var tm = traits[m.loc];

                if (tm.fromWhat == p && hlMoves[p] == guid) {
                    tm.isSpecial = m.isSpecial();
                    tm.isMove = how;
                    if (d != distance) {
                        distance = d;
                        await Task.Delay(TimeSpan.FromSeconds(0.1));
                    }
                }
            }
            if (hlMoves[p] == guid)
                hlMoves.Remove(p);
        }
        public void highlightTarget(Piece p) {
            if (p.myTurn) {
                Position center = p.Margin + new Position(CellSize / 2, CellSize / 2);
                center.x /= CellSize;
                center.y /= CellSize;
                Coord loc = center;

                if (cIn(loc)) {
                    if (target != null)
                        target.isTarget = false;
                    var tt = traits[loc];
                    if (p.moves().Exists(x => x.loc == loc)) {
                        target = tt;
                        target.isTarget = true;
                    }
                }
            }
        }
        public Trait findTarget(Piece avoid) {
            foreach (var t in traits) {
                if (t != (Trait)avoid && t.isTarget)
                    return t;
            }
            return null;
        }
        public void clearJustTouched() {
            foreach (var p in pieces)
                p.justTouched = false;
        }
        public void clearLastMoves() {
            foreach (var p in pieces) {
                p.lastMoves.Clear();
                p.cLastMoves = false;
            }
        }
        public Piece findKing(int player) {
            return pieces.Find(p => p.player == player && p.type == SlabType.King);
        }

        #region Controllers
        public bool cIn(int x, int y) {
            return (x >= 0 && y >= 0 && x < W && y < H);
        }
        public bool cEmpty(int x, int y) {
            return !pieces[x, y].isReal();
        }
        public bool cExist(int x, int y) {
            return !cEmpty(x, y);
        }
        public bool cEnemy(Piece p, int x, int y) {
            if (cExist(x, y)) {
                return pieces[x, y].player != p.player;
            } else
                return false;
        }
        public bool cFriend(Piece p, int x, int y) {
            if (cExist(x, y)) {
                return pieces[x, y].player == p.player;
            } else
                return false;
        }
        public bool cEatable(Piece p, int x = -1, int y = -1) {
            var loc = p.loc;
            if (x >= 0) loc.relX = x;
            if (y >= 0) loc.relY = y;
            return cEatable(p, loc);
        }

        public bool cIn(Coord loc) {
            int x = loc.absX,
                y = loc.absY;
            return (x >= 0 && y >= 0 && x < W && y < H);
        }
        public bool cEmpty(Coord loc) {
            int x = loc.absX,
                y = loc.absY;
            return !pieces[x, y].isReal();
        }
        public bool cExist(Coord loc) {
            int x = loc.absX,
                y = loc.absY;
            return !cEmpty(x, y);
        }
        public bool cEnemy(Piece p, Coord loc) {
            int x = loc.absX,
                y = loc.absY;
            if (cExist(x, y)) {
                return pieces[x, y].player != p.player;
            } else
                return false;
        }
        public bool cFriend(Piece p, Coord loc) {
            int x = loc.absX,
                y = loc.absY;
            if (cExist(x, y)) {
                return pieces[x, y].player == p.player;
            } else
                return false;
        }

        /// <summary>
        /// Controlla se la pedina può essere mangiata nella posizione data.
        /// </summary>
        /// <param name="p">Pedina da controllare.</param>
        /// <param name="loc">Coordinate dove la pedina verrà controllata.</param>
        public bool cEatable(Piece p, Coord loc) {
            int dX = loc.relX - p.loc.relX,
                dY = loc.relY - p.loc.relY;

            writeLine("");
            writeLine(string.Format("Checking {0} in {1}", p, loc));
            pieces.increaseVirtualization();
            var m = new Move(dX, dY, p, MoveType.Normal);
            if (!m.isNull())
                m.perform();
            List<Piece> enemies = pieces.Where(e => e.player != p.player && e.isReal()).ToList();
            var ret = cCrash(m.loc, m.p, enemies);
            pieces.decreaseVirtualization();
            writeLine(string.Format("Checked ({0})", ret));
            writeLine("");

            return ret;
        }
        /// <summary>
        /// Controlla se la pedina può essere mangiata dopo l'esecuzione della mossa data.
        /// </summary>
        /// <param name="p">Pedina da controllare.</param>
        /// <param name="m">Mossa da eseguire.</param>
        public bool cEatable(Piece p, Move m) {
            writeLine("");
            writeLine(string.Format("Checking {0} moving {1}", p, m));
            pieces.increaseVirtualization();
            if (!m.isNull())
                m.perform();
            List<Piece> enemies = pieces.Where(e => e.player != p.player && e.isReal()).ToList();
            var ret = cCrash(p.loc, p, enemies);
            pieces.decreaseVirtualization();
            writeLine(string.Format("Checked ({0})", ret));
            writeLine("");

            return ret;
        }
        public bool gKingInCheck(int player) {
            if (player.isFirst())
                return cW;
            else if (turn == 1)
                return cB;
            else
                return false;
        }
        public void sKingInCheck(int player, bool value) {
            if (player.isFirst())
                cW = value;
            else if (turn == 1)
                cB = value;
        }

        public Tuple<bool, Piece> cKingCheck(int player) {
            Piece king = findKing(player);
            List<Piece> enemies = pieces.Where(p => p.player != player && p.isReal()).ToList();
            return new Tuple<bool, Piece>(cCrash(king.loc, king, enemies), king);
        }
        public bool cKingCheckMate(int player) {
            Piece king = findKing(player);
            List<Piece> friends = pieces.Where(p => p.player == player && p.isReal()).ToList();
            return cTrap(king, friends);
        }
        public bool cStaleMate(int player) {
            List<Piece> friends = pieces.Where(p => p.player == player && p.isReal()).ToList();
            return cNoMoves(friends);
        }
        public bool cCrash(Coord point, Piece origin, List<Piece> bullets) {
            bool ret = false;
            makeCircled(origin, ref bullets);

            foreach (var b in bullets) {
                var bov = b.moves(true, true);
                if (bov.Exists(m => m.loc == point && (m.type.HasFlag(MoveType.Normal) || m.type.HasFlag(MoveType.OnlyAttack)))) {
                    ret = true;
                    break;
                }
            }

            return ret;
        }
        public bool cTrap(Piece p, List<Piece> pellets) {
            bool ret = true;
            makeCircled(p, ref pellets);

            foreach (var b in pellets) {
                if (b.moves(true, true).Exists(m => m.canSaveKing())) {
                    ret = false;
                    break;
                }
            }

            return ret;
        }
        public bool cNoMoves(List<Piece> pellets) {
            bool ret = true;

            foreach (var f in pellets) {
                if (f.moves(true).Count > 0) {
                    ret = false;
                    break;
                }
            }
            return ret;
        }
        #endregion
    }
}