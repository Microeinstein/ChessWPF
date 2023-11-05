#pragma warning disable CS0660
#pragma warning disable CS0661
#pragma warning disable CS4014
using System;
using System.Collections.Generic;
using System.Linq;
using static ChessWPF.Coord;
using static ChessWPF.Core;

namespace ChessWPF {
    public struct Move {
        public bool hasEaten;
        bool[] states;
        bool unfixed, serializing;
        public readonly int dx, dy, px, py, sdx, sdy;
        Guid ID;
        MoveType _type;
        public SlabType promoted;
        public MoveType type { get { return _type; } }
        Coord _loc, _sloc;
        public Coord oldLoc {
            get { return new Coord(px, py); }
        }
        public Coord loc {
            get { return unfixed ? new Coord(px, py) + (cP)p.loc.player + (rX)dx + (rY)dy : _loc; }
        }
        public Coord sloc {
            get { return unfixed ? new Coord(px, py) + (cP)p.loc.player + (rX)sdx + (rY)sdy : _sloc; }
        }
        Piece _p, _sp;
        public Piece p {
            get {
                Piece pB = sBoard[px, py];
                if (!serializing)
                    _p = _p ?? pB;
                if (gamemode == Gamemodes.Replay) {
                    tempID = ID;
                    var i = replay.Item2.FindIndex(a => a.ID == tempID);
                    if (i > -1)
                        replay.Item2[i] = this;
                }
                if (!serializing) return _p;
                else return pB;
            }
            set { _p = value; }
        }
        public Piece sp {
            get {
                _sp = _sp ?? sBoard[sloc];
                if (gamemode == Gamemodes.Replay) {
                    tempID = ID;
                    var i = replay.Item2.FindIndex(a => a.ID == tempID);
                    if (i > -1)
                        replay.Item2[i] = this;
                }
                return _sp;
            }
            set { _sp = value; }
        }

        /// <summary>
        /// Costruttore della mossa
        /// </summary>
        /// <param name="dX">Di quanto si deve spostare relativamente la pedina nell'asse X</param>
        /// <param name="dY">Di quanto si deve spostare relativamente la pedina nell'asse Y</param>
        /// <param name="piece">Pedina da spostare</param>
        /// <param name="moveType">Tipo di mossa da eseguire</param>
        /// <param name="sdX">Spostamento relativo X aggiuntivo per le mosse speciali</param>
        /// <param name="sdY">Spostamento relativo Y aggiuntivo per le mosse speciali</param>
        public Move(int dX, int dY, Piece piece, MoveType moveType = 0, int sdX = 0, int sdY = 0) {
            dx = dX;
            dy = dY;
            sdx = sdX;
            sdy = sdY;
            px = piece.loc.absX;
            py = piece.loc.absY;
            _p = piece;
            _loc = _p.loc + (rX)dx + (rY)dy;
            _sloc = _p.loc + (rX)sdx + (rY)sdy;
            _sp = piece.B[_sloc];
            unfixed = false;
            serializing = false;
            hasEaten = false;
            states = new bool[3] {false, false, false};
            promoted = SlabType.Null;
            _type = moveType;
            ID = Guid.NewGuid();
        }
        public Move(int dX, int dY, int pX, int pY, MoveType moveType = 0, int sdX = 0, int sdY = 0) {
            dx = dX;
            dy = dY;
            sdx = sdX;
            sdy = sdY;
            px = pX;
            py = pY;
            _p = null;
            _sp = null;
            unfixed = true;
            serializing = false;
            hasEaten = false;
            states = new bool[3] { false, false, false };
            _loc = new Coord();
            _sloc = new Coord();
            promoted = SlabType.Null;
            _type = moveType;
            ID = Guid.NewGuid();
        }
        public static Move Null() {
            return new Move() { _type = MoveType.Null };
        }
        public static Tuple<bool, Move> mkMove(int deltaX, int deltaY, Piece piece, MoveType moveType, bool dontCheck = false, params Coord[] sDelta) {
            var exit = new Tuple<bool, Move>(false, Null());
            bool canMake = true,
                 canSpecial = true,
                 noSpecial = sDelta.Length < 1;
            var B = piece.B;
            MoveType mt = moveType & ~MoveType.Promotion;
            Move theMove;
            Coord n = piece.loc + (rX)deltaX + (rY)deltaY,
                  s0 = noSpecial ? n : sDelta[0],
                  sd = default(Coord);

            if (!B.cIn(n))
                return exit;
            
            bool eMpty = B.cEmpty(n),
                 eNemy = B.cEnemy(piece, n);

            if ((moveType.HasFlag(MoveType.OnlyMove) && (eMpty && !eNemy))
             || (moveType.HasFlag(MoveType.OnlyAttack) && (!eMpty && eNemy))
             || (moveType.HasFlag(MoveType.Normal) && (eMpty || eNemy)) || debugMoves)
                canMake = true;
            else
                return exit;
            
            if (moveType.specialMove(true)) {
                if (B.cIn(s0)) {
                    Piece SPiece = B[s0];
                    if (moveType.HasFlag(MoveType.PawnFirstMove)) {
                        bool eMptyS = B.cEmpty(n - (rY)1);
                        canMake &= (piece.unTouched && eMptyS) || debugSpecial;
                    } else if (moveType.HasFlag(MoveType.Passant)) {
                        canMake &= SPiece.justTouched
                                    && SPiece.firstMove
                                    && SPiece.player != piece.player
                                    && SPiece.type == SlabType.Pawn
                                    || debugSpecial;
                        sd = new Coord(s0.relX - piece.loc.relX, 0);
                    } else if (moveType.HasFlag(MoveType.Promotion))
                        canSpecial &= (n.relY) == 7;
                    else if (moveType.HasFlag(MoveType.Castling)) {
                        bool L4 = sDelta.Length > 3;
                        Coord s1 = sDelta[1],
                              s2 = sDelta[2],
                              s3 = L4 ? sDelta[3] : default(Coord);

                        if (L4)
                            canMake = B.cIn(s0) && B.cIn(s1) && B.cIn(s2) && B.cIn(s3) &&
                                    ((B.cEmpty(s0) && B.cEmpty(s1) && B.cEmpty(s2) && B.cExist(s3)) || debugSpecial);
                        else
                            canMake = B.cIn(s0) && B.cIn(s1) && B.cIn(s2) &&
                                    ((B.cEmpty(s0) && B.cEmpty(s1) && B.cExist(s2)) || debugSpecial);

                        if (canMake) {
                            Piece Rook;
                            if (L4) {
                                Rook = B[s3];
                                canMake &= Rook.type == SlabType.Rook && Rook.unTouched && piece.unTouched;
                                if (!dontCheck)
                                    canMake &= !B.cEatable(piece, s0.relX)
                                            && !B.cEatable(piece, s1.relX)
                                            && !B.cEatable(piece, s2.relX)
                                            && !B.cEatable(piece, s3.relX);
                            } else {
                                Rook = B[s2];
                                canMake &= Rook.type == SlabType.Rook && Rook.unTouched && piece.unTouched;
                                if (!dontCheck)
                                    canMake &= !B.cEatable(piece, s0.relX)
                                            && !B.cEatable(piece, s1.relX)
                                            && !B.cEatable(piece, s2.relX);
                            }
                            sd = new Coord(Rook.loc.relX - piece.loc.relX, 0);
                        }
                    }
                } else
                    canMake = false;
            }

            theMove = new Move(deltaX, deltaY, piece, canSpecial ? moveType : mt,
                               noSpecial ? 0 : sd.absX,
                               noSpecial ? 0 : sd.absY);
            if (!dontCheck && canMake)
                canMake &= !B.cEatable(B.findKing(piece.player), theMove);

            return new Tuple<bool, Move>(canMake, theMove);
        }
        public static List<Move> mkSeries(SerialType mode, Piece piece, bool dontCheck = false) {
            Coord loc = piece.loc, l;
            int tx, ty, vx, vy;
            bool endLoop = false;
            var B = piece.B;
            List<Move> mtemp = new List<Move>(),
                       mv = new List<Move>();

            switch (mode) {
                case SerialType.Vertical:
                    vx = 0;
                    vy = 1;
                    break;
                case SerialType.Horizontal:
                    vx = 1;
                    vy = 0;
                    break;
                case SerialType.DiagonalUp:
                    vx = 1;
                    vy = 1;
                    break;
                case SerialType.DiagonalDown:
                    vx = -1;
                    vy = 1;
                    break;
                default:
                    vx = 0;
                    vy = 0;
                    break;
            }

            tx = 0;
            ty = 0;
            l = loc;
            while (!endLoop) {
                tx += vx;
                ty += vy;
                l = l + (rX)vx + (rY)vy;
                if (B.cIn(l)) {
                    if (B.cEmpty(l) || debugSeries)
                        mtemp.Add(new Move(tx, ty, piece, MoveType.Normal));
                    else {
                        if (B.cEnemy(piece, l))
                            mtemp.Add(new Move(tx, ty, piece, MoveType.Normal));
                        endLoop = true;
                    }
                } else
                    endLoop = true;
            }

            tx = 0;
            ty = 0;
            l = loc;
            endLoop = false;
            while (!endLoop) {
                tx -= vx;
                ty -= vy;
                l = l - (rX)vx - (rY)vy;
                if (B.cIn(l)) {
                    if (B.cEmpty(l) || debugSeries)
                        mtemp.Add(new Move(tx, ty, piece, MoveType.Normal));
                    else {
                        if (B.cEnemy(piece, l))
                            mtemp.Add(new Move(tx, ty, piece, MoveType.Normal));
                        endLoop = true;
                    }
                } else
                    endLoop = true;
            }

            if (dontCheck) {
                mv.AddRange(mtemp);
            } else {
                foreach (var m in mtemp) {
                    if (!B.cEatable(B.findKing(piece.player), m))
                        mv.Add(m);
                }
            }

            return mv;
        }
        public static Move Parse(string txt) {
            var spl = txt.Split(',').Select((a, b) => b = int.Parse(a)).ToArray();
            return new Move(spl[0], spl[1], spl[2], spl[3], (MoveType)spl[4], spl[5], spl[6]) { promoted = (SlabType)spl[7] };
        }

        public bool isNull() {
            return _type == MoveType.Null;
        }
        public bool isSpecial(bool making = false) {
            return type.specialMove(making);
        }
        public bool canSaveKing() {
            Piece k;
            if (p.type == SlabType.King) k = p;
            else k = p.B.findKing(p.player);
            bool ret = !p.B.cEatable(k, this);
            return ret;
        }

        public void perform(bool redo = false) {
            //writeLine(string.Format("## {0}: {1} ~ {2}, {3}, {4}", redo ? "Redo" : "Perform", ToString(), _type, promoted, ID));
            var B = p.B;
            var promote = type.HasFlag(MoveType.Promotion);

            if (!B.pieces.isVirtualMode()) {
                B.clearJustTouched();
                playSound();
                if (!promote && gamemode != Gamemodes.Replay) {
                    if (redo)
                        replayRedo();
                    else
                        replayMove(this);
                }
                saveStates();
            }

            if (isSpecial()) {
                if (type.HasFlag(MoveType.Passant)) {
                    hasEaten = true;
                    B.swap(p.loc, loc);
                    B.vanish(B[sp.loc]);
                } else if (promote) {
                    if (B[loc].isReal()) {
                        hasEaten = true;
                        B.eat(p, loc);
                    } else
                        B.swap(p.loc, loc);

                    if (!B.pieces.isVirtualMode()) {
                        if (!redo && gamemode != Gamemodes.Replay && promoted == SlabType.Null)
                            B.selectPromote(this);
                        else
                            p.promote(promoted);
                    } else
                        p.fakePromote = true;
                } else if (type.HasFlag(MoveType.Castling)) {
                    Coord rloc = dx > 0 ? loc - (rX)1 : loc + (rX)1;

                    B.swap(p.loc, loc);
                    B.swap(sp.loc, rloc);
                }
            } else {
                if (B[loc].isReal()) {
                    hasEaten = true;
                    B.eat(p, loc);
                } else
                    B.swap(p.loc, loc);
            }

            if (!B.pieces.isVirtualMode()) {
                if (!redo)
                    B.addChrono(this);
                p.refreshStates();
            }
        }
        public void undo() {
            //writeLine(string.Format("## Undo: {0} ~ {1}, {2}, {3}", ToString(), _type, promoted, ID));
            var B = p.B;
            var unloc = p.loc - (rX)dx - (rY)dy;
            var unlocs = unloc + (rX)sdx + (rY)sdy;

            if (!B.pieces.isVirtualMode()) {
                if (gamemode != Gamemodes.Replay)
                    replayUndo();
                playSound();
            }

            loadStates();
            if (type.HasFlag(MoveType.Castling)) {
                Coord rook = unlocs;
                Coord rloc = dx > 0 ? unloc + (rX)1 : unloc - (rX)1;
                
                B.swap(rloc, rook);
            }
            B.swap(p.loc, unloc);
            if (hasEaten)
                B.restoreLast();
            if (type.HasFlag(MoveType.Promotion))
                p.promote(SlabType.Pawn);
            p.isHover = p.IsMouseOver;
        }
        void saveStates() {
            states[0] = p.unTouched;
            states[1] = p.firstMove;
            states[2] = p.justTouched;
        }
        void loadStates() {
            p.unTouched = states[0];
            p.firstMove = states[1];
            p.justTouched = states[2];
        }

        public static bool operator ==(Move a, Move b) {
            return a.ID == b.ID;
        }
        public static bool operator !=(Move a, Move b) {
            return a.ID != b.ID;
        }
        public override string ToString() {
            serializing = true;
            string ret = string.Join(",", dx, dy, oldLoc.absX, oldLoc.absY, (int)_type, sdx, sdy, (int)promoted);
            serializing = false;
            return ret;
        }
    }
}