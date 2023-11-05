using static ChessWPF.Core;

namespace ChessWPF {
    public partial class Promotion {

        public Promotion() {
            InitializeComponent();

            Queen.setType(SlabType.Queen);
            Rook.setType(SlabType.Rook);
            Bishop.setType(SlabType.Bishop);
            Knight.setType(SlabType.Knight);

            Queen.Clicked += mClicked;
            Rook.Clicked += mClicked;
            Bishop.Clicked += mClicked;
            Knight.Clicked += mClicked;
        }

        public void mClicked(FakePiece p) {
            disappear();
            sBoard.promote(p.type);
        }
    }
}
