using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Micro.NetLib;
using static ChessWPF.Core;

namespace ChessWPF {
    public partial class Win {
        List<Storyboard> waves = new List<Storyboard>();

        public Win() {
            InitializeComponent();
            //sDisappear.Children[0].Duration = TimeSpan.Zero;
            
            //endDisappear += Hide;
        }

        public void Show(WinType mode, User subject) {
            Hide();

            string tex, pWhite, pBlack, pSubject = "Someone", pGU = "gave up";
            if (gamemode == Gamemodes.Remote) {
                pWhite = match.gameSet.white.nickname;
                pBlack = match.gameSet.black.nickname;
                if (match.gameSet.canPlay) {
                    pSubject = net.connect.myself.nickname;
                    pGU = "wins";
                } else if (subject != null) {
                    pSubject = subject.nickname;
                }
            } else {
                pWhite = "White";
                pBlack = "Black";
            }

            switch (mode) {
                case WinType.White:      tex = pWhite + " wins";           break;
                case WinType.Black:      tex = pBlack + " wins";           break;
                case WinType.Stale:      tex = "Stalemate";                break;
                case WinType.Draw:       tex = "Draw";                     break;
                case WinType.GiveUp:     tex = pSubject + " " + pGU;       break;
                case WinType.Disconnect: tex = pSubject + " disconnected"; break;
                default:                 tex = "This can't happen";        break;
            }
            text.Text = tex;

            //sAppear.Completed += _start;
            _start(null, null);
            appear();
        }
        public void Hide() {
            Visibility = Visibility.Collapsed;
            Opacity = 0;
            foreach (var sb in waves)
                sb.Stop();
            text.TextEffects.Clear();
            waves.Clear();
        }
        void _start(object sender, EventArgs e) {
            text.TextEffects = new TextEffectCollection();

            for (int i = 0; i < text.Text.Length; i++) {
                var wave = ((Storyboard)FindResource("Wave")).Clone();
                AddTextEffectForCharacter(i);
                AddWaveAnimation(wave, i);
                AddPause(wave, i);
                SetBeginTime(wave, i);
                waves.Add(wave);
            }
            foreach (var sb in waves)
                sb.Begin(this);
        }

        void AddTextEffectForCharacter(int charIndex) {
            TextEffect effect = new TextEffect();
            
            effect.PositionStart = charIndex;
            effect.PositionCount = 1;
            
            effect.Transform = new TranslateTransform();
            text.TextEffects.Add(effect);
        }
        void AddWaveAnimation(Storyboard sb, int charIndex) {
            DoubleAnimation anim = ((DoubleAnimation)FindResource("CharWave")).Clone();
            string path = string.Format("TextEffects[{0}].Transform.Y", charIndex);
            Storyboard.SetTargetProperty(anim, new PropertyPath(path));

            sb.Children.Add(anim);
        }
        void AddPause(Storyboard sb, int charIndex) {
            DoubleAnimation pause = ((DoubleAnimation)FindResource("Pause")).Clone();
            pause.Duration = TimeSpan.FromSeconds(2.5);
            sb.Children.Add(pause);
        }
        void SetBeginTime(Timeline anim, int charIndex) {
            double totalMs = 750;
            double offset = totalMs / 10;
            double resolvedOffset = offset * charIndex;
            anim.BeginTime = TimeSpan.FromMilliseconds(resolvedOffset);
        }
        
        //void test(object sender, EventArgs e) {
        //    Console.WriteLine("Opacity: " + Opacity);
        //    Console.WriteLine("Visibility: " + Visibility);
        //    Console.WriteLine("Margins: " + Margin);
        //}
    }
}