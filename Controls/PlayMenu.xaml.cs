using MaterialDesignThemes.Wpf;
using System;
using System.Windows;
using System.Windows.Media;
using static ChessWPF.Core;

namespace ChessWPF {
    public partial class PlayMenu {
        public PlayMenu() {
            InitializeComponent();
            Width = realW;
            
            bUndo.Click += delegate {
                if (gamemode == Gamemodes.Local)
                    sBoard.undoLast();
                else if (gamemode == Gamemodes.Remote)
                    net.queueRequestP2(AskType.Undo);
            };
            bRedo.Click += delegate {
                if (gamemode == Gamemodes.Local)
                    sBoard.redoLast();
                else if (gamemode == Gamemodes.Remote)
                    net.queueRequestP2(AskType.Redo);
            };
            bDraw.Click += delegate {
                if (gamemode == Gamemodes.Remote)
                    net.queueRequestP2(AskType.Draw);
            };
            bGiveUp.Click += delegate {
                if (gamemode == Gamemodes.Remote && !match.gameSet.canPlay)
                    net.returnToLobby();
                else
                    sBoard.askGiveUp();
            };
        }

        public void prepare() {
            if (gamemode == Gamemodes.Local) {
                bUndo.Visibility = Visibility.Visible;
                bRedo.Visibility = Visibility.Visible;
                bDraw.IsEnabled = false;
                bGiveUp.ToolTip = "Give Up";
                imgExit.Kind = PackIconKind.Flag;
            } else if (gamemode == Gamemodes.Remote) {
                var cp = match.gameSet.canPlay;
                bUndo.Visibility = cp ? Visibility.Visible : Visibility.Hidden;
                bRedo.Visibility = cp ? Visibility.Visible : Visibility.Hidden;
                bUndo.IsEnabled = cp && match.gameSet.undo;
                bRedo.IsEnabled = cp && match.gameSet.redo;
                bDraw.IsEnabled = cp && match.gameSet.draw;
                bGiveUp.ToolTip = cp ? "Give Up" : "Leave match";
                imgExit.Kind = cp ? PackIconKind.Flag : PackIconKind.ExitToApp;
            }
        }
        public void updateInfo(int turn, int history, int actions, TimeSpan tW, TimeSpan tB) {
            timeW.Text = string.Format("{0}:{1}:{2}.{3}",
                tW.Hours.ToString().PadLeft(2, '0'),
                tW.Minutes.ToString().PadLeft(2, '0'),
                tW.Seconds.ToString().PadLeft(2, '0'),
                tW.Milliseconds.ToString().PadLeft(3, '0'));
            timeB.Text = string.Format("{0}:{1}:{2}.{3}",
                tB.Hours.ToString().PadLeft(2, '0'),
                tB.Minutes.ToString().PadLeft(2, '0'),
                tB.Seconds.ToString().PadLeft(2, '0'),
                tB.Milliseconds.ToString().PadLeft(3, '0'));
            bUndo.IsEnabled = (history > 0 && actions > 0 && inGame) && (gamemode == Gamemodes.Remote ? match.gameSet.undo : true);
            bRedo.IsEnabled = (history > 0 && actions < history && inGame) && (gamemode == Gamemodes.Remote ? match.gameSet.redo : true);
            if (inGame) {
                if (gamemode != Gamemodes.Remote)
                    txtT.Text = turn.isFirst() ? "White" : "Black";
                else
                    txtT.Text = turn.isFirst() ? match.gameSet.white.nickname : match.gameSet.black.nickname;
                timeW.Foreground = turn.isFirst() ? Brushes.Aqua : Brushes.White;
                timeB.Foreground = turn.isFirst() ? Brushes.White : Brushes.Aqua;
            }
            bGiveUp.IsEnabled = inGame;
        }
    }
}
