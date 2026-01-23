using System.Collections.Generic;
using System.Diagnostics;
using TCPFurhatComm;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace OPEN.PandemicAI
{
    public partial class Furhat
    {
        [Header("Debug Gaze State Information (DO NOT EDIT!!!)")]
        [SerializeField] private IdleGazeState currentIdleState = IdleGazeState.LookAtUser;
        [SerializeField] private GazeState currentGazeState = GazeState.Idle;
        [SerializeField] private Vector3 furhatGazePosition = new Vector3(0, 0, 0);
        [SerializeField] private GazeAversionType currentAversionType;
        [SerializeField] private float currentGazeTimer;
        [SerializeField] private float currentGazePriority;

        [SerializeField] private TextMeshProUGUI gazeStateTextMeshPro;

        private User user = null;
        private Stopwatch userLastSeenSW = new();
        private UserGazingAt userGazingAt = UserGazingAt.Robot;
        private Stopwatch currentGazeSW = new();
        private GazeTargetType currentGazeTargetType = GazeTargetType.UserFace;
        private Vector3 lastSentGazePosition = Vector3.zero;
        private bool gazePositionChanged = false;
        private const float GAZE_POSITION_THRESHOLD = 0.05f;
        private bool gazeSpeechBoardRandom = false;
        private bool gazeSpeechUser = false;
        private bool pendingUserSpeakingCommand = false;
        private string gazeSpeechCity = null;
        private Vector3 animationGazePosition;
        private Vector3 cityGazePosition;

        private bool suppressIdle = false;

        [SerializeField] private int minGazeIntervalMs = 120;                
        [SerializeField] private int minGazeIntervalWhileSpeakingMs = 2000;
        private readonly Stopwatch gazeSendCooldown = new();
        private bool gazeSentThisFrame = false; 
        private bool forceGazeAtClick = false;
        private Vector2 lookAtScreenPositionClicked;

        public enum GazeTargetType
        {
            UserFace,       // Gaze is actively tracking the user's face
            BoardRandom,    // Gaze is directed at a random point on the board
            BoardClick,     // Gaze is directed at a specific clicked point on the board
            BoardAnimation, // Gaze is directed at an animated point on the board
            Aversion,       // Gaze is directed away (e.g., "look around")
            None            // No specific gaze target (or idle, handled by ChooseNextIdleState)
        }

        public void TickGaze()
        {
            if (gazeSpeechBoardRandom)
            {
                lookAtBoardRandom(GazeState.ExpressiveSpeech, gazeSettings.ScanBoardTR);
                currentGazeTargetType = GazeTargetType.BoardRandom;
                gazeSpeechBoardRandom = false;
            }
            else if (gazeSpeechCity != null)
            {
                lookAtCity(GazeState.ExpressiveSpeech, gazeSettings.BoardClickTR, gazeSpeechCity);
                gazeSpeechCity = null;
            }
            else if (gazeSpeechUser)
            {
                lookAtUser(GazeState.ExpressiveSpeech, gazeSettings.UserSpeechTR);
                currentGazeTargetType = GazeTargetType.UserFace;
                gazeSpeechUser = false;
            }
            else if (forceGazeAtClick)
            {
                Vector3 pos = getInterpolatedGaze(lookAtScreenPositionClicked);
                GazeUpdate(pos, GazeState.ExpressiveSpeech, gazeSettings.BoardClickTR);
                currentGazeTargetType = GazeTargetType.BoardClick;
                forceGazeAtClick = false;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                Vector2 screenPosition = Input.mousePosition;
                EDGazeFromBoardClick(screenPosition);
            }

            else if (currentGazeTargetType == GazeTargetType.BoardAnimation)
            {
                UpdateAnimationGaze();
            }
            else if (currentGazeTargetType == GazeTargetType.UserFace && user != null)
            {
                UpdateUserFaceGaze();
            }

            if (currentGazeTimer <= 0)
            {
                if (!isInMiddleOfSpeaking && !isMyTurn)
                {
                    ChooseNextIdleState();
                }
            }

            else if (currentGazeTargetType == GazeTargetType.UserFace &&
                userLastSeenSW.ElapsedMilliseconds > gazeSettings.UserLostTimeMS)
            {
                user = null;
                ChooseNextIdleState();
            }

            currentGazeTimer -= Time.deltaTime;

            SendGazeCommandIfNeeded();
        }

        private void UpdateAnimationGaze()
        {
            Vector3 newGazePosition = animationGazePosition;
            furhatGazePosition = newGazePosition;
            gazePositionChanged = true;
        }
        private void LateUpdate()
        {
            gazeSentThisFrame = false;
        }
        private bool IsGazeCoolingDown()
        {
            int requiredInterval = isInMiddleOfSpeaking ? minGazeIntervalWhileSpeakingMs : minGazeIntervalMs;
            return gazeSendCooldown.IsRunning && gazeSendCooldown.ElapsedMilliseconds < requiredInterval;
        }


        private void UpdateUserFaceGaze()
        {
            var newGazePosition = new Vector3(
                (float)user.location.x,
                (float)user.location.y,
                (float)user.location.z
            );

           
                furhatGazePosition = newGazePosition;
        }

        private void SendGazeCommandIfNeeded()
        {
            if (Vector3.Distance(lastSentGazePosition, furhatGazePosition) <= GAZE_POSITION_THRESHOLD)
                return;

            LogCommand("Gaze", $"{FormatGazeMode()}  {furhatGazePosition}");
            furhat.Gaze(furhatGazePosition.x, furhatGazePosition.y, furhatGazePosition.z);
            lastSentGazePosition = furhatGazePosition;
        }

        private void DebugGazeInformation()
        {
            gazeStateTextMeshPro.text = $"Current Gaze State: {currentGazeState}";
            gazeStateTextMeshPro.text += $"\nCurrent Gaze Target Type: {currentGazeTargetType}";
            gazeStateTextMeshPro.text += $"\nCurrent Gaze point {furhatGazePosition}";
            gazeStateTextMeshPro.text += $"\nLast Sent Gaze point {lastSentGazePosition}";
            gazeStateTextMeshPro.text += $"\nAnimation Gaze point {animationGazePosition}";
            if (currentGazeState == GazeState.JointAttention)
                gazeStateTextMeshPro.text += $"\nUser Gaze: {userGazingAt}";
            else if (currentGazeState == GazeState.Idle)
            {
                gazeStateTextMeshPro.text += $"\nCurrent Idle State: {currentIdleState}";
                if (currentIdleState == IdleGazeState.JointAttention)
                    gazeStateTextMeshPro.text += $"\nCurrent Idle State: {currentIdleState}";
            }
            gazeStateTextMeshPro.text += $"\nGaze Timer {currentGazeTimer}";
        }

        private void DebugTurnInformation()
        {
            gazeStateTextMeshPro.text = $"Current Playing State: {playingState}";
            gazeStateTextMeshPro.text += $"\nAI mode: {currentAiMode}";
            gazeStateTextMeshPro.text += $"\nIn middle of speaking: {isInMiddleOfSpeaking}";
            gazeStateTextMeshPro.text += $"\nIs my turn: {isMyTurn}";
            gazeStateTextMeshPro.text += $"\nLast announced action {_lastActionAnnounced}";
            gazeStateTextMeshPro.text += $"\nLast rec: {recognizedSpeech}";
            gazeStateTextMeshPro.text += $"\nState: {LLMRequestHandler.executing}";
        }

        public void UpdateUser(List<User> users)
        {
            if (users == null || users.Count == 0) return;

            user = users[0];
            userLastSeenSW.Restart();

            bool updated = false;
            (updated, userGazingAt) = UserGazeClassifier.UpdateState(user.location, user.rotation, gazeSettings, userGazingAt);
        }

        private void EDGazeExpressiveSpeech()
        {
            currentGazeState = GazeState.ExpressiveSpeech;
        }

        private void EDGazeExpressiveSpeechStop()
        {
            if (!isMyTurn || GameRoot.State.CurrentState != GameState.PlayerActions)
            {
                ChooseNextIdleState();
            }
        }

        private void EDGazeUserSpeech()
        {
            if (gazeProbabilityCheck(GazeState.UserSpeech, gazeSettings.ProbabilityUserSpeech))
            {
                lookAtUser(GazeState.UserSpeech, gazeSettings.UserSpeechTR);
            }
        }

        private void EDGazeBoardAnimation(Vector3 worldPosition)
        {
            if (IsGazeCoolingDown())
            {
                return;
            }

            if (gazeProbabilityCheck(GazeState.BoardAnimation, gazeSettings.ProbabilityBoardAnimation))
            {
                currentGazeTargetType = GazeTargetType.BoardAnimation;

                Vector3 screenPos3D = Camera.main.WorldToScreenPoint(worldPosition);
                Vector2 screenPos2D = new Vector2(screenPos3D.x, screenPos3D.y);
                Vector3 newAnimationGaze = getInterpolatedGaze(screenPos2D);
                
                if (Vector3.Distance(animationGazePosition, newAnimationGaze) > GAZE_POSITION_THRESHOLD)
                {
                    GazeUpdate(animationGazePosition, GazeState.BoardAnimation, gazeSettings.BoardAnimationTR);
                    animationGazePosition = newAnimationGaze;
                    gazePositionChanged = true;
                }
            }
        }


        private bool gazeProbabilityCheck(GazeState state, float probability)
        {
            return Random.value < probability && currentGazeState >= state;
        }

        private void EDGazeFromBoardClick(Vector2 screenPosition)
        {

            if (gazeProbabilityCheck(GazeState.BoardClick, gazeSettings.ProbabilityBoardClick))
            {
                currentGazeTargetType = GazeTargetType.BoardClick;
                var pos = getInterpolatedGaze(screenPosition);
                GazeUpdate(pos, GazeState.BoardClick, gazeSettings.BoardClickTR);
            }
        }

        public Vector3 getInterpolatedGaze(Vector2 screenPos)
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            float tx = 1.0f - (screenPos.x / screenWidth);
            float ty = 1.0f - (screenPos.y / screenHeight);

            float interpX = Mathf.Lerp(
                Mathf.Lerp(gazeSettings.CalibrationTargetBR.x, gazeSettings.CalibrationTargetTR.x, tx),
                Mathf.Lerp(gazeSettings.CalibrationTargetBL.x, gazeSettings.CalibrationTargetTL.x, tx),
                ty
            );

            float interpY = Mathf.Lerp(
                Mathf.Lerp(gazeSettings.CalibrationTargetBR.y, gazeSettings.CalibrationTargetTR.y, tx),
                Mathf.Lerp(gazeSettings.CalibrationTargetBL.y, gazeSettings.CalibrationTargetTL.y, tx),
                ty
            );

            float interpZ = Mathf.Lerp(
                Mathf.Lerp(gazeSettings.CalibrationTargetBR.z, gazeSettings.CalibrationTargetTR.z, tx),
                Mathf.Lerp(gazeSettings.CalibrationTargetBL.z, gazeSettings.CalibrationTargetTL.z, tx),
                ty
            );

            return new Vector3(interpX, interpY, interpZ);
        }

        void ChooseNextIdleState()
        {
            currentGazeState = GazeState.Idle;

            float rand = Random.value;
            if (rand < gazeSettings.WeightLookAtUser)
            {
                currentIdleState = IdleGazeState.LookAtUser;
                lookAtUser(GazeState.Idle, gazeSettings.LookAtUserTR);
            }
            else if (rand < gazeSettings.WeightScanBoard + gazeSettings.WeightLookAtUser)
            {
                currentIdleState = IdleGazeState.ScanBoard;
                lookAtBoardRandom(GazeState.Idle, gazeSettings.ScanBoardTR);
            }

            else
            {
                currentIdleState = IdleGazeState.LookAround;
                lookAround(GazeState.Idle, gazeSettings.LookAroundTR);
            }
        }

        private void lookAtBoardRandom(GazeState state, Vector2 timing)
        {
            currentGazeTargetType = GazeTargetType.BoardRandom;
            Vector2 screenPosition = new Vector2(Random.Range(0, Screen.width), Random.Range(0, Screen.height));
            Vector3 targetGaze = getInterpolatedGaze(screenPosition);
            GazeUpdate(targetGaze, state, timing);
        }

        private void lookAtCity(GazeState state, Vector2 timing, string cityName)
        {
            int cityID = GetEngineInfo.FindCityIndexByName(cityName);
            currentGazeTargetType = GazeTargetType.BoardRandom;
            RectTransform objectPosition = FurhatGameMap.Instance.GetItemPlacement(cityID.ToString());

            Vector2 worldCenter = objectPosition.TransformPoint(objectPosition.rect.center);
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, worldCenter);

            cityGazePosition = getInterpolatedGaze(screenPosition);
            GazeUpdate(cityGazePosition, state, timing);
        }

        private void lookAtUser(GazeState state, Vector2 timings)
        {
            currentGazeTargetType = GazeTargetType.UserFace;
            Vector3 lookAtUser;
            if (user != null)
                lookAtUser = new Vector3((float)user.location.x, (float)user.location.y, (float)user.location.z);
            else
                lookAtUser = new Vector3(0, 0, 1);
            GazeUpdate(lookAtUser, state, timings);
        }

        private void lookAround(GazeState state, Vector2 timings)
        {
            currentGazeTargetType = GazeTargetType.Aversion;
            float xr = Random.Range(gazeSettings.gazeAversionXmin, gazeSettings.gazeAversionXmax);
            float yr = Random.Range(gazeSettings.gazeAversionYmin, gazeSettings.gazeAversionYmax);
            float zr = Random.Range(gazeSettings.gazeAversionZmin, gazeSettings.gazeAversionZmax);

            bool lookLeft = false;

            lookLeft = Random.value < 0.5f;

            if (lookLeft) xr = -xr;

            GazeUpdate(new Vector3(xr, yr, zr), state, timings);
        }

        public void GazeUpdate(Vector3 gazeTo, GazeState gazeType, Vector2 timeRange)
        {
            furhatGazePosition = gazeTo;
            currentGazeState = gazeType;
            currentGazeTimer = Random.Range(timeRange.x, timeRange.y);
            currentGazeSW.Restart();
        }

        private string FormatGazeMode()
        {
            string state = currentGazeState == GazeState.Idle
                ? $"Idle/{currentIdleState}"
                : currentGazeState.ToString();

            return $"{state}|{currentGazeTargetType}";
        }


        public void GazeAtClick(Vector2 screenPosition)
        {
            lookAtScreenPositionClicked = screenPosition;
            forceGazeAtClick = true;
        }

    }
}