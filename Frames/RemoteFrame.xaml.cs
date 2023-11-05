using Micro;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Micro.NetLib;
using static ChessWPF.Core;

namespace ChessWPF {
    public partial class RemoteFrame {
        SolidColorBrush playingColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e91e63"));
        bool empty = true;

        public RemoteFrame() {
            InitializeComponent();
            txtChat.KeyDown += sendChat;
        }
        
        public void clearListes() {
            rwlPlayers.Items.Clear();
            rwlPlaying.Items.Clear();
        }
        public void addUser(User u, bool playing) {
            var ui = new ListBoxItem() { Content = u.nickname, Tag = u };
            if (u == net.connect.serverUser)
                ui.FontWeight = FontWeights.Bold;
            if (playing)
                ui.Foreground = playingColor;
            if (u != net.connect.myself) {
                ui.MouseLeftButtonUp += (a, b) => net.queueRequest(u, AskType.Play);
                ui.MouseRightButtonUp += (a, b) => sBase.kickShow(u);
            } else
                ui.IsEnabled = false;
            rwlPlayers.Items.Add(ui);
        }
        public void addMatch(GamePlay m) {
            var ui = new ListBoxItem() { Content = string.Format("{0} ~ {1}", m.gameSet.white.nickname, m.gameSet.black.nickname), Tag = m };
            ui.MouseUp += (a, b) => selectMatch((GamePlay)ui.Tag);
            rwlPlaying.Items.Add(ui);
        }
        void selectMatch(GamePlay m) {
            net.spectate(m);
        }
        void sendChat(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                net.sendText(txtChat.Text);
                txtChat.Text = "";
                e.Handled = true;
            }
        }

        public void clearChat() {
            empty = true;
            textLog.Inlines.Clear();
        }
        public void writeChat(string text) {
            bool scrollDown = scrollLog.VerticalOffset == scrollLog.ScrollableHeight;
            if (!empty) {
                textLog.Inlines.Add(new LineBreak());
            } else
                empty = false;
            textLog.Inlines.Add(new Run(string.Format("{0} ~ ", getTime())) { Foreground = rainbowColor() });
            textLog.Inlines.Add(new Run(string.Format("{0}", text)) { Foreground = Brushes.Black });
            if (textLog.Inlines.Count > 300 * 3) {
                for (int i = 0; i < 3; i++) {
                    textLog.Inlines.Remove(textLog.Inlines.FirstInline);
                }
            }
            if (scrollDown)
                scrollLog.ScrollToEnd();
        }
        string getTime() {
            var now = DateTime.Now;
            string h = now.Hour + "",
                   m = now.Minute + "",
                   s = now.Second + "";
            h = h.Length < 2 ? "0" + h : h;
            m = m.Length < 2 ? "0" + m : m;
            s = s.Length < 2 ? "0" + s : s;
            return string.Format("{0}:{1}:{2}", h, m, s);
        }
    }
}
