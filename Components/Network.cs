#pragma warning disable CS0660, CS0661
using Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Micro.NetLib;
using static Micro.NetLib.Core;
using static ChessWPF.Core;

namespace ChessWPF {
    public class Network {
        public Connection connect;
        public NetState state { get; private set; }
        public Guid myID { get; private set; }
        public bool changing = false,
                    serverMode = false,
                    matching = false,
                    moving = false,
                    winning = false;
        public Request pendingReq, queueReq;
        bool resultReturn = false;
        List<Request> pendingReqs, queueReqs, matchReqs;
        List<UndoRedoReq> urReqs;
        List<WinRequest> winReqs;
        List<GamePlay> matches;
        List<Ignore> ignores;

        public Network() {
            myID = Guid.NewGuid();
            connect = new Connection();
            connect.startResult += startResult;
            connect.stoppedClient += stoppedClient;
            connect.stoppedServer += stoppedServer;
            connect.updateUserList += () => { };
            connect.updateForm += () => { };
            connect.incomingMessage += incomingMessage;
            connect.incomingUser += incomingUser;
            connect.leavingUser += leavingUser;
            ignores = new List<Ignore>();
        }

        public void startClient(string ip, int port) {
            Task.Run(async () => {
                bool thereIsResult = false;
                for (int l = 0; l < 3; l++) {
                    connect.startClient(Nickname, ip, port, myID);
                    resultReturn = false;
                    await Task.Delay(3000);
                    if (thereIsResult = (resultReturn || !connect.isInternalConnected))
                        break;
                    try { connect.stop(); } catch (Exception) { }
                }
                if (!thereIsResult) {
                    resultReturn = true;
                    Invoke(new Action<string>(sBase.remoteStat), "Connection error (retry later)");
                }
            });
        }
        public void startServer(int port) {
            connect.startServer(Nickname, port, myID);
        }
        void startResult(bool success) {
            resultReturn = true;
            if (success) {
                remote = true;
                if (connect.isServer) {
                    matchReqs = new List<Request>();
                    urReqs = new List<UndoRedoReq>();
                    winReqs = new List<WinRequest>();
                }
                pendingReqs = new List<Request>();
                queueReqs = new List<Request>();
                matches = new List<GamePlay>();
                setStatus(NetState.waiting);
                writeText(connect.isServer ? "Listening" : "Connected");
                connect.startProcessing();
            } else
                Invoke(new Action<string>(sBase.remoteStat), "Unable to " + (connect.isServer ? "listen" : "connect"));
        }
        void stoppedClient(stopReason reason, string additional) {
            if (resultReturn)
                Invoke(new Action<string>(sBase.remoteStat), connect.getLeaveReason(reason, additional));
            setStatus(NetState.none);
            clean();
        }
        void stoppedServer() {
            setStatus(NetState.none);
            clean();
        }
        void clean() {
            pendingReqs?.Clear();
            queueReqs?.Clear();
            matchReqs?.Clear();
            urReqs?.Clear();
            winReqs?.Clear();
            matches?.ForEach(m => m.playing = false);
            matches?.Clear();
            
            matches = null;
            pendingReq = null;
            queueReq = null;
            
            pendingReqs = null;
            queueReqs = null;
            matchReqs = null;
            urReqs = null;
            winReqs = null;
            
            changing = false;
            serverMode = false;
            matching = false;
            moving = false;
            resultReturn = false;
            match = new GamePlay();

            checkIgnore();
        }

        void updateUserList() {
            Invoke(new Action(sRemoteFrame.clearListes));
            //var wait = from u in connect.Users
            //           where !matches.Exists(m => m.gameSet.playing(u)) && u != connect.me
            //           select u;

            checkIgnore();

            //var inst = new List<Action>() {
            //    () => {
            //        foreach (var u in connect.Users) {
            //            Invoke(new Action<User, bool>(sRemoteFrame.addUser), u, matches.Exists(m => m.gameSet.playing(u)));
            //        }
            //    },
            //    () => Invoke(new Action<User, bool>(sRemoteFrame.addUser), connect.me, matches.Exists(m => m.gameSet.playing(connect.me)))
            //};
            //if (connect.isServer)
            //    inst.Reverse();
            //inst[0]();
            //inst[1]();
            foreach (var u in connect.Users)
                Invoke(new Action<User, bool>(sRemoteFrame.addUser), u, matches.Exists(m => m.gameSet.playing(u)));

            foreach (GamePlay m in matches) {
                if (!m.gameSet.isEmpty)
                    Invoke(new Action<GamePlay>(sRemoteFrame.addMatch), m);
            }
        }
        void updateFixDisconnect() {
            var mLink = matches.FindIndex(m => m.id == match.id);
            if (mLink >= 0 && !matches.Contains(match)) {
                match.Merge(matches[mLink]);
                matches[mLink] = match;
            }

            if (connect.isServer) {
                foreach (var m in matches.ToList()) {
                    if (!connect.Users.Exists(u => u == m.gameSet.white || u == m.gameSet.black)) {
                        writeMatchDelete(m);
                        matches.Remove(m);
                        continue;
                    }
                    foreach (var s in m.spectators.ToList()) {
                        if (!connect.Users.Concat(new User[] { connect.serverUser }).Contains(s))
                            m.spectators.Remove(s);
                    }
                }
            }
            if (matching && !matches.Contains(match) && !match.wonIs(WinType.Disconnect))
                closeMatch();

            queueReqs.RemoveAll(r => !connect.existUser(r.to));
            pendingReqs.RemoveAll(r => !connect.existUser(r.sender));
            if (queueReq != null && !connect.existUser(queueReq.to))
                execAnswer(queueReq, false);
            if (pendingReq != null && !connect.existUser(pendingReq.sender))
                execCancel(pendingReq);
            if (connect.isServer)
                matchReqs.RemoveAll(r => !connect.existUser(r.sender) || !connect.existUser(r.to));
        }
        void incomingUser(User user) {
            if (connect.isServer)
                syncListes();
            writeText(string.Format("{0} connected", user.nickname));
        }
        void leavingUser(User user, stopReason reason, string additional) {
            if (connect.isServer) {
                GamePlay m;
                if ((m = matches.Find(n => n.gameSet.playing(user))) != null) {
                    matches.Remove(m);
                    writeMatchStop(m);
                } else if ((m = matches.Find(n => n.spectators.Contains(user))) != null)
                    m.spectators.Remove(user);
                syncListes();
            }
            writeText(connect.getLeaveReason(reason, additional, user));
        }

        #region Messages
        void incomingMessage(User user, string[] text) {
            switch (StringEnum<NetCmds>(text[0])) {
                case NetCmds.Message:
                    execMessage(user, text[1]);
                    break;
                case NetCmds.Matches:
                    execMatches((from t in text
                                 where t != text[0]
                                 select GamePlay.Parse(t)).ToList());
                    break;
                case NetCmds.Question:
                    execQuestion(Request.Parse(text[1]));
                    break;
                case NetCmds.Cancel:
                    execCancel(Request.Parse(text[1]));
                    break;
                case NetCmds.Answer:
                    execAnswer(Request.Parse(text[1]), text[2] == "1");
                    break;
                case NetCmds.NewMatch:
                    execNewMatch(Request.Parse(text[1]));
                    break;
                case NetCmds.Match:
                    execMatch(GamePlay.Parse(text[1]));
                    break;
                case NetCmds.Spectate:
                    execSpectate(user, Guid.Parse(text[1]));
                    break;
                case NetCmds.Settings:
                    execSettings(Guid.Parse(text[1]), int.Parse(text[2]), text[3]);
                    break;
                case NetCmds.PassAdmin:
                    execPassAdmin(Guid.Parse(text[1]));
                    break;
                case NetCmds.Play:
                    execPlay(Guid.Parse(text[1]), ref text);
                    break;
                case NetCmds.Stop:
                    execStop(user, Guid.Parse(text[1]), ref text);
                    break;
                case NetCmds.Time:
                    execTime(Guid.Parse(text[1]), Clock.Parse(text[2]));
                    break;
                case NetCmds.Move:
                    execMove(Guid.Parse(text[1]), Move.Parse(text[2]), ref text);
                    break;
                case NetCmds.UndoRedo:
                    execUndoRedo(UndoRedoReq.Parse(text[1]), ref text);
                    break;
                case NetCmds.Win:
                    execWin(WinRequest.Parse(text[1]), ref text);
                    break;
            }
        }
        void execMessage(User user, string text) {
            writeText(string.Format("{0}: {1}", user.nickname, text));
        }
        void execMatches(List<GamePlay> newMatches) {
            var rem = from m in matches
                      where !newMatches.Exists(n => n.id == m.id)
                      select m;
            var edit = from m in newMatches
                       let n = matches.Find(n => n.id == m.id)
                       where n != null
                       select new KeyValuePair<GamePlay, GamePlay>(n, m);
            var add = from m in newMatches
                      where !matches.Exists(n => n.id == m.id)
                      select m;

            foreach (var m in rem.ToList()) {
                matches.Remove(m);
                writeMatchStop(m);
                if (m.id == match.id)
                    closeMatch();
            }

            foreach (var m in edit.ToList())
                m.Key.Merge(m.Value);

            foreach (var m in add.ToList()) {
                matches.Add(m);
                writeMatchStart(m);
            }

            updateFixDisconnect();
            updateUserList();
        }
        void execQuestion(Request req) {
            pendingReqs.Add(req);
            proceedRequests();
        }
        void execCancel(Request req) {
            var req2 = pendingReqs.Find(r => r == req);
            if (req2 != null) {
                pendingReqs.Remove(req2);
            } else if (pendingReq == req) {
                Invoke(new Action(sBase.askClose));
                pendingReq = null;
                proceedRequests();
            }
        }
        void execAnswer(Request req, bool value) {
            if (queueReq == req) {
                Invoke(new Action<bool>(sBase.waitResult), value);
                if (value)
                    completeRequest(queueReq);
                queueReq = null;
                proceedRequests();
            }
        }
        void execNewMatch(Request req) {
            if (connect.isServer) {
                Request req2 = matchReqs.Find(r => r == req);
                if (req2 == null) {
                    matchReqs.Add(req);
                } else {
                    matchReqs.Remove(req2);
                    var nMatch = new GamePlay() {
                        gameSet = new GameSet() {
                            admin = req.sender,
                            draw = true,
                            redo = true,
                            undo = true,
                            white = req.sender,
                            black = req.to,
                            timeMode = Timing.Infinite,
                            timeLimit = new TimeSpan(1, 0, 0)
                        }
                    };
                    matches.Add(nMatch);
                    writeMatchStart(nMatch);
                    syncListes();
                    var nMatchS = ((GameID)nMatch).ToString();
                    connect.send(req.sender, EnumString(NetCmds.Match), nMatchS);
                    connect.send(req.to, EnumString(NetCmds.Match), nMatchS);
                }
            }
        }
        void execMatch(GamePlay match) {
            if (matches.Exists(n => n.id == Core.match.id))
                leaveMatch();
            var mNew = matches.Find(n => n.id == match.id);
            if (mNew != null) {
                if (connect.isClient)
                    mNew.Merge(match);
                Core.match = mNew;
                matching = true;
                Invoke(new Action(sBase.askMatchEnd));
                if (mNew.playing) {
                    setStatus(NetState.playing);
                    moving = true;
                    for (int i = 0; i < mNew.movCount; i++) {
                        Invoke(new Action<bool>(mNew.chrono[i].perform), false);
                        Invoke(new Action(sBoard.nextTurn));
                    }
                    moving = false;
                    sBoard.chrono.AddRange(mNew.chrono.Skip(mNew.movCount));
                    if (!mNew.wonIs(0)) {
                        winning = true;
                        Invoke(new Action<WinType, User>(sBoard.win), mNew.won.type, mNew.won.subject);
                        winning = false;
                    }
                } else
                    Invoke(new Action(sBase.prepareShow));
            }
        }
        void execSpectate(User user, Guid matchID) {
            var m = matches.Find(n => n.id == matchID);
            if (m != null && connect.isServer && !m.spectators.Contains(user)) {
                m.spectators.Add(user);
                connect.send(user, EnumString(NetCmds.Match), m.ToString());
                connect.send(user, EnumString(NetCmds.Settings), m.id + "", "-1", m.gameSet.ToString());
                syncTime(m);
            }
        }
        void execSettings(Guid matchID, int type, string value) {
            changing = true;
            var m = matches.Find(n => n.id == matchID);
            if (m != null) {
                if (type == -1)
                    m.gameSet = GameSet.Parse(value);
                else
                    m.gameSet.setAttr(type, value);
                if (match.id == matchID)
                    Invoke(new Action(sBase.prepareLoad));
            }
            changing = false;
        }
        void execPassAdmin(Guid matchID) {
            var m = matches.Find(n => n.id == matchID);
            if (m != null) {
                m.gameSet.passAdmin();
                if (match.id == matchID)
                    Invoke(new Action(sBase.prepareEnable));
            }
        }
        void execPlay(Guid matchID, ref string[] text) {
            var m = matches.Find(n => n.id == matchID);
            if (m != null) {
                if (connect.isServer && !serverMode) {
                    serverMode = true;
                    sendAll(m, -1, text);
                    m.playing = true;
                    syncListes();
                    syncTime(m);
                    serverMode = false;
                } else if (match.id == matchID) {
                    match.playing = true;
                    setStatus(NetState.playing);
                    Invoke(new Action(sBase.prepareClose));
                }
            }
        }
        void execStop(User user, Guid matchID, ref string[] text) {
            var m = matches.Find(n => n.id == matchID);
            if (m != null) {
                if (connect.isServer && !serverMode) {
                    serverMode = true;
                    if (m.gameSet.playing(user)) {
                        sendAll(m, -1, text);
                        m.playing = false;
                        matches.Remove(m);
                        writeMatchStop(m);
                        syncListes();
                    } else {
                        m.spectators.Remove(user);
                        connect.send(user, text);
                    }
                    serverMode = false;
                } else if (match.id == matchID) {
                    closeMatch();
                }
            }
        }
        void execTime(Guid matchID, Clock clock) {
            if (match.id == matchID && match.playing) {
                Invoke(new Action<Clock>(sBoard.setTime), clock);
            }
        }
        void execMove(Guid matchID, Move mov, ref string[] text) {
            var m = matches.Find(n => n.id == matchID);
            if (m != null) {
                if (connect.isServer && !serverMode) {
                    serverMode = true;
                    m.addChrono(mov);
                    sendAll(m, m.turn, text);
                    m.movCount++;
                    syncTime(m);
                    serverMode = false;
                } else if (match.id == matchID && match.playing) {
                    if (moving) {
                        match.addChrono(mov);
                        match.movCount++;
                    } else {
                        moving = true;
                        Invoke(new Action<bool>(mov.perform), false);
                        Invoke(new Action(sBoard.nextTurn));
                        moving = false;
                    }
                }
            }
        }
        void execUndoRedo(UndoRedoReq req, ref string[] text) {
            var m = matches.Find(n => n.id == req.matchID);
            if (m != null) {
                if (connect.isServer && !serverMode) {
                    var urr = urReqs.Find(n => n == req);
                    if (urr == null) {
                        urReqs.Add(req);
                    } else {
                        urReqs.Remove(urr);
                        serverMode = true;
                        sendAll(m, -1, text);
                        syncTime(m);
                        serverMode = false;
                    }
                } else if (m.playing) {
                    if (match.id == req.matchID) {
                        moving = true;
                        if (req.undoRedo)
                            Invoke(new Action(sBoard.redoLast));
                        else
                            Invoke(new Action(sBoard.undoLast));
                        moving = false;
                    }
                    m.movCount += req.undoRedo ? 1 : -1;
                }
            }
        }
        void execWin(WinRequest req, ref string[] text) {
            var m = matches.Find(n => n.id == req.matchID);
            if (m != null) {
                if (connect.isServer && !serverMode) {
                    serverMode = true;
                    if (!req.specialType) {
                        var wr = winReqs.Find(n => n == req);
                        if (wr == null) {
                            winReqs.Add(req);
                        } else {
                            winReqs.Remove(req);
                            sendAll(m, req.subject, text);
                        }
                    } else {
                        winReqs.RemoveAll(n => n.matchID == req.matchID);
                        sendAll(m, req.subject, text);
                    }
                    m.won = req;
                    syncTime(m);
                    syncListes();
                    serverMode = false;
                } else if (match.id == req.matchID && match.playing) {
                    winning = true;
                    Invoke(new Action<WinType, User>(sBoard.win), req.type, req.subject);
                    match.won = req;
                    winning = false;
                }
            }
        }
        #endregion
        #region Sync
        void syncListes() {
            updateFixDisconnect();
            updateUserList();
            syncMatches();
        }
        void syncMatches() {
            var par = new List<string> { EnumString(NetCmds.Matches) };
            var ser = from m in matches
                      select ((GameID)m).ToString();
            connect.broadcast(null, par.Concat(ser).ToArray());
        }
        void syncTime(GamePlay m) {
            if (m.playing) {
                m.clock.setTurn(m.turn);
                sendAll(m, -1, EnumString(NetCmds.Time), m.id + "", m.clock.ToString());
            }
        }
        #endregion

        #region Actions
        void setStatus(NetState state) {
            if (state != this.state) {
                switch (this.state) {
                    case NetState.none:
                        Invoke(new Action(sBase.remoteClose));
                        break;
                    case NetState.playing:
                        Invoke(new Action(sBase.endGame));
                        break;
                }
                switch (this.state = state) {
                    case NetState.none:
                        if (matching && match.gameSet.canPlay) {
                            winGame(WinType.Disconnect, connect.myself);
                            Thread.Sleep(100);
                        }
                        connect.stop();
                        remote = false;
                        writeText("Disconnected");
                        Invoke(new Action(sRemoteFrame.clearListes));
                        Invoke(new Action(sBase.remoteShow));
                        Invoke(new Action(sBase.askClose));
                        Invoke(new Action(sBase.waitClose));
                        Invoke(new Action(sBase.prepareClose));
                        Invoke(new Action<bool>(sBase.kickClose), false);
                        Invoke(new Action<Frame>(Frame.goTo), sBase.Menu);
                        break;
                    case NetState.waiting:
                        Invoke(new Action<Frame>(Frame.goTo), sRemoteFrame);
                        updateUserList();
                        break;
                    case NetState.playing:
                        Invoke(new Action(sBase.preparePlayRemote));
                        break;
                }
            }
        }
        void sendAll(GamePlay m, User skip, params string[] text) {
            var n = -1;
            if (skip != null) {
                if (skip == m.gameSet.white)
                    n = 0;
                if (skip == m.gameSet.black)
                    n = 1;
            }
            sendAll(m, n, text);
        }
        void sendAll(GamePlay m, int skip, params string[] text) {
            if (skip != 0)
                connect.send(m.gameSet.white, text);
            if (skip != 1)
                connect.send(m.gameSet.black, text);
            foreach (var spect in m.spectators)
                connect.send(spect, text);
        }
        void writeText(string text) {
            Invoke(new Action<string>(sRemoteFrame.writeChat), text);
        }
        void writeMatch(GamePlay m, string action) {
            writeText(string.Format("Match {0}: {1} ~ {2}", action, m.gameSet.white?.nickname ?? "<white>", m.gameSet.black?.nickname ?? "<black>"));
        }
        void writeMatchStart(GamePlay m) {
            writeMatch(m, "started");
        }
        void writeMatchStop(GamePlay m) {
            writeMatch(m, "ended");
        }
        void writeMatchDelete(GamePlay m) {
            writeMatch(m, "removed");
        }
        void closeMatch() {
            matching = false;
            if (match.playing)
                Invoke(new Action(sBoard.resetTime));
            Invoke(new Action(sBase.askMatchEnd));
            match = new GamePlay();
            Invoke(new Action(sBase.prepareClose));
            setStatus(NetState.waiting);
        }
        #endregion
        #region Public Entry Points
        public void disconnect() {
            setStatus(NetState.none);
        }
        public void sendText(string text) {
            execMessage(connect.myself, text);
            connect.sendAll(EnumString(NetCmds.Message), text);
        }
        public void syncSettings(int from, string str) {
            if (match.gameSet.isAdmin)
                connect.sendAll(EnumString(NetCmds.Settings), match.id + "", from + "", str);
        }
        public void passAdmin() {
            if (match.gameSet.canPlay && match.gameSet.isAdmin) {
                match.gameSet.passAdmin();
                Invoke(new Action(sBase.prepareEnable));
                connect.sendAll(EnumString(NetCmds.PassAdmin), match.id + "");
            }
        }
        public void playMatch(bool action) {
            if (action)
                connect.send(connect.serverUser, EnumString(NetCmds.Play), match.id + "");
            else
                leaveMatch();
        }
        public void leaveMatch() {
            connect.send(connect.serverUser, EnumString(NetCmds.Stop), match.id + "");
        }
        public void spectate(GamePlay match) {
            connect.send(connect.serverUser, EnumString(NetCmds.Spectate), match.id + "");
        }
        public void performMove(Move m) {
            if (!moving)
                connect.send(connect.serverUser, EnumString(NetCmds.Move), match.id + "", m.ToString());
        }
        public void winGame(WinType mode, User subject = null) {
            if (match.gameSet.canPlay && !winning) {
                connect.send(connect.serverUser, EnumString(NetCmds.Win), new WinRequest(match.id, mode, subject).ToString());
            }
        }
        public void returnToLobby() {
            if (state == NetState.waiting || (state == NetState.playing))
                leaveMatch();
            else
                closeMatch();
        }
        public User getSubject(WinType mode) {
            User subject = null;
            switch (mode) {
                case WinType.White:      subject = match.gameSet.white; break;
                case WinType.Black:      subject = match.gameSet.black; break;
                case WinType.GiveUp:
                case WinType.Disconnect: subject = connect.myself;      break;
            }
            return subject;
        }
        #endregion

        #region Requests
        void proceedRequests() {
            checkIgnore();

            if (queueReq == null && queueReqs.Count > 0) {
                queueReqs.Remove(queueReq = queueReqs[0]);
                Invoke(new Action(sBase.waitShow));
                connect.send(queueReq.to, EnumString(NetCmds.Question), queueReq.ToString());
            }

            if (pendingReq == null && pendingReqs.Count > 0) {
                pendingReqs.Remove(pendingReq = pendingReqs[0]);
                if (ignores.Exists(i => i.user == pendingReq.sender))
                    sendAnswer(false);
                else
                    Invoke(new Action<Request>(sBase.askShow), pendingReq);
            }
        }
        public void queueRequest(User user, AskType quest) {
            queueReqs.Add(new Request(connect.myself, user, quest));
            proceedRequests();
        }
        public void queueRequestP2(AskType quest) {
            queueRequest(match.gameSet.notMe, quest);
        }
        public void sendCancelLast() {
            if (queueReq != null) {
                connect.send(queueReq.to, EnumString(NetCmds.Cancel), queueReq.ToString());
                queueReq = null;
                proceedRequests();
            }
        }
        public void sendAnswer(bool answer) {
            if (pendingReq != null) {
                connect.send(pendingReq.sender, EnumString(NetCmds.Answer), pendingReq.ToString(), answer ? "1" : "0");
                if (answer)
                    completeRequest(pendingReq);
                pendingReq = null;
                proceedRequests();
            }
        }
        public void completeRequest(Request req) {
            switch (req.quest) {
                case AskType.Play:
                    if (matching)
                        returnToLobby();
                    connect.send(connect.serverUser, EnumString(NetCmds.NewMatch), req.ToString());
                    break;
                case AskType.Undo:
                    connect.send(connect.serverUser, EnumString(NetCmds.UndoRedo), new UndoRedoReq(match.id, false).ToString());
                    break;
                case AskType.Redo:
                    connect.send(connect.serverUser, EnumString(NetCmds.UndoRedo), new UndoRedoReq(match.id, true).ToString());
                    break;
                case AskType.Draw:
                    winGame(WinType.Draw);
                    break;
            }
        }
        #endregion
        #region Ignores
        public void addIgnore(User u, IgnoreType t) {
            checkIgnore();
            ignores.Add(new Ignore(u, t));
        }
        void checkIgnore() {
            ignores.RemoveAll(i => i.isElapsed);
        }
        #endregion
    }

    public class Request {
        public static Regex regx = new Regex(string.Format(@"^{0},{1},{2}$",
                                                           @"[(\[{]?([-0-9a-f]{36})[)\]}]?",
                                                           @"[(\[{]?([-0-9a-f]{36})[)\]}]?",
                                                           @"(\d+)"));
        public User sender, to;
        public AskType quest;

        public Request(User from, User to, AskType quest) {
            this.sender = from;
            this.to = to;
            this.quest = quest;
        }
        public static Request Parse(string txt) {
            var grps = regx.Match(txt).Groups;
            return new Request(
                net.connect.getUser(Guid.Parse(grps[1].Value)),
                net.connect.getUser(Guid.Parse(grps[2].Value)),
                (AskType)int.Parse(grps[3].Value));
        }

        public override string ToString() {
            return string.Join(",", sender.id, to.id, (int)quest);
        }
        public static bool operator ==(Request a, Request b) {
            return (a?.sender == b?.sender) && (a?.to == b?.to) && (a?.quest == b?.quest);
        }
        public static bool operator !=(Request a, Request b) {
            return (a?.sender != b?.sender) || (a?.to != b?.to) || (a?.quest != b?.quest);
        }
    }
    public class UndoRedoReq {
        public static Regex regx = new Regex(string.Format(@"^{0},{1}$",
                                                           @"[(\[{]?([-0-9a-f]{36})[)\]}]?",
                                                           @"(\d+)"));
        public Guid matchID;
        public bool undoRedo;

        public UndoRedoReq(Guid m, bool ur) {
            matchID = m;
            undoRedo = ur;
        }
        public static UndoRedoReq Parse(string txt) {
            var grps = regx.Match(txt).Groups;
            return new UndoRedoReq(
                Guid.Parse(grps[1].Value),
                grps[2].Value == "1");
        }

        public override string ToString() {
            return string.Join(",", matchID, undoRedo ? "1" : "0");
        }
        public static bool operator ==(UndoRedoReq a, UndoRedoReq b) {
            return (a?.matchID == b?.matchID) && (a?.undoRedo == b?.undoRedo);
        }
        public static bool operator !=(UndoRedoReq a, UndoRedoReq b) {
            return (a?.matchID != b?.matchID) || (a?.undoRedo != b?.undoRedo);
        }
    }
    public class WinRequest {
        public static Regex regx = new Regex(string.Format(@"^{0},{1},{2}$",
                                                           @"[(\[{]?([-0-9a-f]{36})[)\]}]?",
                                                           @"(\d+)",
                                                           @"(?:[(\[{]?([-0-9a-f]{36})[)\]}]?)?"));
        public Guid matchID;
        public WinType type;
        public User subject;
        public bool specialType {
            get { return type == WinType.GiveUp || type == WinType.Disconnect; }
        }

        public WinRequest(Guid m, WinType t, User s = null) {
            matchID = m;
            type = t;
            subject = s;
        }
        public static WinRequest Parse(string txt) {
            var grps = regx.Match(txt).Groups;
            User subj = null;
            if (grps[3].Value != "")
                subj = net.connect.getUser(Guid.Parse(grps[3].Value));
            return new WinRequest(
                Guid.Parse(grps[1].Value),
                (WinType)int.Parse(grps[2].Value),
                subj);
        }

        public override string ToString() {
            return string.Join(",", matchID, (int)type, subject?.id);
        }
        public static bool operator ==(WinRequest a, WinRequest b) {
            return (a?.matchID == b?.matchID) && (a?.type == b?.type) && (a?.subject == b?.subject);
        }
        public static bool operator !=(WinRequest a, WinRequest b) {
            return (a?.matchID != b?.matchID) || (a?.type != b?.type) || (a?.subject != b?.subject);
        }
    }
    public class Ignore {
        public readonly User user;
        public readonly IgnoreType type;
        public readonly Guid matchID;
        public readonly DateTime added;
        public bool isElapsed {
            get {
                switch (type) {
                    case IgnoreType.tenMinutes: return (DateTime.Now - added).TotalMinutes >= 10;
                    case IgnoreType.matchEnds:  return match.id != matchID || !match.wonIs(0);
                    case IgnoreType.disconnect: return net.connect.isNone;
                    default:                    return false;
                }
            }
        }

        public Ignore(User u, IgnoreType t) {
            user = u;
            type = t;
            matchID = match.id;
            added = DateTime.Now;
        }
    }
}
