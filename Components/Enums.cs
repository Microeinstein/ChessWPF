using System;

namespace ChessWPF {
    public enum GameState {
        Menu,
        Game,
        Win
    }
    public enum FrameType {
        Menu,
        Info,
        Settings,
        RemoteFrame,
        PlayFrame,
        Promotion,
        Win
    }
    public enum Gamemodes {
        Local,
        Remote,
        Replay
    }
    public enum ReplayNode {
        Move,
        Undo,
        Redo
    }
    public enum NetCmds {
        Message,        //Messaggio chat
        Matches,        //Lista delle partite (senza mosse eseguite)
        Question,       //Invio richiesta
        Cancel,         //Annulla richiesta
        Answer,         //Risposta alla richiesta
        NewMatch,       //Richiesta/creazione di nuova partita
        Match,          //Invio oggetto match esistente oppure nuovo oggetto partita
        Spectate,       //Richiesta di essere spettatore
        Settings,       //Scambio parametri partita
        PassAdmin,      //Donazione potere di modifica
        Play,           //Inizio/cancellazione partita
        Stop,           //Abbandona partita
        Time,           //Sincronizzazione tempi
        Move,           //Mossa eseguita
        UndoRedo,       //Invio azione annulla o ripeti
        Win             //Fine partita
    }
    public enum NetState {
        none,
        waiting,
        playing
    }
    public enum AskType {
        Play,
        Undo,
        Redo,
        Draw,
        Win
    }
    public enum IgnoreType {
        tenMinutes,
        matchEnds,
        disconnect,
        gameClose
    }

    [Flags]
    public enum MoveType {
        Null = 0,
        OnlyMove = 1,
        OnlyAttack = 2,
        Normal = 4,
        PawnFirstMove = 8 | OnlyMove,
        Passant = 16 | OnlyMove,
        Promotion = 32,
        Castling = 64 | OnlyMove
    }
    public enum SerialType {
        Vertical,
        Horizontal,
        DiagonalUp,
        DiagonalDown
    }
    public enum SlabType {
        Null,
        Trait,
        Pawn,
        Rook,
        Knight,
        Bishop,
        Queen,
        King
    }
    public enum WinType {
        Null,
        White,
        Black,
        Stale,
        Draw,
        Strange,
        GiveUp,
        Disconnect
    }
    public enum KeyStateType {
        Released,
        Pressed,
        Repeat
    }
    public enum Timing {
        Infinite,
        Limit
    }
}
