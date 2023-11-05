using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static ChessWPF.Core;

namespace ChessWPF {
    public partial class FakePiece {
        public delegate void ClickedEvent(FakePiece sender);
        public event ClickedEvent Clicked;
        
        public string symbol;
        public SlabType type;

        bool
            mHold = false,
            mousehover = false,
            _isHover = false,
            _isPress = false;
        public bool isHover {
            get { return _isHover; }
            set {
                if (_isHover != value) {
                    _isHover = value;
                    this.beginAnimation(value ? "Hover" : "Leave");
                    if (!value)
                        _isPress = false;
                }
            }
        }
        public bool isPress {
            get { return _isPress; }
            set {
                if (_isPress != value) {
                    _isPress = value;
                    this.beginAnimation(value ? "Press" : "Leave");
                    if (!value)
                        _isHover = false;
                }
            }
        }

        public FakePiece() {
            InitializeComponent();
            Width = CellSize * 1.5;
            Height = CellSize * 1.5;
            Clip = new EllipseGeometry(
                new Point(CellSize * 1.5 / 2, CellSize * 1.5 / 2),
                CellSize * 1.5 / 2 - CellPadding,
                CellSize * 1.5 / 2 - CellPadding);
            
            glyph.FontSize = CellSize * 1.5 / 4 * 3;
            
            MouseEnter += mEnter;
            MouseLeave += mLeave;
            MouseDown += mPress;
            MouseUp += mRelease;
            MouseMove += mMove;
        }
        public void setType(SlabType type) {
            this.type = type;
            switch (type) {
                case SlabType.Pawn:
                    symbol = "\u265F";
                    break;
                case SlabType.Rook:
                    symbol = "\u265C";
                    break;
                case SlabType.Knight:
                    symbol = "\u265E";
                    break;
                case SlabType.Bishop:
                    symbol = "\u265D";
                    break;
                case SlabType.Queen:
                    symbol = "\u265B";
                    break;
                case SlabType.King:
                    symbol = "\u265A";
                    break;
                case SlabType.Null:
                    symbol = "";
                    break;
            }
            glyph.Text = symbol;
        }

        public void mEnter(object sender, EventArgs e) {
            if (!mHold)
                isHover = true;
        }
        public void mLeave(object sender, EventArgs e) {
            if (!mHold)
                isHover = false;
        }
        public void mPress(object sender, MouseEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                mHold = true;
                CaptureMouse();
                isPress = true;
            }
        }
        public void mRelease(object sender, MouseEventArgs e) {
            if (mHold) {
                mHold = false;
                ReleaseMouseCapture();
                isPress = false;
                if (mousehover)
                    Clicked(this);
            }
        }
        public void mMove(object sender, MouseEventArgs e) {
            if (IsMouseCaptured) {
                bool temp = false;
                VisualTreeHelper.HitTest(this,
                    d => {
                        if (d == this)
                            temp = true;
                        return HitTestFilterBehavior.Stop;
                    },
                    ht => HitTestResultBehavior.Stop,
                    new PointHitTestParameters(e.GetPosition(this))
                );
                mousehover = temp;
            }
        }
    }
}
