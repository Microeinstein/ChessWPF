using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ChessWPF {
    public partial class Menu {
        public Menu() {
            InitializeComponent();
            Opacity = 1;
            Visibility = Visibility.Visible;
            Background = Brushes.White;

            _pt1.Point = new Point(Width, 0);
            _pt2.Point = new Point(Width, Height);
            _pt3.Point = new Point(Width, Height);
            _pt4.Point = new Point(0, Height);
            Storyboard puf = (Storyboard)FindResource("Puffy");
            PointAnimation pA1 = (PointAnimation)puf.Children[0],
                           pA2 = (PointAnimation)puf.Children[1];
            pA1.To = new Point(Width, 0);
            pA2.To = new Point(0, Height);
        }
    }
}
