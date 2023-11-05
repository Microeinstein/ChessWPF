using System.Windows.Controls;
using static ChessWPF.Core;

namespace ChessWPF {
    public partial class ReplayMenu : UserControl {
        public ReplayMenu() {
            InitializeComponent();

            bPrev.Click += delegate { sBoard.replayPrev(); };
            bNext.Click += delegate { sBoard.replayNext(); };
        }

        public void update(int history, int actions) {
            bPrev.IsEnabled = (history > 0 && actions > 0);
            bNext.IsEnabled = (history > 0 && actions <= history);
        }
    }
}
