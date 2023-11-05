#pragma warning disable CS4014
using System.Diagnostics;
using System.Windows.Navigation;

namespace ChessWPF {
    public partial class Info {
        public Info() {
            InitializeComponent();
        }

        void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
