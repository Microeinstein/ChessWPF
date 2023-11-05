using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static ChessWPF.Settings;

namespace ChessWPF {
    public partial class Menu {
        public delegate void LocalGame(int player);
        public event LocalGame StartLocal;
        bool localShown = false;

        public Menu() {
            InitializeComponent();

            bLocal.Click += localGamePlay;
            bLocalWhite.Click += (a, b) => { localShown = false; StartLocal.Invoke(0); };
            bLocalBlack.Click += (a, b) => { localShown = false; StartLocal.Invoke(1); };
        }

        void localGamePlay(object sender, EventArgs e) {
            localShown = !localShown;
            if (localShown)
                this.beginAnimation("localShow");
            else
                this.beginAnimation("localLeave");
        }
    }
}
