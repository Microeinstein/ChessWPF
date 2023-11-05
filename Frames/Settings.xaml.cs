using System;
using static ChessWPF.Core;

namespace ChessWPF {
    public partial class Settings {
        bool loaded;

        public Settings() {
            InitializeComponent();

            loaded = false;
            sFPS.ValueChanged += save;
            txtNick.TextChanged += save;
            chkLogs.Checked += save;
            chkLogs.Unchecked += save;
            chkReplays.Checked += save;
            chkReplays.Unchecked += save;
        }

        public void load() {
            sFPS.Value = FPS;
            txtNick.Text = Nickname;
            chkLogs.IsChecked = Logs;
            chkReplays.IsChecked = Replays;
            loaded = true;
        }
        public void save(object sender, EventArgs e) {
            if (loaded) {
                setFPS((int)sFPS.Value);
                if (txtNick.Text != "") {
                    if (txtNick.Text == "Microeinstein" && (Environment.MachineName != "MICRO-PC" || !Environment.UserName.StartsWith("Micro")))
                        txtNick.Text = "Nope ♥";
                    Nickname = txtNick.Text;
                }
                Logs = chkLogs.IsChecked ?? false;
                Replays = chkReplays.IsChecked ?? false;
            }
        }
    }
}
