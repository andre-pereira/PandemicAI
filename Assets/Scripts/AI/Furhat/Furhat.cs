using UnityEngine;
using TCPFurhatComm;
using System.Collections.Generic;
using System;
using TMPro;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using static OPEN.PandemicAI.Enums;
using System.IO;
using VadDotNet;

namespace OPEN.PandemicAI
{
    public partial class Furhat : MonoBehaviour
    {
        public const int FURHATPLAYERPOSITION = 3;
        public const int USERPLAYERPOSITION = 1;

        public float timeSinceGameStarted = float.PositiveInfinity;
        public TextMeshProUGUI textbox;
        public LLMRequestHandler aiHandler;
        public QuitGame quitGame;

        private const Player.Roles AI_ROLE = Player.Roles.QuarantineSpecialist;
        FurhatInterface furhat;
        public string IPAdress;
        public bool furhatAsleep = true;
        public LaserPointerScript laserPointerScript;

        [Header("Config")]
        [SerializeField] private GazeSettings gazeSettings;
        [SerializeField] public FurhatConfig furhatConfig;

        public enum AIMode
        {
            Idle,
            GeneratingPlan,
            AskingForFeedback,
            ExecutingPlan,
            GameEnded
        }
        public AIMode currentAiMode = AIMode.Idle;

        private readonly Stopwatch timeSinceSpeechEnded = new Stopwatch();
        private readonly Stopwatch timeSinceSpeechStarted = new Stopwatch();

        private readonly Stopwatch timeSincePlanDisclosureStart = new Stopwatch();

        public Stopwatch timeSinceHandledEvent = new Stopwatch();

        private bool speechActFinished = false;
        public bool listenToFeedback = false;
        private bool microphoneIsOn = false;
        private string recognizedSpeech;
        private bool userIsSpeaking;
        private Stopwatch timeOfUserSilence = new Stopwatch();
        private long USERSILENCECUTOFF = 10000;
        private Plan plan;
        private Player me;
        private Player partner;
        private bool isMyTurn;
        private PandemicAI ai = new();
        [SerializeField] private PlayingState playingState = PlayingState.None;
        private bool dragging = false;
        private int cardToDiscard;
        private ActionType _lastActionAnnounced = ActionType.None;
        private string logFilePath;
        Stopwatch userStoppedSpeakingStopwatch = new();
        public static bool listening = false;
        public int DialogActStartingUtterance;

        public GameStartManager gameStartManager;
        public SileroVadRunner vad;
        bool isInitialized = false;

        #region Singleton
        private static Furhat instance;


        public static Furhat Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<Furhat>();
                }
                return instance;
            }
        }
        #endregion

        void Start()
        {
            // Check if we need to wait for an API key.
            if  (GameRoot.Config.UseFurhat && (gameStartManager == null || !gameStartManager.gameObject.activeInHierarchy))
            {
                Debug.LogError("Furhat script requires a GameStartManager in the scene to handle API keys.");
                // If not, initialize Furhat immediately.
                InitializeFurhat();
            }
            else
            {
                // Otherwise, wait for the GameStartManager's "ready" signal.
                Debug.Log("Furhat is waiting for a valid API key...");
                GameStartManager.OnApiKeyReady += InitializeFurhat;
            }
            string ts = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string logsDir = Path.Combine(Application.dataPath, "Logs");
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }
            logFilePath = Path.Combine(logsDir, $"furhat_log_{ts}.txt");
            DialogActStartingUtterance = furhatConfig.DialogActStartingUtterance;
        }

        // IMPORTANT: Always unsubscribe from the event when the object is destroyed.
        private void OnDestroy()
        {
            // Make sure the gameStartManager reference exists before trying to unsubscribe
            if (gameStartManager != null)
            {
                GameStartManager.OnApiKeyReady -= InitializeFurhat;
            }
        }

        private void InitializeFurhat()
        {
            Debug.Log("API Key ready or not required. Initializing Furhat systems...");

            // This is your original setup code
            dialogActs = new DialogActs();
            timeSinceGameStarted = Time.fixedTime;
            string furPath = Application.streamingAssetsPath;
            dialogActs.LoadMappings(Path.Combine(furPath, furhatConfig.JSONFileName));

            SetupFurhatInterface();
            FurhatGameMap.Instance.SetCityPositions();

            isInitialized = true;
            timeSinceHandledEvent.Start();
            timeSinceSpeechStarted.Start();
        }

        void Update()
        {
            if (!isInitialized) return;

            DebugTurnInformation();

            if (userIsSpeaking)
                return;

            if (listening & !microphoneIsOn)
            {
                LogCommand("StartMicrophone");
                microphoneIsOn = true;
                vad.StartMicrophone();
                ChangeLed(200, 200, 200);
            }
            else if (!listening & microphoneIsOn)
            {
                LogCommand("StopMicrophone");
                microphoneIsOn = false;
                vad.StopMicrophone();
                ChangeLed(0, 0, 0);
            }
            if ((GameRoot.State.CurrentState != GameState.PlayerActions) && listening) { StopListeningToUser(); }

            if (timeOfUserSilence.ElapsedMilliseconds > USERSILENCECUTOFF)
            {
                UserSilenceHandling();
            }

            TickGaze();
            isMyTurn = GameRoot.State.CurrentPlayer.Role == AI_ROLE;

            if (Timeline.Instance.HasPendingEvent || isInMiddleOfSpeaking || turnEnded || timeSinceHandledEvent.Elapsed.TotalSeconds < furhatConfig.handleEventTime) return;

            CleanDraggingStateIfDone();

            HandleSpeechEnded();

            if (playingState != PlayingState.None) return;
            if (currentAiMode == AIMode.GameEnded) quitGame.Quit();
            
            if (isMyTurn)
            {
                HandleAITurnLogic();
            }
            else
            {
                // Ensure mode is Idle if it's not our turn
                currentAiMode = AIMode.Idle;
            }
        }

        private void UserSilenceHandling()
        {
            if (aiHandler.LLMMode != LLMRequestHandler.GenerationMode.free)
            {
                if (listenToFeedback)
                    listenToFeedback = false;
                else
                    Timeline.Instance.AddEvent(new RIntentHumor(partner.CurrentCity.CityCard.CityID));
            }
            else {
                if (LLMRequestHandler.executing != LLMRequestHandler.LLMState.Negotiating)
                    Timeline.Instance.AddEvent(new RIntentHumor(partner.CurrentCity.CityCard.CityID));
                else {
                    LLMRequestHandler.executing = LLMRequestHandler.LLMState.Executing;
                }
        }
        }

        /// <summary>
        /// Sets up the Furhat interface, subscribes to events, and puts Furhat to sleep.
        /// </summary>
        private void SetupFurhatInterface()
        {
            furhatAsleep = true;
            if (furhat == null)
            {
                furhat = new FurhatInterface(IPAdress, nameForSkill: "Pandemic AI");
            }
            furhat.EnableMicroexpressions(false);
            furhat.Gesture(GESTURES.EYES_CLOSE);

            furhat.SensedUsersAction = new Action<List<User>>((users) => UpdateUser(users));
            furhat.EndSpeechAction = new Action(() => SpeechEnded());
            furhat.CustomEvent = new Action<string>((s) => HandleCustomEvents(furhat, s, variablesString));
            //furhat.RecognizedSpeechAction = new Action<string>((s) => speechRecognized(s));
            //furhat.RecognizedPartialSpeechAction = new Action<string>((s) => partialSpeechRecognized(s));
        }

        /// <summary>
        /// Handles logic that should run after a speech act, or after a period of silence.
        /// </summary>
        private void HandleSpeechEnded()
        {
            if (speechActFinished && timeSinceSpeechEnded.Elapsed.TotalSeconds > furhatConfig.reactiveCommentDelay)
            {
                speechActFinished = false; // Consume the flag immediately.
                ManagePlayingState();
            }
        }

        /// <summary>
        /// Handles the main logic for the AI's turn.
        /// </summary>
        private void HandleAITurnLogic()
        {
            if (GameRoot.State.CurrentState == GameState.Discarding)
                return;

            if (me.ActionsRemaining == 0 || (me.ActionsRemaining > 0 && plan != null && plan.ActionQueue.Count == 0))
            {
                if (aiHandler.LLMMode == LLMRequestHandler.GenerationMode.free && LLMRequestHandler.executing == LLMRequestHandler.LLMState.Executing)
                {
                    LLMRequestHandler.executing = LLMRequestHandler.LLMState.Responding;
                    return;
                }
            }

            if (currentAiMode == AIMode.GeneratingPlan && aiHandler.LLMMode != LLMRequestHandler.GenerationMode.free)
                if (timeSincePlanDisclosureStart.Elapsed.TotalSeconds < furhatConfig.planningPhase)
                {
                    return;
                }
                //else currentAiMode = AIMode.AskingForFeedback;

            if (plan != null && plan.ActionQueue.Count > 0 && aiHandler.LLMMode != LLMRequestHandler.GenerationMode.free && !listenToFeedback)
            {
                currentAiMode = AIMode.ExecutingPlan;
                ExecuteNextActionInPlan();
            }
            
            else if (me.ActionsRemaining > 0 && aiHandler.LLMMode != LLMRequestHandler.GenerationMode.free && !listenToFeedback)
            {
                GenerateAndAnnounceNewPlan();
            }

            else if (plan != null && plan.ActionQueue.Count > 0 && aiHandler.LLMMode == LLMRequestHandler.GenerationMode.free && LLMRequestHandler.executing == LLMRequestHandler.LLMState.Executing)
            {
                currentAiMode = AIMode.ExecutingPlan;
                ExecuteNextActionInPlan();
            }
            else if (me.ActionsRemaining > 0 && aiHandler.LLMMode == LLMRequestHandler.GenerationMode.free && LLMRequestHandler.executing == LLMRequestHandler.LLMState.Responding)
            {
                plan = ai.PlanMove(me, partner);
                LLMRequestHandler.executing = LLMRequestHandler.LLMState.Idle;
                aiHandler.ProcessSpeech("Your turn now.");
            }
            //else if (me.ActionsRemaining > 0 && aiHandler.LLMMode == LLMRequestHandler.GenerationMode.free && LLMRequestHandler.executing == LLMRequestHandler.LLMState.Idle)
            //{
            //aiHandler.ProcessSpeech("Your turn now.");
            //LLMRequestHandler.executing = LLMRequestHandler.LLMState.Negotiating;
            //}
        }

        /// <summary>
        /// Executes the next action based on the current plan.
        /// </summary>
        private void ExecuteNextActionInPlan()
        {
            // LogCommand("ExecuteNextActionInPlan", $"Current AI mode: {currentAiMode}, Plan: {plan?.ToString() ?? "null"}");
            PlayerAction nextAction = plan.ActionQueue[0];
            LogCommand("Execute Next Action in Plan", $"{plan.PlanPriority} Action : {nextAction}");
            // LogCommand("This is the plan", "${plan.PlanPriority} for {plan.TargetCity} with color {plan.TargetColor} and explanation: {plan.PlanExplanation}");
            if (nextAction.Type == ActionType.Treat && _lastActionAnnounced == ActionType.Treat)
            {
                AnnounceConsecutiveTreat();
                return; // Skip the main switch statement
            }

            // Record the type of action we are about to announce
            _lastActionAnnounced = nextAction.Type;

            switch (nextAction.Type)
            {
                case ActionType.EndTurn:
                    TransitionToState(PlayingState.EndingTurn, new RPlayEndTurn());
                    break;
                case ActionType.Move:
                    ExecuteMoveAction();
                    break;
                case ActionType.FindCure:
                    TransitionToState(PlayingState.Curing, new RCurePlay(me, plan));
                    break;
                case ActionType.Share:
                    TransitionToState(PlayingState.Sharing, new RSharePlay(me, plan));
                    break;
                case ActionType.Treat:
                    TransitionToState(PlayingState.Treating, new RTreatPlay(me, plan));
                    break;
                case ActionType.Charter:
                    TransitionToState(PlayingState.Chartering, new RCharterPlay(me, plan));
                    break;
                case ActionType.Fly:
                    TransitionToState(PlayingState.Flying, new RFlyPlay(me, plan));
                    break;
            }
        }

        /// <summary>
        /// Generates a new plan for the AI and discloses it.
        /// </summary>
        private void GenerateAndAnnounceNewPlan()
        {
            currentAiMode = AIMode.GeneratingPlan;
            plan = ai.PlanMove(me, partner);

            if (plan.ActionQueue.Count == 1 &&
            plan.ActionQueue[0].Type == ActionType.Treat &&
            _lastActionAnnounced == ActionType.Treat)
            {
                ExecuteNextActionInPlan();
            }
            else
            {
            Timeline.Instance.AddEvent(new RPlanDisclosure(me, plan, partner));
            timeSincePlanDisclosureStart.Restart();
            //_lastActionAnnounced = ActionType.None;
            listenToFeedback = true;
            }
            LogCommand("GenerateAndAnnounceNewPlan", $"Generated plan: {plan?.ToString() ?? "null"}");
        }

        /// <summary>
        /// Handles the specific logic for a move action, including merging multiple moves.
        /// </summary>
        private void ExecuteMoveAction()
        {
            int mergedActions = 0;
            int nextTargetCity = plan.ActionQueue[0].TargetCity;

            // Merge consecutive move actions
            for (int i = 1; i < plan.ActionQueue.Count; i++)
            {
                if (plan.ActionQueue[i].Type == ActionType.Move)
                {
                    mergedActions++;
                    nextTargetCity = plan.ActionQueue[i].TargetCity;
                }
                else break;
            }

            for (int i = 0; i < mergedActions; i++) plan.ActionQueue.RemoveAt(0);

            TransitionToState(PlayingState.Moving, new RMovePlay(me, nextTargetCity));
        }

        /// <summary>
        /// Manages the state machine for executing multi-step actions after speech.
        /// </summary>
        private void ManagePlayingState()
        {
            switch (playingState)
            {
                // Move & Charter Actions
                case PlayingState.Moving:
                    dragging = true;
                    TransitionToState(PlayingState.MovingDragPawn, new RMoveDragPawn(me, plan.ActionQueue[0].TargetCity));
                    break;
                case PlayingState.Chartering:
                    dragging = true;
                    TransitionToState(PlayingState.CharteringDragPawn, new RCharterDragPawn(me, plan));
                    break;

                // Finalize Drag Actions
                //case PlayingState.MovingDragPawn:
                //case PlayingState.CharteringDragPawn:
                //    FinalizeAction();
                //    break;

                // Fly Action States
                case PlayingState.Flying:
                    TransitionToState(PlayingState.FlyingClickCard, new RFlySelectDestination(me, plan));
                    break;
                case PlayingState.FlyingClickCard:
                    TransitionToState(PlayingState.FlyingAccept, new RFlyAccept(me, plan));
                    break;
                case PlayingState.FlyingAccept:
                    FinalizeAction();
                    break;

                // Treat Action States
                case PlayingState.Treating:
                    TransitionToState(PlayingState.TreatingClickCube, new RTreatClickCube(me, plan));
                    break;
                case PlayingState.TreatingClickCube:
                case PlayingState.TreatingClickCubeAgain:
                case PlayingState.TreatingClickCubeFinal:
                    plan.ActionQueue.RemoveAt(0);
                    playingState = PlayingState.None;
                    break;

                // Cure Action States
                case PlayingState.Curing:
                    TransitionToState(PlayingState.CuringSelectCardOne, new RCureSelectCardOne(me, plan));
                    break;
                case PlayingState.CuringSelectCardOne:
                    TransitionToState(PlayingState.CuringSelectCardTwo, new RCureSelectCardTwo(me, plan));
                    break;
                case PlayingState.CuringSelectCardTwo:
                    TransitionToState(PlayingState.CuringSelectCardThree, new RCureSelectCardThree(me, plan));
                    break;
                case PlayingState.CuringSelectCardThree:
                    TransitionToState(PlayingState.CuringSelectCardFinal, new RCureSelectCardFinal(me, plan));
                    break;
                case PlayingState.CuringSelectCardFinal:
                    TransitionToState(PlayingState.CuringAccept, new RCureAccept(me, plan));
                    break;
                case PlayingState.CuringAccept:
                    FinalizeAction();
                    break;

                // Discard Action States
                case PlayingState.Discarding:
                    cardToDiscard = ai.SelectCardToDiscard(me, partner);
                    TransitionToState(PlayingState.DiscardingClickCard, new RDiscardClickCard(cardToDiscard));
                    break;
                case PlayingState.DiscardingClickCard:
                    TransitionToState(PlayingState.DiscardingAccept, new RDiscardAccept(cardToDiscard));
                    break;
                case PlayingState.DiscardingAccept:
                    break;

                // Share Action States
                case PlayingState.Sharing:
                    TransitionToState(PlayingState.SharingAccept, new RShareAccept(me, plan));
                    break;
                case PlayingState.SharingAccept:
                    FinalizeAction();
                    break;

                // End Turn
                case PlayingState.EndingTurn:
                    FinalizeAction();
                    turnEnded = true;
                    break;

                case PlayingState.None:
                    break;
            }
        }
        /// <summary>
        /// Helper to transition to a new state and add an event to the timeline.
        /// </summary>
        private void TransitionToState(PlayingState newState, TimelineEvent timelineEvent)
        {
            LogCommand("TransitionToState", $"{newState} with event {timelineEvent.GetType().Name}");
            playingState = newState;
            Timeline.Instance.AddEvent(timelineEvent);
        }
        /// <summary>
        /// Finalizes an action by removing it from the queue and resetting the state.
        /// </summary>
        private void FinalizeAction()
        {
            plan.ActionQueue.RemoveAt(0);
            playingState = PlayingState.None;
            if (me.ActionsRemaining == 0)
                _lastActionAnnounced = ActionType.None;
        }
        /// <summary>
        /// Activates Furhat's listening mode.
        /// </summary>
        private void StartListeningToUser()
        {
            if (GameRoot.Config.SpeechRecActivated)
            {
                timeOfUserSilence.Start();
                listening = true;
            }
        }
        /// <summary>
        /// Bypasses the initial "Treating" state for consecutive treat actions.
        /// </summary>
        private void AnnounceConsecutiveTreat()
        {
            bool isLastTreat = (plan.ActionQueue.Count == 1) || (plan.ActionQueue[1].Type != ActionType.Treat);
            PlayingState nextState = isLastTreat ? PlayingState.TreatingClickCubeFinal : PlayingState.TreatingClickCubeAgain;
            TimelineEvent nextEvent = isLastTreat ? new RTreatClickCubeFinal(me, plan) : (TimelineEvent)new RTreatClickCubeAgain(me, plan);
            TransitionToState(nextState, nextEvent);
        }
        private void SpeechEnded()
        {
            LogCommand("SpeechEnded");
            isInMiddleOfSpeaking = false;
            speechActFinished = true;
            timeSinceSpeechEnded.Restart();
            timeSinceSpeechStarted.Stop();

            if (!isMyTurn && GameRoot.State.CurrentState == GameState.PlayerActions && GameRoot.State.CurrentPlayer.ActionsRemaining > 0)
            {
                StartListeningToUser();
            }
            else if (listenToFeedback || isMyTurn && aiHandler.LLMMode == LLMRequestHandler.GenerationMode.free && LLMRequestHandler.executing == LLMRequestHandler.LLMState.Negotiating)
            {
                StartListeningToUser();
            }
            if (currentGazeState == GazeState.ExpressiveSpeech) 
            { 
                EDGazeExpressiveSpeechStop(); 
            }
            
        }
        private void StopListeningToUser()
        {
            LogCommand("StopListening");
            timeOfUserSilence.Reset();
            listening = false;
        }
        public void partialSpeechRecognized()
        {
            LogCommand("PartialSpeechRecognized");
            userIsSpeaking = true;
            timeOfUserSilence.Reset();
            EDGazeUserSpeech();
        }
        public void speechRecognized(string s)
        {
            recognizedSpeech = s;
            LogCommand("SpeechRecognized", s);
            userStoppedSpeakingStopwatch.Restart();
            userIsSpeaking = false;
            StopListeningToUser();
            aiHandler.ProcessSpeech(s);
        }
        private void ChangeLed(int v1, int v2, int v3)
        {
            LogCommand("ChangeLed", $"{v1},{v2},{v3}");
            furhat.ChangeLed(v1, v2, v3);
            
        }
        private void CleanDraggingStateIfDone()
        {
            if (playingState == PlayingState.MovingDragPawn || playingState == PlayingState.CharteringDragPawn)
            {
                if (!dragging)
                {
                    FinalizeAction();
                }
            }
        }
        public void WakeUpFurhat()
        {
            if (furhat != null)
            {
                furhatAsleep = false;
                furhat.EnableMicroexpressions(true);
                furhat.Gesture(GESTURES.EYES_OPEN);
                partner = GameRoot.State.Players[0];
                me = GameRoot.State.Players[1];
            }
        }
        public void ReactToIntent(int i)
        {
            if (listenToFeedback)
            {
                listenToFeedback = false;
                switch (i)
                {
                    case 0: Debug.Log("Original plan"); break;
                    case 1: { 
                            Debug.Log("Share Knowledge");
                            plan = ai.PlanMove(me, partner, Plan.PlanPriorities.ShareKnowledge);
                            playingState = PlayingState.None;
                            Timeline.Instance.AddEvent(new RChangingPlan(plan));
                            break;
                        }
                    case 2: 
                        {
                            Debug.Log("Find cure");
                            plan = ai.PlanMove(me, partner, Plan.PlanPriorities.FindCure);
                            playingState = PlayingState.None;
                            Timeline.Instance.AddEvent(new RChangingPlan(plan));
                            break;
                        }
                    case 3: 
                        {
                            Debug.Log("Safeguard cube supply");
                            plan = ai.PlanMove(me, partner, Plan.PlanPriorities.SafeguardCubeSupply);
                            playingState = PlayingState.None;
                            Timeline.Instance.AddEvent(new RChangingPlan(plan));
                            break;
                        }
                    case 4: 
                        {
                            Debug.Log("Safeguard ourbreak");
                            plan = ai.PlanMove(me, partner, Plan.PlanPriorities.SafeguardOutbreak);
                            playingState = PlayingState.None;
                            Timeline.Instance.AddEvent(new RChangingPlan(plan)); 
                            break;
                        }
                    case 5: 
                        {
                            Debug.Log("Manage disease");
                            plan = ai.PlanMove(me, partner, Plan.PlanPriorities.ManagingDisease);
                            playingState = PlayingState.None;
                            Timeline.Instance.AddEvent(new RChangingPlan(plan));
                            break;
                        }
                }
                
            }
            else
            {
                switch (i)
                {
                    case 0: Timeline.Instance.AddEvent(new RIntentAcknowledge()); break;
                    case 1: Timeline.Instance.AddEvent(new RIntentDefer()); break;
                    case 2: Timeline.Instance.AddEvent(new RIntentSuggest(partner, ai.PlanMove(partner, me), me)); break;
                    case 3: Timeline.Instance.AddEvent(new RIntentHumor(partner.CurrentCity.CityCard.CityID)); break;
                    case 4: Timeline.Instance.AddEvent(new RIntentEncouragement()); break;
                }
            }
        }

        public void NotifyObjectOnScreenMoved(Transform transform)
        {
            if (!isInMiddleOfSpeaking)
            {
                EDGazeBoardAnimation(transform.position);
            }

            if (laserPointerScript != null)
            {
                laserPointerScript.MoveToPosition(transform.position);
            }
        }

        public void DragEnded() => dragging = false;

        private void OnApplicationQuit()
        {
            if (furhat != null)
            {
                furhat?.StopListening();
                furhat?.CloseConnection();
            }
        }

        public bool OverwritePlan(Plan newPlan)
        {
            if (newPlan == null || newPlan.ActionQueue.Count == 0) return false;

            plan = newPlan;
            currentAiMode = AIMode.ExecutingPlan;
            playingState = PlayingState.None;

            timeSincePlanDisclosureStart.Restart();

            return true;
        }

        private readonly object logLock = new object();

        public void LogCommand(string command, string details = "")
        {
            lock (logLock)
            {
                string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string line = $"[{ts}]\t{command}\t{details}";
                File.AppendAllText(logFilePath, line + Environment.NewLine);
            }
        }
    }
}