#pragma warning disable CS4014
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static ChessWPF.Coord;
using static ChessWPF.Core;
using static ChessWPF.Move;

namespace ChessWPF {
    public partial class Piece {
        public Board B;
        public List<Move> lastMoves;
        Position mOld, mNew, mBack;
        Dictionary<int, Coord> locs;
        public Guid ID;
        public string symbol;
        public bool cLastMoves = false;
        public bool fakePromote = false;
        public double baseAngle { get; private set; } = 0;

        public new Coord loc {
            get {
                cleanLocs();
                if (locs.ContainsKey(B.pieces.realityLevel))
                    return locs[B.pieces.realityLevel];
                else {
                    var niu = locs[B.pieces.realityLevel - 1];
                    locs.Add(B.pieces.realityLevel, niu);
                    return niu;
                }
            }
            set {
                cleanLocs();
                if (locs.ContainsKey(B.pieces.realityLevel))
                    locs[B.pieces.realityLevel] = value;
                else
                    locs.Add(B.pieces.realityLevel, value);
            }
        }
        public new SlabType type {
            get { return fakePromote ? SlabType.Queen : base.type; }
            set {
                fakePromote = false;
                base.type = value;
            }
        }

        #region States
        public bool
            unTouched = true,
            firstMove = false,
            justTouched = false,
            isEaten = false;
        bool
            mHold = false,
            _isHover = false,
            _isPress = false,
            _isInDanger = false;

        public bool isHover {
            get { return _isHover; }
            set {
                if (_isHover != value) {
                    _isHover = value;
                    this.beginAnimation(value ? "Hover" : "Leave");
                    if (!value)
                        _isPress = false;
                }
            }
        }
        public bool isPress {
            get { return _isPress; }
            set {
                if (_isPress != value) {
                    _isPress = value;
                    this.beginAnimation(value ? "Press" : (_isHover ? "Hover" : "Leave"));
                }
            }
        }
        public bool isInDanger {
            get { return _isInDanger; }
            set {
                if (value)
                    inDangerOn();
                _isInDanger = value;
            }
        }
        public bool myTurn {
            get {
                bool remV = true;
                if (gamemode == Gamemodes.Remote)
                    remV = match.gameSet.canPlay && player == match.gameSet.myTurn;
                return player == B.turn && (gamemode != Gamemodes.Replay) && remV;
            }
        }
        #endregion

        public Piece() {
            InitializeComponent();
        }
        public Piece(Board b, int x, int y, int pl = 0, SlabType ty = SlabType.Null) : base(x, y, pl, ty) {
            InitializeComponent();
            Width = CellSize;
            Height = CellSize;
            B = b;
            locs = new Dictionary<int, Coord>();
            loc = base.loc;
            Margin = new Position(loc.absX * CellSize, loc.absY * CellSize);
            if (isReal())
                Visibility = Visibility.Visible;
            mBack = Margin;
            lastMoves = new List<Move>();
            Clip = new EllipseGeometry(
                new Point(CellSize / 2, CellSize / 2),
                CellSize / 2 - CellPadding,
                CellSize / 2 - CellPadding);
            ID = Guid.NewGuid();

            setType(ty);
            glyph.FontSize = CellSize / 4 * 3;
            newGlyph.FontSize = CellSize / 4 * 3;
            glyph.Text = symbol;

            Foreground = (player.isFirst() ? Brushes.White : Brushes.Black);
            circleOn.Fill = (player.isFirst() ? Brushes.Black : Brushes.White);
            shadow.Color = (player.isFirst() ? Colors.Black : Colors.White);
            newShadow.Color = (player.isFirst() ? Colors.Black : Colors.White);

            MouseEnter += mEnter;
            MouseLeave += mLeave;
            MouseDown += mPress;
            MouseUp += mRelease;
            MouseMove += mMove;
        }
        public Piece(Board b, Slab s) : this(b, s.loc.relX, s.loc.relY, s.player, s.type) { }
        public void setType(SlabType type) {
            this.type = type;
            switch (type) {
                case SlabType.Pawn:
                    symbol = "\u265F";
                    break;
                case SlabType.Rook:
                    symbol = "\u265C";
                    break;
                case SlabType.Knight:
                    symbol = "\u265E";
                    break;
                case SlabType.Bishop:
                    symbol = "\u265D";
                    break;
                case SlabType.Queen:
                    symbol = "\u265B";
                    break;
                case SlabType.King:
                    symbol = "\u265A";
                    break;
                case SlabType.Null:
                    symbol = "";
                    break;
            }
        }
        public void setRot(double angle) {
            baseAngle = angle;
            Rotation.Angle = angle;
        }

        public void translate(Coord looc) {
            loc = new Coord(looc.absX, looc.absY, player);
            if (!B.pieces.isVirtualMode()) {
                mBack = new Position(loc.absX * CellSize, loc.absY * CellSize);
                if (isReal())
                    beginLocation();
                else
                    Margin = mBack;
            }
        }
        public void delete() {
            if (!B.pieces.isVirtualMode()) {
                isEaten = true;
                eating = ID;
                if (isReal()) {
                    beginEaten();
                    B.graveyard.Add(this);
                } else
                    _delete(null, null);
            }
        }
        public void undelete() {
            if (!B.pieces.isVirtualMode()) {
                isEaten = false;
                B.graveyard.Remove(this);
                B[loc] = this;
                if (eating == ID)
                    eating = Guid.Empty;
                if (isReal()) {
                    stopEaten();
                    Opacity = 1;
                    circleOn.Opacity = 0;
                    pieceScale.ScaleX = 1;
                    pieceScale.ScaleY = 1;
                    if (B.graveyard.Contains(this))
                        B.graveyard.Remove(this);
                }
            }
        }
        void _delete(object sender, EventArgs e) {
            if (sender != null)
                stopEaten();
            B.remove(this);
            if (eating == ID)
                eating = Guid.Empty;
        }
        public void promote(SlabType type) {
            setType(type);
            newGlyph.Text = symbol;
            newGlyph.Visibility = Visibility.Visible;
            beginPromote();
        }
        void _promote(object sender, EventArgs e) {
            this.stopAnimation("Promote");
            glyph.Text = symbol;
            glyphRotation.Angle = 0;
            newRotation.Angle = 0;
            newGlyph.Visibility = Visibility.Collapsed;
            glyph.Visibility = Visibility.Visible;
            glyph.Opacity = 1;
        }
        public void cleanLocs() {
            int[] keys = new int[locs.Keys.Count];
            locs.Keys.CopyTo(keys, 0);
            foreach (var k in keys)
                if (k > 0 && k > B.pieces.realityLevel)
                    locs.Remove(k);
        }
        public bool isReal() {
            return type != SlabType.Null;
        }

        public List<Move> moves(bool dontSave = false, bool dontCheck = false) {
            List<Move> ret = new List<Move>(0);

            if (!cLastMoves || dontSave) {
                dontCheck |= B.pieces.isVirtualMode();
                List<Tuple<bool, Move>> muves = new List<Tuple<bool, Move>>(0);
                Coord sl1, sl2, sl3, sl4,
                      sr1, sr2, sr3, sr4;
                bool noCheck = dontSave || dontCheck || debugMoves;

                switch (type) {
                    case SlabType.Pawn:
                        sr1 = loc + (rX)1;
                        sl1 = loc - (rX)1;

                        muves.Add(mkMove(0, 1, this, MoveType.OnlyMove | MoveType.Promotion, noCheck));
                        muves.Add(mkMove(-1, 1, this, MoveType.OnlyAttack | MoveType.Promotion, noCheck));
                        muves.Add(mkMove(1, 1, this, MoveType.OnlyAttack | MoveType.Promotion, noCheck));
                        muves.Add(mkMove(0, 2, this, MoveType.PawnFirstMove, noCheck));
                        muves.Add(mkMove(-1, 1, this, MoveType.Passant, noCheck, sl1));
                        muves.Add(mkMove(1, 1, this, MoveType.Passant, noCheck, sr1));
                        break;
                    case SlabType.Knight:
                        muves.Add(mkMove(1, 2, this, MoveType.Normal, noCheck));
                        muves.Add(mkMove(2, 1, this, MoveType.Normal, noCheck));
                        muves.Add(mkMove(2, -1, this, MoveType.Normal, noCheck));
                        muves.Add(mkMove(1, -2, this, MoveType.Normal, noCheck));
                        muves.Add(mkMove(-1, -2, this, MoveType.Normal, noCheck));
                        muves.Add(mkMove(-2, -1, this, MoveType.Normal, noCheck));
                        muves.Add(mkMove(-2, 1, this, MoveType.Normal, noCheck));
                        muves.Add(mkMove(-1, 2, this, MoveType.Normal, noCheck));
                        break;
                    case SlabType.Rook:
                        muves.AddRange(mkSeries(SerialType.Vertical, this, noCheck).ConvertAll(m => new Tuple<bool, Move>(true, m)));
                        muves.AddRange(mkSeries(SerialType.Horizontal, this, noCheck).ConvertAll(m => new Tuple<bool, Move>(true, m)));
                        break;
                    case SlabType.Bishop:
                        muves.AddRange(mkSeries(SerialType.DiagonalUp, this, noCheck).ConvertAll(m => new Tuple<bool, Move>(true, m)));
                        muves.AddRange(mkSeries(SerialType.DiagonalDown, this, noCheck).ConvertAll(m => new Tuple<bool, Move>(true, m)));
                        break;
                    case SlabType.Queen:
                        muves.AddRange(mkSeries(SerialType.Vertical, this, noCheck).ConvertAll(m => new Tuple<bool, Move>(true, m)));
                        muves.AddRange(mkSeries(SerialType.Horizontal, this, noCheck).ConvertAll(m => new Tuple<bool, Move>(true, m)));
                        muves.AddRange(mkSeries(SerialType.DiagonalUp, this, noCheck).ConvertAll(m => new Tuple<bool, Move>(true, m)));
                        muves.AddRange(mkSeries(SerialType.DiagonalDown, this, noCheck).ConvertAll(m => new Tuple<bool, Move>(true, m)));
                        break;
                    case SlabType.King:
                        sr1 = loc + (rX)1;  //Valori assoluti, NON relativi
                        sr2 = loc + (rX)2;
                        sr3 = loc + (rX)3;
                        sr4 = loc + (rX)4;
                        sl1 = loc - (rX)1;
                        sl2 = loc - (rX)2;
                        sl3 = loc - (rX)3;
                        sl4 = loc - (rX)4;
                        
                        muves.Add(mkMove(0, 1, this, MoveType.Normal, noCheck));
                        muves.Add(mkMove(1, 1, this, MoveType.Normal, noCheck));
                        muves.Add(mkMove(1, 0, this, MoveType.Normal, noCheck));
                        muves.Add(mkMove(1, -1, this, MoveType.Normal, noCheck));
                        muves.Add(mkMove(0, -1, this, MoveType.Normal, noCheck));
                        muves.Add(mkMove(-1, -1, this, MoveType.Normal, noCheck));
                        muves.Add(mkMove(-1, 0, this, MoveType.Normal, noCheck));
                        muves.Add(mkMove(-1, 1, this, MoveType.Normal, noCheck));
                        if (!dontSave) {
                            muves.Add(mkMove(-2, 0, this, MoveType.Castling, noCheck, sl1, sl2, sl3));
                            muves.Add(mkMove(-2, 0, this, MoveType.Castling, noCheck, sl1, sl2, sl3, sl4));
                            muves.Add(mkMove(2, 0, this, MoveType.Castling, noCheck, sr1, sr2, sr3));
                            muves.Add(mkMove(2, 0, this, MoveType.Castling, noCheck, sr1, sr2, sr3, sr4));
                        }
                        break;
                }
                muves.Reverse();
                foreach (var tuple in muves) {
                    if (tuple.Item1) {
                        if (!B.pieces.isVirtualMode() && B.gKingInCheck(player) && type != SlabType.King) {
                            if (tuple.Item2.canSaveKing())
                                ret.Add(tuple.Item2);
                        } else
                            ret.Add(tuple.Item2);
                    }
                }
                if (!dontSave) {
                    lastMoves.AddRange(ret);
                    cLastMoves = true;
                }
            } else
                ret.AddRange(lastMoves);

            return ret;
        }

        public void mEnter(object sender, EventArgs e) {
            bool mT = myTurn && (gamemode == Gamemodes.Remote ? player == match.gameSet.myTurn : true);
            if (isReal() && !mHold && !isEaten && B.frameTop == 0) {
                //if (canSeeEnemyMoves)
                isHover = true;
            }
        }
        public void mLeave(object sender, EventArgs e) {
            if (isReal() && !mHold && !isEaten) {
                isHover = false;
            }
        }
        public void mPress(object sender, MouseEventArgs e) {
            if (isHover && isReal() && !B.pieces.isVirtualMode()) {
                if (e.LeftButton == MouseButtonState.Pressed && !isEaten) {
                    mHold = true;
                    mOld = e.GetPosition(this);
                    stopLocation(sender, null);
                    CaptureMouse();
                    Panel.SetZIndex(B.top, 0);
                    Panel.SetZIndex(this, 1);
                    writeLine(string.Format("## Clicked: {0}", ToString()));
                    B.top = this;
                    B.highlightMoves(this, moves(), true);
                    B.refreshTraits(this);
                    isPress = true;
                } else if (deletePieces && e.RightButton == MouseButtonState.Pressed && type != SlabType.King && eating != ID) {
                    ReleaseMouseCapture();
                    B.addChrono(new Move(0, 0, this, MoveType.Normal) { hasEaten = true });
                    writeLine(string.Format("## Deleted: {0}", ToString()));
                    B.vanish(this);
                    B.nextTurn();
                }
            }
        }
        public void mRelease(object sender, MouseEventArgs e) {
            if (isReal() && mHold && !isEaten && !B.pieces.isVirtualMode()) {
                mHold = false;
                ReleaseMouseCapture();
                Trait t = B.findTarget(this);
                var oldMoves = moves();
                beginLocation();
                if (myTurn && t != null) {
                    t.isMove = false;
                    t.isTarget = false;
                    if (noTurns || myTurn) {
                        writeLine(string.Format("## Moved: {0}", ToString()));
                        Move m = oldMoves.Find(x => x.loc == t.loc);
                        m.perform();
                        if (!m.type.HasFlag(MoveType.Promotion))
                            B.nextTurn();
                    }
                }
                B.highlightMoves(this, oldMoves, false);
                isPress = false;
            }
        }
        public void mMove(object sender, MouseEventArgs e) {
            if (myTurn && mHold && e.LeftButton == MouseButtonState.Pressed && !isEaten) {
                mNew = e.GetPosition(this);
                Position min = (mNew - mOld);
                min.y = -min.y;
                Position sum = Margin;
                if (baseAngle != 0)
                    min.rotateClockwise(baseAngle, 0, 0);
                sum += min;
                sum.x = Math.Max(0 - CellPadding, sum.x);
                sum.x = Math.Min(realW - CellSize + CellPadding, sum.x);
                sum.y = Math.Max(0 - CellPadding, sum.y);
                sum.y = Math.Min(realW - CellSize + CellPadding, sum.y);
                Margin = sum;
                B.highlightTarget(this);
            }
        }

        public void beginPromote() {
            var sb = (Storyboard)this.FindResource("Promote");
            var rot1 = (DoubleAnimation)sb.Children[0];
            var rot2 = (DoubleAnimation)sb.Children[1];
            rot1.From = 0;
            rot1.To = 1080;
            rot2.From = 0;
            rot2.To = 1080;
            sb.Begin();
        }
        public void beginEaten() {
            Panel.SetZIndex(this, 2);
            var sb = (Storyboard)this.FindResource("Eat");
            sb.Begin();
        }
        public void beginLocation() {
            var sb = (Storyboard)this.FindResource("Location");
            var ta = (ThicknessAnimation)sb.Children[0];
            sb.Stop();
            ta.To = mBack;
            sb.Begin();
        }
        public void inDangerOn() {
            circleDanger.Visibility = Visibility.Visible;
            this.beginAnimation("InDanger");
        }
        public void stopEaten() {
            Panel.SetZIndex(this, 0);
            var sb = (Storyboard)this.FindResource("Eat");
            sb.Stop();
        }
        public void stopLocation(object sender, EventArgs e) {
            var sb = (Storyboard)this.FindResource("Location");
            var ta = (ThicknessAnimation)sb.Children[0];
            Margin = mBack;
            sb.Stop();
        }
        public void refreshStates() {
            firstMove = unTouched;
            unTouched = false;
            justTouched = true;
        }
        void dangerloop(object sender, EventArgs e) {
            if (_isInDanger)
                this.beginAnimation("InDanger");
            else
                circleDanger.Visibility = Visibility.Collapsed;
        }

        public static implicit operator Trait(Piece p) {
            return p.B.traits[p.loc];
        }
        public override string ToString() {
            return string.Format("{0} ({1})", type.ToString(), loc.ToString());
        }
    }
}