#pragma warning disable CS4014
using System.Windows.Media.Animation;
using static ChessWPF.Core;

namespace ChessWPF {
    public partial class Back {
        Storyboard sAppear, sDisappear;
        public bool isShown { get; private set; } = false;

        public Back() {
            InitializeComponent();
            Click += (a, b) => goBack();
            sAppear = (Storyboard)FindResource("sAppear");
            sDisappear = (Storyboard)FindResource("sDisappear");
        }
        
        public void check() {
            if (fShown.canGoBack())
                appear();
            else
                disappear();
        }
        public void goBack() {
            switch (fShown.Kind) {
                case FrameType.Info:
                case FrameType.Settings:
                    Frame.goTo(sBase.Menu);
                    break;
                case FrameType.RemoteFrame:
                    net.disconnect();
                    break;
                case FrameType.PlayFrame:
                case FrameType.Win:
                    if (GState != GameState.Menu)
                        sBase.endGame();
                    if (gamemode == Gamemodes.Remote)
                        net.returnToLobby();
                    else
                        Frame.goTo(sBase.Menu);
                    break;
            }
        }
        public void appear() {
            if (!isShown) {
                sAppear.Begin();
                isShown = true;
            }
        }
        public void disappear() {
            if (isShown) {
                sDisappear.Begin();
                isShown = false;
            }
        }

        //void goBack() {
        //    if (!inTransition) {
        //        if (FState == FrameType.Info)
        //            sBase.Info.switchTo(sBase.Menu);
        //        if (FState == FrameType.Settings) {
        //            sBase.Settings.switchTo(sBase.Menu);
        //        } else if (FState == FrameType.Win ||
        //                  (FState == FrameType.PlayFrame && gamemode == Gamemodes.Replay))
        //            sBase.endGame();
        //    }
        //}
    }
}
