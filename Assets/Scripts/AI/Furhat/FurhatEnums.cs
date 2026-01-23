namespace OPEN.PandemicAI
{
    public enum PlayingState { None, Moving, Chartering, Flying, Treating, Curing, EndingTurn, Sharing, Discarding,
        MovingDragPawn,
        CharteringDragPawn,
        SharingAccept,
        FlyingClickCard,
        FlyingAccept,
        TreatingClickCube,
        TreatingClickCubeAgain,
        TreatingClickCubeFinal,
        CuringSelectCard,
        CuringSelectCardOne,
        CuringSelectCardTwo,
        CuringSelectCardThree,
        CuringSelectCardFinal,
        DiscardingClickCard,
        DiscardingAccept,
        CuringAccept
    }
    public enum GazeState { ExpressiveSpeech = 1, UserSpeech = 2, BoardClick = 3, BoardAnimation = 4, JointAttention = 5, Idle = 6}
    public enum IdleGazeState { LookAtUser, ScanBoard, JointAttention, LookAround}
    public enum GazeAversionType { Side, Up }
    public enum UserGazingAt { Robot, Board, ElsewhereLeft, ElseWhereRight }
}