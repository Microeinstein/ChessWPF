namespace ChessWPF {
    public class ChessBoard : Board {
        public ChessBoard() : base() {
            setup();
        }

        public void setup() {
            for (int p = 0; p < 2; p++) {
                for (int pe = 0; pe < 8; pe++)
                    insert(new Piece(this, pe, 1, p, SlabType.Pawn));
                insert(new Piece(this, 0, 0, p, SlabType.Rook));
                insert(new Piece(this, 1, 0, p, SlabType.Knight));
                insert(new Piece(this, 2, 0, p, SlabType.Bishop));
                insert(new Piece(this, p.isFirst() ? 3 : 4, 0, p, SlabType.Queen));
                insert(new Piece(this, p.isFirst() ? 4 : 3, 0, p, SlabType.King));
                insert(new Piece(this, 5, 0, p, SlabType.Bishop));
                insert(new Piece(this, 6, 0, p, SlabType.Knight));
                insert(new Piece(this, 7, 0, p, SlabType.Rook));
            }
        }
    }
}
