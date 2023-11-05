#pragma warning disable CS4014
using Micro;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Micro.NetLib;
using static ChessWPF.Core;

namespace ChessWPF {
    public partial class Base : Window {
        const int noSpamDelay = 4;
        OpenFileDialog openFileDialog;
        List<Key> pressed;
        DispatcherTimer waitTimer;
        TimeSpan timeLimit {
            get {
                int hh, mm, ss;
                int.TryParse(ptxtH.Text, out hh);
                int.TryParse(ptxtM.Text, out mm);
                int.TryParse(ptxtS.Text, out ss);
                return new TimeSpan(hh, mm, ss);
            }
        }
        User kickUser;
        int waitCounter = noSpamDelay;
        bool asking = false,
             fixingTime = false;

        public Base() {
            init();
            InitializeComponent();
            Settings.load();

            var TitleH = SystemParameters.WindowCaptionHeight + SystemParameters.ResizeFrameHorizontalBorderHeight;
            var VerticalW = SystemParameters.ResizeFrameVerticalBorderWidth;

            Width = realW + VerticalW * 4;
            Height = realH + 75 + TitleH * 1.5 - 1;
            MaxWidth = Width;
            MaxHeight = Height;
            MinWidth = Width;
            MinHeight = realH;
            pressed = new List<Key>(2);
            remote = false;
            waitTimer = new DispatcherTimer();
            kickUser = null;

            VirtualizingPanel.SetVirtualizationMode(this, VirtualizationMode.Recycling);
            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), fpm);

            sBase = this;
            sBack = Back;
            sDebug = new Debug();
            sPlayFrame = PlayFrame;
            sRemoteFrame = RemoteFrame;
            sBoard = PlayFrame.ChessBoard;
            sPlayMenu = PlayFrame.PlayMenu;
            sReplayMenu = PlayFrame.ReplayMenu;
            sPromotion = PlayFrame.Promotion;
            sWin = PlayFrame.Win;
            fNow = Menu;

            setEvents();

            openFileDialog = new OpenFileDialog() {
                Filter = "Replay files (*.chessrep)|*.chessrep",
                Title = "Select a replay",
                InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, repDir),
                Multiselect = false,
                CheckPathExists = true,
                CheckFileExists = true
            };

            prepareLog();
            writeLine("Initialized");
        }
        void setEvents() {
            Loaded += (a, b) => loaded();
            Menu.bLocal.Click += (a, b) => prepareShow();
            Menu.bRemote.Click += (a, b) => remoteShow();
            Menu.bReplay.Click += (a, b) => selectReplay();
            Menu.bInfo.Click += (a, b) => Frame.goTo(Info);
            Menu.bSettings.Click += (a, b) => Frame.goTo(Settings);

            waitTimer.Tick += waitTimeout;
            waitTimer.Interval = new TimeSpan(0, 0, 1);

            rtxtH.TextChanged += (a, b) => remoteAllow();
            rtxtP.TextChanged += (a, b) => remoteAllow();
            rbClient.Click += (a, b) => remoteConnect(false);
            rbServer.Click += (a, b) => remoteConnect(true);
            rbClose.Click += (a, b) => remoteClose();

            pStopwatch.Checked += (a, b) => { prepareUpdate((RadioButton)a, 0); prepareSave(0); };
            pCountdown.Checked += (a, b) => { prepareUpdate((RadioButton)a, 1); prepareSave(0); };
            ptxtH.TextChanged += (a, b) => {
                if (!fixingTime) {
                    prepareTimeFix(ptxtH);
                    prepareAllow((TextBox)a);
                    prepareSave(1);
                }
                if (!ptxtH.IsFocused)
                    prepareTimeDuo((TextBox)a);
            };
            ptxtM.TextChanged += (a, b) => {
                if (!fixingTime) {
                    prepareTimeFix(ptxtM);
                    prepareAllow((TextBox)a);
                    prepareSave(1);
                }
                if (!ptxtM.IsFocused)
                    prepareTimeDuo((TextBox)a);
            };
            ptxtS.TextChanged += (a, b) => {
                if (!fixingTime) {
                    prepareTimeFix(ptxtS);
                    prepareAllow((TextBox)a);
                    prepareSave(1);
                }
                if (!ptxtS.IsFocused)
                    prepareTimeDuo((TextBox)a);
            };
            ptxtH.LostFocus += (a, b) => { prepareTimeDuo((TextBox)a); prepareAllow((TextBox)a); };
            ptxtM.LostFocus += (a, b) => { prepareTimeDuo((TextBox)a); prepareAllow((TextBox)a); };
            ptxtS.LostFocus += (a, b) => { prepareTimeDuo((TextBox)a); prepareAllow((TextBox)a); };
            pWhite.Checked += (a, b) => { prepareUpdate((RadioButton)a, 2); prepareSave(2); };
            pBlack.Checked += (a, b) => { prepareUpdate((RadioButton)a, 3); prepareSave(2); };
            pUndo.Click += (a, b) => prepareSave(3);
            pRedo.Click += (a, b) => prepareSave(4);
            pDraw.Click += (a, b) => prepareSave(5);
            pbSwitch.Click += (a, b) => net.passAdmin();
            pbClose.Click += (a, b) => prepareBack();
            pbPlay.Click += (a, b) => { prepareSave(-1); preparePlay(); };
            
            DialogGiveUp.DialogClosing += (a, b) => sBoard.answerGiveUp((bool)b.Parameter);
            DialogWait.DialogClosing += (a, b) => waitClose();
            DialogAsk.DialogClosing += (a, b) => askSend((bool)b.Parameter);
            DialogKick.DialogClosing += (a, b) => kickClose((bool)b.Parameter);
            
            Closing += quit;
        }
        void loaded() {
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1) {
                var replay = args[1];
                if (File.Exists(replay) && Path.GetExtension(replay) == replayExt)
                    startReplay(replay);
            }
        }
        public void quit(object sender, CancelEventArgs e) {
            if (!net.connect.isNone)
                net.disconnect();
            if (GState != GameState.Menu)
                sBoard.clear();
            writeLine("Bye");
            try {
                logFile.Close();
            } catch (Exception) { }
        }

        public void remoteShow() {
            var ht = lastHostname;
            var pt = lastPort;
            rtxtH.Text = ht;
            rtxtP.Text = pt.ToString();
            Remote.IsOpen = true;
        }
        public void remoteAllow() {
            var pd = rtxtP.Text.isDigit();
            rbClient.IsEnabled = rtxtH.Text.isAddress() && pd;
            rbServer.IsEnabled = pd;
        }
        public void remoteClose() {
            Remote.IsOpen = false;
            if (rtxtH.Text.isAddress())
                lastHostname = rtxtH.Text;
            if (rtxtP.Text.isDigit())
                lastPort = int.Parse(rtxtP.Text);
            remoteStat("Ready");
        }
        public void remoteConnect(bool server) {
            var port = int.Parse(rtxtP.Text);
            if (server) {
                remoteStat("Listening...");
                net.startServer(port);
            } else {
                remoteStat("Connecting...");
                net.startClient(rtxtH.Text, port);
            }
        }
        public void remoteStat(string msg) {
            rStatus.Content = msg;
        }
        
        void waitTimeout(object sender, EventArgs e) {
            if (waitCounter <= 0) {
                dcButton.Content = "CANCEL";
                dcButton.IsEnabled = true;
                waitTimer.Stop();
                waitTimer.IsEnabled = false;
            } else {
                dcButton.Content = string.Format("CANCEL ({0})", waitCounter--);
            }
        }
        public void waitShow() {
            dcLabel.Content = "Waiting response...";
            dcButton.Content = string.Format("CANCEL ({0})", noSpamDelay + 1);
            dcButton.IsEnabled = false;
            asking = true;
            waitCounter = noSpamDelay;
            waitTimer.IsEnabled = true;
            waitTimer.Start();
            DialogWait.IsOpen = true;
        }
        public void waitResult(bool answer) {
            asking = false;
            dcButton.IsEnabled = !answer;
            waitTimer.Stop();
            waitTimer.IsEnabled = false;
            dcLabel.Content = answer ? "Request accepted" : "Request declined";
            if (answer)
                DialogWait.IsOpen = false;
            else
                dcButton.Content = "BACK";
        }
        public void waitClose() {
            if (asking) {
                net.sendCancelLast();
                waitTimer.Stop();
                waitTimer.IsEnabled = false;
            }
        }
        public void askMatchEnd() {
            dapiMatchEnd.IsEnabled = net.matching;
        }
        public void askShow(Request req) {
            sBoard.frameTop++;
            daWho.Text = req.sender.nickname;
            switch (req.quest) {
                case AskType.Play: daWhat.Text = "play with you";    break;
                case AskType.Undo: daWhat.Text = "undo last move";   break;
                case AskType.Redo: daWhat.Text = "repeat last move"; break;
                case AskType.Draw: daWhat.Text = "draw this match";  break;
            }
            chkIgnore.IsChecked = false;
            askMatchEnd();
            daBlock.Visibility = req.quest == AskType.Play ? Visibility.Visible : Visibility.Collapsed;
            DialogAsk.IsOpen = true;
        }
        public void askClose() {
            if (DialogAsk.IsOpen) {
                sBoard.frameTop--;
                DialogAsk.IsOpen = false;
            }
        }
        public void askSend(bool value) {
            if (!value && (chkIgnore.IsChecked ?? false) && (cbIgnore.SelectedIndex >= 0))
                net.addIgnore(net.pendingReq.sender, (IgnoreType)cbIgnore.SelectedIndex);
            Task.Run(delegate {
                Task.Delay(50);
                sBoard.frameTop--;
                net.sendAnswer(value);
            });
        }

        public void kickShow(User user) {
            if (net.connect.isServer) {
                kickUser = user;
                dkWho.Text = user.nickname;
                dkReason.Text = "";
                DialogKick.IsOpen = true;
            }
        }
        public void kickClose(bool choice) {
            if (net.connect.isServer && choice && kickUser != null) {
                var rsn = dkReason.Text;
                net.connect.kick(kickUser.id, string.IsNullOrWhiteSpace(rsn) ? "" : rsn);
                kickUser = null;
            }
            DialogKick.IsOpen = false;
        }

        public void prepareShow() {
            prepareLoad();
            prepareEnable();

            var vi = remote ? Visibility.Visible : Visibility.Collapsed;
            pColor.Visibility = vi;
            pPlayer.Visibility = match.gameSet.canPlay ? vi : Visibility.Collapsed;
            pFlags.Visibility = vi;
            pbSwitch.Visibility = vi;

            Preparation.IsOpen = true;
        }
        public void prepareLoad() {
            switch (match.gameSet.timeMode) {
                case Timing.Infinite:
                    pStopwatch.IsChecked = true;
                    pCountdown.IsChecked = false;
                    break;
                case Timing.Limit:
                    pStopwatch.IsChecked = false;
                    pCountdown.IsChecked = true;
                    break;
            }

            ptxtH.Text = (match.gameSet.timeLimit.Hours + "").PadLeft(2, '0');
            ptxtM.Text = (match.gameSet.timeLimit.Minutes + "").PadLeft(2, '0');
            ptxtS.Text = (match.gameSet.timeLimit.Seconds + "").PadLeft(2, '0');

            if (!remote) {
                pWhite.IsChecked = true;
                pBlack.IsChecked = false;
            } else {
                lWhite.Content = match.gameSet.white.nickname;
                lBlack.Content = match.gameSet.black.nickname;
                if (match.gameSet.canPlay) {
                    pWhite.IsChecked = match.gameSet.isWhite;
                    pBlack.IsChecked = match.gameSet.isBlack;
                }
            }

            pUndo.IsChecked = match.gameSet.undo;
            pRedo.IsChecked = match.gameSet.redo;
            pDraw.IsChecked = match.gameSet.draw;
        }
        public void prepareEnable() {
            var en = !remote || (remote && match.gameSet.canPlay && match.gameSet.isAdmin);

            pStopwatch.IsEnabled = en;
            pCountdown.IsEnabled = en;
            pTime.IsEnabled = match.gameSet.timeMode == Timing.Limit && en;
            pWhite.IsEnabled = en;
            pBlack.IsEnabled = en;
            pUndo.IsEnabled = en;
            pRedo.IsEnabled = en;
            pDraw.IsEnabled = en;
            pbSwitch.IsEnabled = en;
            pbPlay.IsEnabled = en;
        }
        public void prepareUpdate(RadioButton chk, int what) {
            if ((!remote || remote && match.gameSet.isAdmin && match.gameSet.canPlay) && (chk.IsChecked ?? false)) {
                switch (what) {
                    case 0:
                        pCountdown.IsChecked = false;
                        pTime.IsEnabled = false;
                        break;
                    case 1:
                        pStopwatch.IsChecked = false;
                        pTime.IsEnabled = true;
                        break;
                    case 2:
                        pBlack.IsChecked = false;
                        break;
                    case 3:
                        pWhite.IsChecked = false;
                        break;
                }
            }
        }
        public void prepareTimeFix(TextBox txtBox) {
            fixingTime = true;
            if (txtBox.Text.isDigit()) {
                int n = int.Parse(txtBox.Text),
                    i = txtBox.CaretIndex;
                string txt = "";
                if (txtBox == ptxtH)
                    txt = Math.Min(Math.Max(n, 0), 96) + "";
                if (txtBox == ptxtM)
                    txt = Math.Min(Math.Max(n, 0), 59) + "";
                if (txtBox == ptxtS)
                    txt = Math.Min(Math.Max(n, 0), 59) + "";
                if (txtBox.Text != txt && txtBox.Text != txt.PadLeft(2, '0')) {
                    txtBox.Text = txt;
                    txtBox.CaretIndex = i;
                }
            }
            fixingTime = false;
        }
        public void prepareTimeDuo(TextBox txtBox) {
            fixingTime = true;
            txtBox.Text = txtBox.Text.PadLeft(2, '0');
            fixingTime = false;
        }
        public void prepareAllow(TextBox txtBox) {
            if (!remote || (remote && match.gameSet.isAdmin)) {
                pbPlay.IsEnabled = txtBox.Text.isDigit() && timeLimit.Ticks > 0;
            }
        }
        public void prepareSave(int from) {
            if (Preparation.IsOpen && !net.changing) {
                int hh, mm, ss;
                int.TryParse(ptxtH.Text, out hh);
                int.TryParse(ptxtM.Text, out mm);
                int.TryParse(ptxtS.Text, out ss);

                match.gameSet.timeMode = (pStopwatch.IsChecked ?? true) ? Timing.Infinite : Timing.Limit;
                match.gameSet.timeLimit = timeLimit;
                match.gameSet.undo = pUndo.IsChecked ?? false;
                match.gameSet.redo = pRedo.IsChecked ?? false;
                match.gameSet.draw = pDraw.IsChecked ?? false;
                match.gameSet.save();
                if (remote && match.gameSet.isAdmin) {
                    if (((pBlack.IsChecked ?? false) && !match.gameSet.isBlack) || ((pWhite.IsChecked ?? false) && !match.gameSet.isWhite))
                        match.gameSet.swapPlayers();
                    net.syncSettings(from, match.gameSet.getAttr(from));
                }
            }
        }
        public void prepareClose() {
            Preparation.IsOpen = false;
        }
        public void prepareBack() {
            if (remote)
                net.playMatch(false);
            prepareClose();
        }
        public void preparePlay() {
            if (!remote) {
                startGame(Gamemodes.Local, 0);
            } else {
                net.playMatch(true);
            }
            prepareClose();
        }
        public void preparePlayRemote() {
            startGame(Gamemodes.Remote, match.gameSet.isWhite ? 0 : match.gameSet.isBlack ? 2 : 1);
        }

        public void selectReplay() {
            if (openFileDialog.ShowDialog() == true)
                startReplay(openFileDialog.FileName);
        }
        public void startReplay(string path) {
            replayRead(path);
            if (replay != null)
                startGame(Gamemodes.Replay, 1);
        }
        public void startGame(Gamemodes gm, int side = 0) {
            gamemode = gm;
            sBoard.clear();
            sBoard.setup();
            sBoard.tune(side, gm);
            PlayFrame.prepare();
            sBoard.start();
            Frame.goTo(PlayFrame);
        }
        public void endGame() {
            writeLine("## Ended game ##");
            if (sDebug.IsLoaded)
                sDebug.Close();

            GState = GameState.Menu;
        }

        void kUp(object sender, KeyEventArgs e) {
            if (pressed.Contains(e.Key))
                pressed.Remove(e.Key);
        }
        void kDown(object sender, KeyEventArgs e) {
            if (!pressed.Contains(e.Key))
                pressed.Add(e.Key);

            if (GState == GameState.Game) {
                if (pressed.Contains(Key.LeftCtrl)) {
                    if (gamemode != Gamemodes.Replay && sBoard.frameTop == 0) {
                        if (pressed.Contains(Key.Z))
                            sBoard.undoLast();
                        else if (pressed.Contains(Key.Y))
                            sBoard.redoLast();
                        e.Handled = true;
                    }
                } else {
                    if (pressed.Contains(Key.D) && gamemode == Gamemodes.Local) {
                        if (!sDebug.IsLoaded) {
                            pressed.Remove(Key.D);
                            sDebug = new Debug();
                            sDebug.Show();
                        }
                    }
                    if (gamemode == Gamemodes.Replay) {
                        if (pressed.Contains(Key.Left))
                            sBoard.replayPrev();
                        if (pressed.Contains(Key.Right))
                            sBoard.replayNext();
                        e.Handled = true;
                    }
                }
            }
            if (pressed.Contains(Key.Escape)) {
                if (DialogGiveUp.IsOpen) {
                    DialogGiveUp.IsOpen = false;
                    sBoard.answerGiveUp(false);
                } else if (DialogAsk.IsOpen) {
                    DialogAsk.IsOpen = false;
                    askSend(false);
                } else if (DialogWait.IsOpen) {
                    if (!waitTimer.IsEnabled) {
                        DialogWait.IsOpen = false;
                        waitClose();
                    }
                } else if (Preparation.IsOpen) {
                    prepareBack();
                } else if (GState == GameState.Game)
                    sBoard.askGiveUp();
                else if (sBack.isShown)
                    sBack.goBack();
            }
        }
    }
}