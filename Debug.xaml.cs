using System;
using System.Windows.Controls;
using static ChessWPF.Core;

namespace ChessWPF {
    public partial class Debug {
        public Debug() {
            InitializeComponent();
            loadSettings();

            cNoTurns.Click += changeSettings;
            cDelPieces.Click += changeSettings;
            cDMoves.Click += changeSettings;
            cDSeries.Click += changeSettings;
            cDSpecial.Click += changeSettings;
        }

        void loadSettings() {
            cNoTurns.IsChecked = noTurns;
            cDelPieces.IsChecked = deletePieces;
            cDMoves.IsChecked = debugMoves;
            cDSeries.IsChecked = debugSeries;
            cDSpecial.IsChecked = debugSpecial;
        }
        void changeSettings(object sender, EventArgs e) {
            var chkbox = sender as CheckBox;
            bool state = chkbox.IsChecked ?? false;
            writeLine(string.Format("{0}: {1}", chkbox.Content, (state ? "On" : "Off")));

            if (sender == cNoTurns)
                noTurns = state;
            else if (sender == cDelPieces)
                deletePieces = state;
            else if (sender == cDMoves)
                debugMoves = state;
            else if (sender == cDSeries)
                debugSeries = state;
            else if (sender == cDSpecial)
                debugSpecial = state;

            foreach (var p in sBoard.pieces) {
                p.lastMoves.Clear();
                p.cLastMoves = false;
            }
        }

        public void updateInfo(int turn, int history, int actions) {
            txtT.Text = turn.isFirst() ? "White" : "Black";
            txtH.Text = history + "";
            txtA.Text = actions + "";
        }
    }
}
