using System.Windows;
using static ChessWPF.Core;

namespace ChessWPF {
    public partial class PlayFrame {
        public PlayFrame() {
            InitializeComponent();
        }

        public void prepare() {
            if (gamemode == Gamemodes.Replay) {
                PlayMenu.Visibility = Visibility.Collapsed;
                ReplayMenu.Visibility = Visibility.Visible;
            } else {
                PlayMenu.prepare();
                PlayMenu.Visibility = Visibility.Visible;
                ReplayMenu.Visibility = Visibility.Collapsed;
            }
        }
    }
}
