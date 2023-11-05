using System;
using System.Windows;
using System.Windows.Media;
using static ChessWPF.Core;

namespace ChessWPF {
    public partial class Trait{
        public Piece fromWhat;
        Color colorN = Color.FromArgb(187, 64, 64, 255),
              colorS = Color.FromArgb(187, 154, 64, 255),
              colorT = Color.FromArgb(187, 255, 64, 64),
              colorE = Color.FromArgb(187, 154, 154, 154),
              actual;
        bool
            _isMove = false,
            _isTarget = false,
            _isSpecial = false,
            _isLast = false;

        public bool isMove {
            get { return _isMove; }
            set {
                if (_isMove != value) {
                    if (_isMove = value)
                        moveOn();
                    else
                        moveOff();
                }
                setColor();
            }
        }
        public bool isTarget {
            get { return _isTarget; }
            set {
                _isTarget = value;
                setColor();
            }
        }
        public bool isSpecial {
            get { return _isSpecial; }
            set {
                _isSpecial = value;
                setColor();
            }
        }
        public bool isLast {
            get { return _isLast; }
            set {
                if (_isLast != value) {
                    if (_isLast = value)
                        lastOn();
                    else
                        lastOff();
                }
                setColor();
            }
        }
        public bool isEnemy {
            get { return !(fromWhat?.myTurn ?? true); }
        }

        public Trait() {
            InitializeComponent();
        }
        public Trait(int x, int y, int pl = 0) : base(x, y, pl, SlabType.Trait) {
            InitializeComponent();
            Width = CellSize;
            Height = CellSize;
            Margin = new Position(loc.absX * CellSize, loc.absY * CellSize);
            //lastC1.Center = lastC2.Center = new Point(CellSize / 2, CellSize / 2);
            //lastC1.RadiusX = CellSize / 4;
            //lastC1.RadiusY = CellSize / 4;
            //lastC2.RadiusX = CellSize / 6;
            //lastC2.RadiusY = CellSize / 6;
        }

        public void moveOn() {
            Visibility = Visibility.Visible;
            this.stopAnimation("MoveOff");
            this.beginAnimation("MoveOn");
        }
        public void moveOff() {
            this.stopAnimation("MoveOn");
            this.beginAnimation("MoveOff");
        }
        public void lastOn() {
            Visibility = Visibility.Visible;
            this.stopAnimation("LastOff");
            this.beginAnimation("LastOn");
        }
        public void lastOff() {
            this.stopAnimation("LastOn");
            this.beginAnimation("LastOff");
        }
        public void setColor() {
            if (_isTarget)
                circleMove.Fill = new SolidColorBrush(colorT);
            else {
                if (isEnemy)
                    actual = colorE;
                else if (_isSpecial)
                    actual = colorS;
                else
                    actual = colorN;
                circleMove.Fill = new SolidColorBrush(actual);
            }
        }

        void _invisible(object sender, EventArgs e) {
            if (!_isMove && !_isLast)
                Visibility = Visibility.Collapsed;
        }
    }
}
