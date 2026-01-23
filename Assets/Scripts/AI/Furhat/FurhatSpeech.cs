using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using TCPFurhatComm;
using System.Diagnostics;
using Debug = UnityEngine.Debug;


namespace OPEN.PandemicAI
{
    public partial class Furhat
    {
        [Header("Debug Speech State Information (DO NOT EDIT!!!)")]
        [SerializeField] private bool isInMiddleOfSpeaking = false;

        Dictionary<string, string> variablesString = new();
        DialogActs dialogActs;
        private bool turnEnded = false;

        public void ProcessEvent(string type, string eventText)
        {
            Debug.Log($"Processing event: {type} with text: {eventText}");

            if (type == typeof(PNewTurn).Name ||
                type == typeof(EDrawInfectionCard).Name ||
                type == typeof(EEpidemicInitiate).Name ||
                type == typeof(EGameOver).Name ||
                type == typeof(EIncreaseInfectionRate).Name ||
                type == typeof(EIncreaseOutbreak).Name)
            {
                //GetEngineInfo.AddEngineEvent(type, eventText);
            }

            if (type == typeof(EInitializeFirstPlayer).Name)
            {
                WakeUpFurhat();
            }

            if (furhatAsleep) return;

            isMyTurn = GameRoot.State.CurrentPlayer.Role == AI_ROLE;

            if (CheckSpecialEvents(type, eventText)) return;

            //Debug.Log($"Checked for special event: {type} with text: {eventText}");

            if (!ShouldSpeak(dialogActs.GetEventRelevance(type))) return;
            Debug.Log($"Decided to go through with event: {type} with text: {eventText}");
            variablesString = Utility.convertVariablesToString(Utility.ParseEventText(eventText));
            variablesString["name"] = partner.Name.ToString();

            if (variablesString.TryGetValue("abort", out string abortValue) && bool.TryParse(abortValue, out bool shouldAbort) && shouldAbort)
            {
                return;
            }

            string conditionString = variablesString.ContainsKey("condition") ? variablesString["condition"] : "";
            var utteranceToSay = dialogActs.GetDialog(type, isMyTurn, conditionString);
            Debug.Log("decided to say: " + utteranceToSay);
            if (utteranceToSay == null)
            {
                Debug.Log($"No dialog mapping found for event type: {type}, condition: {conditionString}");
                return;
            }
            if (listening) StopListeningToUser();
            //Debug.Log($"Utterance to say: {utteranceToSay}");
            ExecuteDialogAct(utteranceToSay, variablesString);
        }

        /// <summary>
        /// Checks for and handles special event types that require unique logic.
        /// </summary>
        /// <returns>True if the event was handled and processing should stop, false otherwise.</returns>
        private bool CheckSpecialEvents(string type, string eventText)
        {
            if (type == typeof(EGameOver).Name)
                currentAiMode = AIMode.GameEnded;
            if (type == typeof(RIntentAcknowledge).Name ||
                type == typeof(RIntentDefer).Name ||
                type == typeof(RIntentEncouragement).Name ||
                type == typeof(RIntentHumor).Name ||
                type == typeof(RIntentSuggest).Name)
            { 
                if (currentAiMode == AIMode.Idle) return false;
                else return true;
            }
                if (type == typeof(PNewTurn).Name)
            {
                HandleNewTurnEvent();
                return false; // Continue processing
            }
            if (type == typeof(GCardStateChange).Name)
            {
                return HandleCardStateChangeEvent(eventText);
            }
            if (type == typeof(USpeechPartial).Name 
                || type == typeof(GActionButtonClicked).Name
                || type == typeof(USpeechEnded).Name)
            {
                return true;
            }else 
                return false;
        }

        /// <summary>
        /// Handles logic for the start of a new turn.
        /// </summary>
        private void HandleNewTurnEvent()
        {
            if (plan == null) return;

            if (isMyTurn)
            {
                plan.ActionQueue.Clear();
                turnEnded = false;
                playingState = PlayingState.None;
                //if (isInMiddleOfSpeaking)
                //{
                //    furhat.StopSpeaking();
                //    isInMiddleOfSpeaking = false;
                //}
            }
        }

        /// <summary>
        /// Handles the card state change event, specifically for forcing the AI to discard.
        /// </summary>
        /// <returns>True if the event was handled and processing should stop.</returns>
        private bool HandleCardStateChangeEvent(string eventText)
        {
            var dict = Utility.ParseEventText(eventText);
            if (!dict.TryGetValue("role", out var roleObj))
            {
                return false;
            }

            if ((string)roleObj == AI_ROLE.ToString())
            {
                playingState = PlayingState.Discarding;
                return true;
            }
            else
            {
                //Debug.Log("You are discarding card" + eventText);
                return false;
            }

            }

        /// <summary>
        /// Prepares Furhat to speak by setting flags, managing gaze, and stopping listening.
        /// </summary>
        private void BeginSpeaking()
        {
            isInMiddleOfSpeaking = true;
            timeSinceSpeechStarted.Restart();
            //EDGazeExpressiveSpeech();
        }

        public void ExecuteDialogAct(string utterance, Dictionary<string, string> vars = null)
        {
            BeginSpeaking();
            LogCommand("Say", utterance);
            furhat.Say(utterance, keyValuePairs: vars);
        }

        public bool ShouldSpeak(float relevance)
        {
            if (relevance >= 1.0f)
            {
                return true;
            }
            if (isInMiddleOfSpeaking || timeSinceSpeechEnded.Elapsed.TotalSeconds < 1)
            {
                return false;
            }
            // if there has been no speech in a while the relevance goes up.
            double factor = timeSinceSpeechEnded.Elapsed.TotalSeconds;
            relevance += (float)(factor / 10.0);

            return Random.value <= relevance;
        }

        /// <summary>
        /// Handles custom events sent from the Furhat skill.
        /// </summary>
        private void HandleCustomEvents(FurhatInterface furhat, string eventName, Dictionary<string, string> vars)
        {
            
            timeSinceHandledEvent.Restart();
            LogCommand("Event", eventName);
            switch (eventName)
            {
                case "clickActionButton":
                    HandleClickActionButton();
                    break;
                case "dragPawn":
                    FurhatGameExecutor.PendingDragTarget = plan.ActionQueue[0].TargetCity.ToString();
                    break;
                case "clickCard":
                    FurhatGameExecutor.PendingClickTarget = "card" + vars["chosenCard"];
                    break;
                case "clickAccept":
                    FurhatGameExecutor.PendingClickTarget = "accept";
                    break;
                case "clickCube":
                    FurhatGameExecutor.PendingClickTarget = $"cube{me.CurrentCity.CityCard.CityID}{plan.ActionQueue[0].TargetVirus}";
                    break;
                case "clickDiscard":
                    FurhatGameExecutor.PendingClickTarget = "discard";
                    break;
                case "lookAtUser":
                    gazeSpeechUser = true;
                    break;
                case "lookAtBoard":
                    gazeSpeechBoardRandom = true;
                    break;
                default:
                    if (eventName.StartsWith("lookAtCity"))
                    {
                        string cityName = eventName["lookAtCity".Length..];
                        
                        gazeSpeechCity = cityName;
                    }
                    else {Debug.LogWarning($"event content '{eventName}' did not match any defined option in HandleCustomEvents."); }
                        
                    break;
            }
        }

        /// <summary>
        /// Maps the current playing state to the corresponding action button name.
        /// </summary>
        private void HandleClickActionButton()
        {
            switch (playingState)
            {
                case PlayingState.Moving: FurhatGameExecutor.PendingClickTarget = "Move"; break;
                case PlayingState.Chartering: FurhatGameExecutor.PendingClickTarget = "CharterFlight"; break;
                case PlayingState.Flying: FurhatGameExecutor.PendingClickTarget = "DirectFlight"; break;
                case PlayingState.Treating: FurhatGameExecutor.PendingClickTarget = "Treat"; break;
                case PlayingState.Curing: FurhatGameExecutor.PendingClickTarget = "Cure"; break;
                case PlayingState.Sharing: FurhatGameExecutor.PendingClickTarget = "Share"; break;
                case PlayingState.EndingTurn: FurhatGameExecutor.PendingClickTarget = "EndTurn"; break;
            }
        }

    }
}