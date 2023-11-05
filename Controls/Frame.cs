using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static ChessWPF.Core;

namespace ChessWPF {
    public class Frame : UserControl {
        public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
            "Kind",
            typeof(FrameType),
            typeof(Frame),
            new FrameworkPropertyMetadata(FrameType.Menu));

        public FrameType Kind {
            get { return (FrameType)GetValue(KindProperty); }
            set { SetValue(KindProperty, value); }
        }

        //protected bool inTransition;
        //public event Action endAppear, endDisappear;
        //protected Storyboard sAppear, sDisappear;
        //protected DoubleAnimation opacityInc, opacityDec;

        public Frame() {
            Name = GetType().Name + "WPF";
            Width = realW;
            Opacity = 0;
            Visibility = Visibility.Collapsed;
            Foreground = Brushes.White;
            //inTransition = false;

            Resources = new ResourceDictionary();
            
            //opacityInc = new DoubleAnimation(0, 1, new TimeSpan(0, 0, 0, 0, 600));
            //opacityDec = new DoubleAnimation(1, 0, new TimeSpan(0, 0, 0, 0, 600));
            //sAppear = new Storyboard();
            //sDisappear = new Storyboard();

            //Storyboard.SetTargetName(sAppear, Name);
            //Storyboard.SetTargetName(sDisappear, Name);
            //Storyboard.SetTargetProperty(opacityInc, new PropertyPath("Opacity"));
            //Storyboard.SetTargetProperty(opacityDec, new PropertyPath("Opacity"));
            //sAppear.Children.Add(opacityInc);
            //sDisappear.Children.Add(opacityDec);
            //Resources.Add("Appear", sAppear);
            //Resources.Add("Disappear", sDisappear);

            //sAppear.Completed += _endAppear;
            //sDisappear.Completed += _endDisappear;
        }

        public static void goTo(Frame f) {
            if (f != fNow) {
                fNow.disappear();
                (fNow = f).appear();
            }
        }
        public void appear() {
            Opacity = 1;
            Visibility = Visibility.Visible;
            fShown = this;
            sBack.check();
        }
        public void disappear() {
            Visibility = Visibility.Collapsed;
            Opacity = 0;
            fShown = fNow;
        }
        public bool canGoBack() {
            return Kind.In(FrameType.Info, FrameType.Settings, FrameType.RemoteFrame, FrameType.Win) || (Kind == FrameType.PlayFrame && gamemode == Gamemodes.Replay);
        }

        //public async Task switchTo(Frame f) {
        //    if (!inTransition) {
        //        inTransition = true;
        //        Panel.SetZIndex(this, 0);
        //        Panel.SetZIndex(f, 1);
        //        if (f.Kind == FrameType.Settings)
        //            sBase.Settings.load();
        //        f.bringOn();
        //        f.appear();
        //        disappear();
        //        while (f.inTransition)
        //            await Task.Delay(1);
        //        inTransition = false;
        //    }
        //}

        //public void bringOn() {
        //    Visibility = Visibility.Visible;
        //    Opacity = 1;
        //}
        //public void appear() {
        //    FState = Kind;
        //    inTransition = true;
        //    Visibility = Visibility.Visible;
        //    _endAppear(null, null);
        //    //sAppear.Begin();
        //    sBase.Back.update();
        //}
        //public void disappear() {
        //    inTransition = true;
        //    _endDisappear(null, null);
        //    //sDisappear.Begin();
        //}
        //void _endAppear(object sender, EventArgs e) {
        //    inTransition = false;
        //    endAppear.throwEvent();
        //}
        //void _endDisappear(object sender, EventArgs e) {
        //    inTransition = false;
        //    Visibility = Visibility.Collapsed;
        //    endDisappear.throwEvent();
        //}
    }
}
