using Micro;
using System;
using System.Diagnostics;
using Micro.ThreadTimer;
using static ChessWPF.Core;

namespace ChessWPF {
    public class Clock {
        int turn = 0;
        Stopwatch counter;
        Timer timer;
        TimeSpan tWhite, tBlack, ttWhite, ttBlack;
        public TimeSpan timeWhite {
            get { return tWhite + ttWhite; }
        }
        public TimeSpan timeBlack {
            get { return tBlack + ttBlack; }
        }
        public event Action Tick;

        public Clock() {
            counter = new Stopwatch();
        }
        public static Clock Parse(string txt) {
            var spl = txt.Split(',');
            return new Clock() {
                tWhite = TimeSpan.Parse(spl[0]),
                tBlack = TimeSpan.Parse(spl[1])
            };
        }
        public void Merge(Clock c) {
            tWhite = c.tWhite;
            tBlack = c.tBlack;
            restart();
            ttWhite = ttBlack = TimeSpan.Zero;
        }

        public void start() {
            timer = new Timer(100);
            timer.Tick += tick;
            counter.Restart();
            timer.start();
        }
        public void stop() {
            counter.Stop();
            timer?.stop();
            timer = null;
        }
        public void reset() {
            stop();
            tWhite = tBlack = ttWhite = ttBlack = TimeSpan.Zero;
            turn = 0;
        }
        public void restart() {
            if (counter.IsRunning)
                counter.Restart();
        }
        public void setTurn(int turn) {
            if (this.turn != turn) {
                this.turn = turn;
                var tw = ttWhite;
                var tb = ttBlack;
                restart();
                ttWhite = ttBlack = TimeSpan.Zero;
                tWhite += tw;
                tBlack += tb;
            }
        }
        void tick() {
            if (turn.isFirst())
                ttWhite = counter.Elapsed;
            else
                ttBlack = counter.Elapsed;
            if (GState == GameState.Game)
                Invoke(new Action(() => Tick?.Invoke()));
        }

        public override string ToString() {
            return string.Join(",", timeWhite, timeBlack);
        }
    }
}
