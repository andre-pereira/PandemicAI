using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace OPEN.PandemicAI
{
    /// <summary>
    /// Manages interactions with a Large Language Model (LLM) for chat completions.
    /// </summary>
    public class LLMRequestHandler : MonoBehaviour
    {
        public enum LLMModel { Ministral_8b, Gemma3_1b, Gemma3_4b, Gemma3_12b, Gemma3_27b, Gemma_3n_4b_free, GPT_41_Nano_paid, GPT_41_Mini_paid, GPT_41_paid, Gemini_25_Flash_paid }
        public enum GenerationMode { free, intent }

        [Header("API Configuration")]

        [Header("Model Configuration")]
        public LLMModel llmModel = LLMModel.Gemma3_12b;
        public GenerationMode LLMMode;
        [Range(0f, 2f)] public float LLMTemperature = 0.7f;
        public int LLMMaxTokens = 64;

        [Header("System Prompts")]
        [TextArea(3, 10)] public string FreeResponseLLMPromptUserTurn;
        [TextArea(3, 10)] public string FreeResponseLLMPromptAITurn;
        [TextArea(3, 10)] public string IntentLLMPrompt;
        [TextArea(3, 10)] public string IntentLLMPromptFeedback;

        [Header("UI Elements")]
        public TextMeshProUGUI aiResponseText;

        [Header("History Settings")]
        [Tooltip("The maximum number of messages to keep in the conversation history. Older messages will be removed.")]
        public int maxHistoryItems = 10;
        public enum LLMState { Responding, Negotiating, Executing, Idle }
        public static LLMState executing = LLMState.Responding;

        // Queue for processing chat requests sequentially.
        private readonly ConcurrentQueue<string> _requestQueue = new ConcurrentQueue<string>();
        // Stores the chat conversation history.
        private readonly JArray _messagesHistory = new JArray();
        // Stores temporary context messages for a single request.
        private readonly JArray _temporaryContextMessages = new JArray();
        private System.Collections.Generic.List<int> cityEntities = new System.Collections.Generic.List<int>();
        public static bool cityGlows = false;
        private GameStartManager.Backend backend;

        // 1. Create a public variable for the reference.
        public GameStartManager gameStartManager;


        // These would be your private key variables
        private string _openRouterKey;
        private string _openAIKey;
        bool keyInitialized = false;
        private PandemicAI ai = new();

        public void InitializeKeys()
        {
            if (gameStartManager == null)
            {
                Debug.LogError("GameStartManager reference is not set!");
                return;
            }

            // 2. Use the instance variable (gameStartManager) to access the properties.
            backend = gameStartManager.selectedBackend;
            Debug.Log($"Selected Backend: {backend}");

            if (backend == GameStartManager.Backend.OpenRouter)
            {
                // Use the public property we created earlier
                _openRouterKey = gameStartManager.GetApiKey();
                Debug.Log("OpenRouter Key Set!");
            }
            else if (backend == GameStartManager.Backend.OpenAI)
            {
                // Use the same property for the OpenAI key
                _openAIKey = gameStartManager.GetApiKey();
                Debug.Log("OpenAI Key Set!");
            }
            keyInitialized = true;
            Debug.Log($"OpenAI key-{_openAIKey}");
        }

        private void Start()
        {
            if (gameStartManager == null || !gameStartManager.gameObject.activeInHierarchy)
            {
                Debug.Log("GameStartManager not required or is disabled. Starting game immediately.");
                InitializeKeys();
            }
            else
            {
                // If the GameStartManager is active and needs a key, wait for its signal.
                Debug.Log("Waiting for a valid API key before initializing the game...");
                GameStartManager.OnApiKeyReady += InitializeKeys;
            }
        }

        // 3. The Update loop now becomes the "worker" that starts the ENTIRE process.
        private void Update()
        {
            // Ensure keys are initialized before processing requests
            if (!keyInitialized)
            {
                return;
            }
            // If there is a request in the queue, process it.
            if (_requestQueue.TryDequeue(out var textToProcess))
            {
                // Start the NEW all-in-one coroutine.
                Debug.Log($"[LLMRequestHandler] Processing queued text: {textToProcess}");
                StartCoroutine(HandleSingleUtteranceCoroutine(textToProcess));
            }
        }

        #region Public API Methods

        // --- Replace your existing ProcessSpeech method with this one ---

        /// <summary>
        /// Processes user input by initiating a full pipeline of NER and LLM processing.
        /// </summary>
        /// <param name="inputText">The text from the user.</param>
        // 2. The new, simplified public method. It just adds the text to the queue.
        public void ProcessSpeech(string inputText)
        {
            if (GameRoot.State.CurrentState != GameState.PlayerActions)
            {
                //Debug.Log("[LLMRequestHandler] Game is not in PlayerActions state. Ignoring input.");
                return;
            }
            if (string.IsNullOrWhiteSpace(inputText))
            {
                inputText = "(User is silent)";
                if (LLMMode == GenerationMode.intent && !Furhat.Instance.listenToFeedback)
                {
                    return;
                }
            }
            Timeline.Instance.AddEvent(new USpeechEnded(inputText));

            // Add user message to history immediately for UI.
            AddUserMessage(inputText);

            Debug.Log($"[LLMRequestHandler] Queuing user input: {inputText}");
            _requestQueue.Enqueue(inputText);
        }


        // 4. The NEW All-in-One Coroutine that handles everything sequentially.
        private IEnumerator HandleSingleUtteranceCoroutine(string text)
        {
            cityEntities.Clear(); // Clear any previous city entities
            Debug.Log($"[Pipeline] Starting full processing for: {text}");
            _temporaryContextMessages.Clear(); // Clear any previous temporary messages

            if (LLMMode == GenerationMode.free)
            {
                int playerID = GetEngineInfo.LLMTurn() ? 1 : 0;
                string playerName = GetEngineInfo.LLMTurn() ? GameRoot.State.Players[1].Name : GameRoot.State.Players[0].Name;

                AddTemporaryAssistantMessage("[Tool Response] Current game state: " + GetEngineInfo.GetGameInfo());
                AddTemporaryAssistantMessage($"[Tool Response] {playerName} player information: " + GetEngineInfo.GetPlayerInfo(playerID));
                AddTemporaryAssistantMessage($"[Tool Response] {playerName} player current city: " + GetEngineInfo.GetCityInfo(GameRoot.State.Players[playerID].CurrentCity.CityCard.CityName));
                AddTemporaryAssistantMessage($"[Tool Response] {playerName} player info: " + GetEngineInfo.GetPlayerInfo(1 - playerID));
                AddTemporaryAssistantMessage($"[Tool Response] Recommended plan for {playerName} player: " + GetEngineInfo.GetPlan(playerID));
                AddTemporaryAssistantMessage($"[Tool Response] Valid moves for {playerName} player current turn: " + GetEngineInfo.GetValidActions(playerID));
                cityEntities = CheckCityIDReferences(text);
                cityEntities.AddRange(CheckCityIDReferences(GetEngineInfo.GetPlan(playerID)));

                if (cityEntities.Count > 0)
                {
                    foreach (var cityEntity in cityEntities.Distinct())
                    {
                        // Debug
                        Debug.Log($"[Pipeline] Found city entity: {cityEntity} in text: {text}");

                        // Append GetEngineInfo.GetCityInfo and ShortestPathFly to messages history
                        if (cityEntity != -1)
                        {
                            AddTemporaryAssistantMessage($"[Tool Response] {CityDrawer.CityScripts[cityEntity].CityCard.CityName} city info: " + GetEngineInfo.GetCityInfo(CityDrawer.CityScripts[cityEntity].CityCard.CityName));
                            AddTemporaryAssistantMessage($"[Tool Response] Shortest path to {CityDrawer.CityScripts[cityEntity].CityCard.CityName}: " + GetEngineInfo.GetShortestPathFly(playerID, CityDrawer.CityScripts[cityEntity].CityCard.CityName));

                        }
                        else
                        {
                            // Add message indicating city not found
                            AddTemporaryAssistantMessage($"[Tool Response] City '{CityDrawer.CityScripts[cityEntity].CityCard.CityName}' not found in the game catalog. Ask the player to repeat the city name");
                        }

                    }
                    TurnOnCityGlows(cityEntities);
                    cityGlows = true;
                }
            }
            ;


            // We need the most recent history for the call
            var historySnapshot = new JArray(_messagesHistory);
            // Add temporary context messages to the request snapshot
            foreach (var msg in _temporaryContextMessages)
            {
                historySnapshot.Add(msg);
            }

            // Build the request using the results from Part 1
            string systemPrompt;
            if (LLMMode == GenerationMode.intent)
            {
                if (Furhat.Instance.listenToFeedback)
                    systemPrompt = IntentLLMPromptFeedback;
                else
                    systemPrompt = IntentLLMPrompt;
            }
            else
            {
                // Distinguish between LLM turn and user turn
                systemPrompt = GetEngineInfo.LLMTurn() ? FreeResponseLLMPromptAITurn : FreeResponseLLMPromptUserTurn;
            }

            var chatRequest = new ChatCompletionRequest(
                ToApiName(llmModel),
                systemPrompt,
                LLMMode == GenerationMode.intent ? new JArray { new JObject { ["role"] = "user", ["content"] = text } } : historySnapshot,
                LLMMode == GenerationMode.intent ? 0 : LLMTemperature,
                LLMMode == GenerationMode.intent ? 5 : LLMMaxTokens
            );

            using var webRequest = CreateLLMRequest(chatRequest.JsonBody, "/chat/completions");

            // Send the web request and wait for it to return
            yield return webRequest.SendWebRequest();

            // --- Part 3: Handle the response ---
            string finalResponse = string.Empty; // In case of error
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var responseJson = JObject.Parse(webRequest.downloadHandler.text);
                    string reply = responseJson["choices"]?[0]?["message"]?["content"]?.ToString() ?? string.Empty;
                    // Use a regular expression to find and extract the JSON block
                    aiResponseText.text = reply;
                    string pattern = @"json\s*({.*?})\s*";
                    Match match = Regex.Match(reply, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        // The text part is everything BEFORE the JSON block
                        finalResponse = reply.Substring(0, match.Index).Trim();

                        // The clean JSON is in the first captured group
                        string jsonPart = match.Groups[1].Value;

                        Debug.LogWarning($"[Pipeline] Extracted Text: {finalResponse}");
                        Debug.LogWarning($"[Pipeline] Extracted Clean JSON: {jsonPart}");

                        try
                        {
                            var jsonResponse = JObject.Parse(jsonPart);
                            ExecuteActionPlan(jsonResponse);
                        }
                        catch (JsonReaderException e)
                        {
                            // This might happen if the LLM produces malformed JSON inside the code block
                            Debug.LogError($"[Pipeline] Failed to parse extracted JSON part: {jsonPart}\nError: {e.Message}");
                            ExecuteOriginalPlan();
                            // Keep the text part of the response if JSON fails
                            // finalResponse is already set, so no need to change it
                        }
                    }
                    else
                    {
                        // If no JSON block is found, treat the whole reply as the final response
                        finalResponse = reply;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Pipeline] Failed to parse chat response: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"[Pipeline] Chat Request Error: {webRequest.error}");
            }

            // Add the final response to history and update the UI
            AddAssistantMessage(finalResponse);
            HandleChatResponse(finalResponse);

            Debug.Log($"[Pipeline] Finished full processing for: {text}");
        }

        private void ExecuteActionPlan(JObject jsonResponse)
        {
            // check for "plan priority", "target city" and "target color"
            if (jsonResponse["plan priority"] != null && jsonResponse["target city"] != null && jsonResponse["target color"] != null)
            {
                string planPriority = jsonResponse["plan priority"].ToString();
                string targetCity = jsonResponse["target city"].ToString();
                string targetColor = jsonResponse["target color"].ToString();

                if (string.Equals(planPriority, "original plan", StringComparison.OrdinalIgnoreCase))
                {
                    ExecuteOriginalPlan();
                }

                else
                {
                    TryGenerateNewPlan(planPriority, targetCity, targetColor);
                }

                // Log the extracted values
                Debug.Log($"[Pipeline] Plan Priority: {planPriority}, Target City: {targetCity}, Target Color: {targetColor}");
            }
            else
            {
                ExecuteOriginalPlan();
                Debug.LogWarning("[Pipeline] JSON response does not contain expected keys for action plan.");
            }
        }

        private void TryGenerateNewPlan(string planPriority, string targetCity, string targetColor)
        {
            Plan.PlanPriorities llmPlanPriority;
            switch (planPriority.ToLowerInvariant())
            {
                case "share knowledge": llmPlanPriority = Plan.PlanPriorities.ShareKnowledge; break;
                case "find cure": llmPlanPriority = Plan.PlanPriorities.FindCure; break;
                case "safeguard cube supply": llmPlanPriority = Plan.PlanPriorities.SafeguardCubeSupply; break;
                case "safeguard outbreak": llmPlanPriority = Plan.PlanPriorities.SafeguardOutbreak; break;
                case "manage disease": llmPlanPriority = Plan.PlanPriorities.ManagingDisease; break;
                default:
                    Debug.LogWarning($"[Pipeline] Unknown plan priority: {planPriority}. Defaulting to original plan.");
                    ExecuteOriginalPlan();
                    return;
            }

            int llmTargetCityId = -1;

            for (int i = 0; i < GameCatalog.NumberOfCities; i++)
            {
                if (CityDrawer.CityScripts[i].CityCard.CityName.Equals(targetCity, StringComparison.OrdinalIgnoreCase))
                {
                    llmTargetCityId = i;
                    break;
                }
            }

            if (llmTargetCityId == -1)
            {
                Debug.LogWarning($"[Pipeline] Could not find city: {targetCity}. Defaulting to original plan.");
                ExecuteOriginalPlan();
                return;
            }

            Enums.VirusName llmTargetColor;

            switch (targetColor.ToLowerInvariant())
            {
                case "blue": llmTargetColor = Enums.VirusName.Blue; break;
                case "yellow": llmTargetColor = Enums.VirusName.Yellow; break;
                case "red": llmTargetColor = Enums.VirusName.Red; break;
                case "none": llmTargetColor = Enums.VirusName.None; break;
                default:
                    Debug.LogWarning($"[Pipeline] Unknown target color: {targetColor}. Defaulting to original plan.");
                    ExecuteOriginalPlan();
                    return;
            }

            try
            {
                List<PlayerAction> LLMAct = ai.ComputePlanActions(
                    GameRoot.State.Players[GetEngineInfo.LLMTurn() ? 1 : 0],
                    GameRoot.State.Players[GetEngineInfo.LLMTurn() ? 0 : 1],
                    llmPlanPriority,
                    llmTargetCityId,
                    llmTargetColor,
                    GameRoot.State.Players[GetEngineInfo.LLMTurn() ? 1 : 0].ActionsRemaining
                );
                Debug.Log($"[Pipeline] Generated LLM actions: {string.Join(", ", LLMAct.Select(a => a.ToString()))}");
                Furhat.Instance.OverwritePlan(new Plan(llmPlanPriority, llmTargetCityId, llmTargetColor, LLMAct));
                Debug.Log($"[Pipeline] Generated new plan with priority: {llmPlanPriority}, target city ID: {llmTargetCityId}, target color: {llmTargetColor}");
                executing = LLMState.Executing;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Pipeline] Failed to generate new plan: {e.Message}. Defaulting to original plan.");
                ExecuteOriginalPlan();
            }
        }

        private void ExecuteOriginalPlan()
        {
            executing = LLMState.Executing;
        }

        #endregion

        #region LLM Logic

        /// <summary>
        /// Handles the final, sanitized response from the LLM chat.
        /// </summary>
        private void HandleChatResponse(string finalResponse)
        {
            TurnOffCityGlows();
            if (aiResponseText != null)
            {
                //aiResponseText.text = finalResponse;
            }

            if (LLMMode == GenerationMode.intent)
            {
                if (int.TryParse(finalResponse, out int intentId))
                {
                    if (Furhat.Instance != null)
                        Furhat.Instance.ReactToIntent(intentId);
                }
                else
                {
                    if (Furhat.Instance != null)
                    {
                        Debug.LogWarning($"[LLMRequestHandler] Intent mode expected an integer but got: '{finalResponse}'. Defaulting to 0.");
                        Furhat.Instance.ReactToIntent(0);
                    }
                }
            }
            else
            {
                Timeline.Instance.AddEvent(new RLLMResponse(finalResponse));
                var newCities = CheckCityIDReferences(finalResponse);
                //if (newCities.Count > 0)
                //{
                //cityEntities.AddRange(newCities);
                //TurnOnCityGlows(cityEntities);
                //cityGlows = true;
                //}
                Debug.Log($"[LLMRequestHandler] Free response: {finalResponse}");
                // if LLM turn and state idle state should go to negotiation
                if (GetEngineInfo.LLMTurn() && executing == LLMState.Idle)
                    executing = LLMState.Negotiating;
            }
        }

        #endregion

        #region History Management
        private void AddUserMessage(string content)
        {
            _messagesHistory.Add(new JObject { ["role"] = "user", ["content"] = content });
            TrimHistory();
        }

        private void AddAssistantMessage(string content)
        {
            _messagesHistory.Add(new JObject { ["role"] = "assistant", ["content"] = content });
            TrimHistory();
        }

        /// <summary>
        /// Adds a temporary assistant message that will be included in the next LLM request but not saved in the long-term history.
        /// </summary>
        /// <param name="content">The content of the message.</param>
        private void AddTemporaryAssistantMessage(string content)
        {
            _temporaryContextMessages.Add(new JObject { ["role"] = "assistant", ["content"] = content });
        }

        /// <summary>
        /// Removes the oldest messages from the history if the count exceeds maxHistoryItems.
        /// </summary>
        private void TrimHistory()
        {
            if (maxHistoryItems <= 0) return;

            while (_messagesHistory.Count > maxHistoryItems)
            {
                _messagesHistory.RemoveAt(0);
            }
        }
        #endregion

        #region Helpers

        private void TurnOnCityGlows(List<int> cityIDs)
        {
            // foreach (var cityId in cityIDs)
            // {
            //     if (cityId >= 0 && cityId < GameCatalog.NumberOfCities)
            //     {
            //         var city = CityDrawer.CityScripts[cityId];
            //         if (city.GlowGameObject != null)
            //         {
            //             city.GlowGameObject.SetActive(true);
            //         }
            //     }
            // }
        }

        public static void TurnOffCityGlows()
        {
            // foreach (var city in CityDrawer.CityScripts)
            // {
            //     if (city.GlowGameObject != null)
            //     {
            //         city.GlowGameObject.SetActive(false);
            //     }
            // }
        }

        private List<int> CheckCityIDReferences(string text)
        {
            var cityIDs = new List<int>();
            for (int i = 0; i < GameCatalog.NumberOfCities; i++)
            {
                var city = CityDrawer.CityScripts[i].CityCard;
                if (city == null) continue; // Skip if city is null

                // Check if the text contains the city name
                if (text.Contains(city.CityName, StringComparison.OrdinalIgnoreCase))
                {
                    cityIDs.Add(city.CityID);
                }
            }
            return cityIDs;
        }

        /// <summary>
        /// Creates a configured UnityWebRequest for the specified backend and endpoint.
        /// </summary>
        private UnityWebRequest CreateLLMRequest(string jsonBody, string endpoint)
        {
            string url = backend switch
            {
                GameStartManager.Backend.OpenRouter => $"https://openrouter.ai/api/v1{endpoint}",
                GameStartManager.Backend.OpenAI => $"https://api.openai.com/v1{endpoint}",
                GameStartManager.Backend.LMStudio => $"{"http://localhost:1234/v1"}{endpoint}",
                _ => throw new ArgumentOutOfRangeException(nameof(backend), "Invalid backend.")
            };

            var webRequest = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody)),
                downloadHandler = new DownloadHandlerBuffer()
            };

            webRequest.SetRequestHeader("Content-Type", "application/json");
            if (backend == GameStartManager.Backend.OpenRouter)
            {
                webRequest.SetRequestHeader("Authorization", "Bearer " + _openRouterKey);
            }
            else if (backend == GameStartManager.Backend.OpenAI)
            {
                webRequest.SetRequestHeader("Authorization", "Bearer " + _openAIKey);
            }
            Debug.Log($"[LLMRequestHandler] Sending request to {url} with body:\n{jsonBody}");
            return webRequest;
        }

        private string ToApiName(object model) => model switch
        {
            LLMModel.Gemma3_1b => "google/gemma-3-1b",
            LLMModel.Gemma3_4b => "google/gemma-3-4b",
            LLMModel.Gemma3_12b => "google/gemma-3-12b",
            LLMModel.Gemma3_27b => "google/gemma-3-27b",
            LLMModel.GPT_41_Nano_paid => backend == GameStartManager.Backend.OpenRouter ? "openai/" : "" + "gpt-4.1-nano",
            LLMModel.GPT_41_Mini_paid => backend == GameStartManager.Backend.OpenRouter ? "openai/" : "" + "gpt-4.1-mini",
            LLMModel.GPT_41_paid => backend == GameStartManager.Backend.OpenRouter ? "openai/" : "" + "gpt-4.1",
            LLMModel.Gemma_3n_4b_free => "google/gemma-3n-e4b-it:free",
            LLMModel.Gemini_25_Flash_paid => "google/gemini-2.5-flash",
            LLMModel.Ministral_8b => "ministral-8b-instruct-2410",
            _ => throw new ArgumentOutOfRangeException(nameof(model), $"Unknown model: {model}")
        };

        /// <summary>Data Transfer Object for building the chat completion request.</summary>
        public class ChatCompletionRequest
        { 
            public string JsonBody { get; }
            public ChatCompletionRequest(string model, string systemPrompt, JArray history, float temp, int maxTokens)
            {
                var finalMessages = new JArray { new JObject { ["role"] = "system", ["content"] = systemPrompt } };
                foreach (var message in history) { finalMessages.Add(message); }

                var requestBody = new JObject
                {
                    ["model"] = model,
                    ["temperature"] = temp,
                    ["max_tokens"] = maxTokens,
                    ["messages"] = finalMessages
                };
                JsonBody = requestBody.ToString(Formatting.None);
                Debug.Log($"[LLMRequestHandler] Sending Chat Request:\n{requestBody.ToString(Formatting.Indented)}");
            }
        }
        #endregion
    }
}