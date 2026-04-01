using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Security.Cryptography;
using System;
using OPEN.PandemicAI; // Required for the CryptographicException
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using static GameStartManager;

[System.Serializable]
public class GameSettings
{
    public PlayerMode mode;
    public string name; // MODIFICATION: Added name field
    public string apiKey;
}

public class GameStartManager : MonoBehaviour
{
    public enum Backend { OpenRouter, OpenAI, LMStudio }
    public static event Action OnApiKeyReady;

    [Header("Backend Selection")]
    public Backend selectedBackend;

    [Header("UI Elements")]
    public GameObject apiKeyPanel;
    public TMP_InputField apiKeyInputField;
    public TMP_InputField passwordInputField;
    public TMP_InputField nameInputField;
    public Button submitButton;
    public Button startPlayButton;
    public TextMeshProUGUI statusText;
    public TMP_Dropdown gameModeDropdown; // Reference to the gamemode dropdown
    public LLMRequestHandler llmRequestHandler; // Reference to the LLMRequestHandler
    public Furhat furhat; // Reference to the Furhat instance

    public string NamePlayer1; // Player name
    public string NamePlayer2; // Player name
    public PlayerMode Mode;
    public string ApiKey;

    private string _decryptedApiKey;
    private const string ENCRYPTED_KEY_PREF = "EncryptedApiKey_"; // Base name for our PlayerPref

    public enum PlayerMode { Human, RuleBased, LLM, Simulation }

    void Start()
    {
        //UpdateUIForSelectedBackend();
        // Populate the gamemode dropdown
        //gameModeDropdown.ClearOptions();
        //gameModeDropdown.AddOptions(new List<string> {
            //PlayerMode.Human.ToString(),
            //PlayerMode.RuleBased.ToString(),
            //PlayerMode.LLM.ToString(),
            //PlayerMode.Simulation.ToString()
        //}); // Add your actual game modes here

        // pressing the submit button should hide the API key panel
        //submitButton.onClick.AddListener(() =>
        //{
            //apiKeyPanel.SetActive(false);
        //});
        GameRoot.Config.PlayerName = NamePlayer1;
        GameRoot.Config.BotName = NamePlayer2;
        startPlayButton.onClick.AddListener(HandleSubmissionFromEditor);
    }

    private void UpdateUIForSelectedBackend()
    {
        if (selectedBackend == Backend.LMStudio)
        {
            // For LMStudio, we don't need an API key or password
            apiKeyInputField.gameObject.SetActive(false);
            passwordInputField.gameObject.SetActive(false);
            statusText.text = "LMStudio does not require an API key. Ready to start.";
        }
        else
        {
            // For other backends, check if a key is saved
            passwordInputField.gameObject.SetActive(true); // Always need the password field
            string playerPrefKey = ENCRYPTED_KEY_PREF + selectedBackend.ToString();
            if (PlayerPrefs.HasKey(playerPrefKey))
            {
                // Key exists, so we only need the password
                apiKeyInputField.gameObject.SetActive(false);
                statusText.text = "Enter your password to unlock the API Key.";
            }
            else
            {
                // First time setup, we need both the API key and a password
                apiKeyInputField.gameObject.SetActive(true);
                statusText.text = "Enter your API Key and a password to protect it.";
            }
        }
    }


private void HandleSubmissionFromEditor()
    {
        switch (Mode)
        {
            case PlayerMode.Human:
                GameRoot.Config.SpeechRecActivated = false;
                GameRoot.Config.UseFurhat = false;
                GameRoot.Config.UseAIContainmentSpecialist = false;
                GameRoot.Config.UseAIQuarantineSpecialist = false;
                GameRoot.Config.SimulationMode = false;
                GameRoot.Config.StepByStepSimulation = false;
                GameRoot.Config.PlayerCardsSeed = 11;
                GameRoot.Config.InfectionCardsSeed = 11;
                break;
            case PlayerMode.RuleBased:
                GameRoot.Config.SpeechRecActivated = true;
                GameRoot.Config.UseFurhat = true;
                GameRoot.Config.UseAIContainmentSpecialist = false;
                GameRoot.Config.UseAIQuarantineSpecialist = false;
                GameRoot.Config.SimulationMode = false;
                GameRoot.Config.StepByStepSimulation = false;
                selectedBackend = Backend.LMStudio; // Force LMStudio backend for rule-based
                llmRequestHandler.LLMMode = LLMRequestHandler.GenerationMode.intent; // Set to rule-based mode
                llmRequestHandler.llmModel = LLMRequestHandler.LLMModel.Gemma3_12b; // Set the model to RuleBased
                furhat.furhatConfig.JSONFileName = "event-dialog-mapping-flow.json";
                GameRoot.Config.PlayerCardsSeed = 5;
                GameRoot.Config.InfectionCardsSeed = 5;
                break;
            case PlayerMode.LLM:
                GameRoot.Config.SpeechRecActivated = true;
                GameRoot.Config.UseFurhat = true;
                GameRoot.Config.UseAIContainmentSpecialist = false;
                GameRoot.Config.UseAIQuarantineSpecialist = false;
                selectedBackend = Backend.OpenAI; // Force OpenAI backend for LLM
                GameRoot.Config.SimulationMode = false;
                GameRoot.Config.StepByStepSimulation = false;
                llmRequestHandler.LLMMode = LLMRequestHandler.GenerationMode.free; // Set to free mode for LLM
                llmRequestHandler.llmModel = LLMRequestHandler.LLMModel.GPT_41_paid; // Set the model to LLM
                furhat.furhatConfig.JSONFileName = "furhatlm.json";
                GameRoot.Config.PlayerCardsSeed = 2;
                GameRoot.Config.InfectionCardsSeed = 2;
                break;
            case PlayerMode.Simulation:
                GameRoot.Config.SpeechRecActivated = false;
                GameRoot.Config.UseFurhat = false;
                GameRoot.Config.UseAIContainmentSpecialist = true;
                GameRoot.Config.UseAIQuarantineSpecialist = true;
                GameRoot.Config.SimulationMode = true;
                GameRoot.Config.StepByStepSimulation = true;
                break;
            default:
                statusText.text = "Invalid player mode selected in JSON file.";
                return;
        }


        GameRoot.Config.PlayerName = NamePlayer1; // Save the player name
        
        startPlayButton.gameObject.SetActive(false);

        _decryptedApiKey = ApiKey;
        OnApiKeyReady?.Invoke();
    }


    private void HandleSubmissionFromFile()
    {
        PlayerMode playerMode;
        string apiKey;

        // --- NEW LOGIC: Load mode and API key directly from the JSON file ---
        try
        {
            string path = Path.Combine(Application.streamingAssetsPath, "game-mode.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                GameSettings settings = JsonUtility.FromJson<GameSettings>(json);

                // Set mode and API key from the loaded file
                playerMode = settings.mode;
                name = settings.name; // MODIFICATION: Get name from settings
                apiKey = settings.apiKey;

                // TODO: Assign the loaded API key where it needs to be used.
                // For example, if you have a class member variable '_decryptedApiKey':
                _decryptedApiKey = apiKey;

                Debug.Log($"Successfully loaded mode '{playerMode}' and API Key from JSON.");
            }
            else
            {
                Debug.LogError("game-mode.json not found in StreamingAssets. Cannot proceed.");
                statusText.text = "ERROR: game-mode.json not found!";
                return; // Stop execution if the config file is missing
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to load or parse game-mode.json: " + ex.Message);
            statusText.text = "Error reading config file.";
            return; // Stop execution on error
        }
        // --- END OF NEW LOGIC ---

        switch (playerMode)
        {
            case PlayerMode.Human:
                GameRoot.Config.SpeechRecActivated = false;
                GameRoot.Config.UseFurhat = false;
                GameRoot.Config.UseAIContainmentSpecialist = false;
                GameRoot.Config.UseAIQuarantineSpecialist = false;
                GameRoot.Config.SimulationMode = false;
                GameRoot.Config.StepByStepSimulation = false;
                GameRoot.Config.PlayerCardsSeed = 11;
                GameRoot.Config.InfectionCardsSeed = 11;
                break;
            case PlayerMode.RuleBased:
                GameRoot.Config.SpeechRecActivated = true;
                GameRoot.Config.UseFurhat = true;
                GameRoot.Config.UseAIContainmentSpecialist = false;
                GameRoot.Config.UseAIQuarantineSpecialist = false;
                GameRoot.Config.SimulationMode = false;
                GameRoot.Config.StepByStepSimulation = false;
                selectedBackend = Backend.LMStudio; // Force LMStudio backend for rule-based
                llmRequestHandler.LLMMode = LLMRequestHandler.GenerationMode.intent; // Set to rule-based mode
                llmRequestHandler.llmModel = LLMRequestHandler.LLMModel.Gemma3_12b; // Set the model to RuleBased
                furhat.furhatConfig.JSONFileName = "event-dialog-mapping-flow.json";
                GameRoot.Config.PlayerCardsSeed = 5;
                GameRoot.Config.InfectionCardsSeed = 5;
                break;
            case PlayerMode.LLM:
                GameRoot.Config.SpeechRecActivated = true;
                GameRoot.Config.UseFurhat = true;
                GameRoot.Config.UseAIContainmentSpecialist = false;
                GameRoot.Config.UseAIQuarantineSpecialist = false;
                selectedBackend = Backend.OpenAI; // Force OpenAI backend for LLM
                GameRoot.Config.SimulationMode = false;
                GameRoot.Config.StepByStepSimulation = false;
                llmRequestHandler.LLMMode = LLMRequestHandler.GenerationMode.free; // Set to free mode for LLM
                llmRequestHandler.llmModel = LLMRequestHandler.LLMModel.GPT_41_paid; // Set the model to LLM
                furhat.furhatConfig.JSONFileName = "furhatlm.json";
                GameRoot.Config.PlayerCardsSeed = 2;
                GameRoot.Config.InfectionCardsSeed = 2;
                break;
            case PlayerMode.Simulation:
                GameRoot.Config.SpeechRecActivated = false;
                GameRoot.Config.UseFurhat = false;
                GameRoot.Config.UseAIContainmentSpecialist = true;
                GameRoot.Config.UseAIQuarantineSpecialist = true;
                GameRoot.Config.SimulationMode = true;
                GameRoot.Config.StepByStepSimulation = true;
                break;
            default:
                statusText.text = "Invalid player mode selected in JSON file.";
                return;
        }

        //string name = nameInputField.text;
        if (string.IsNullOrEmpty(name))
        {
            statusText.text = "Player name cannot be empty.";
            //return;
        }
        GameRoot.Config.PlayerName = name; // Save the player name

        statusText.text = $"Starting game in {playerMode} mode.";
        startPlayButton.gameObject.SetActive(false);
        OnApiKeyReady?.Invoke();
    }

    public string GetApiKey()
    {
        if (string.IsNullOrEmpty(_decryptedApiKey))
        {
            Debug.LogWarning("API Key has not been decrypted or is not available.");
        }
        return _decryptedApiKey;
    }

    public void DeleteApiKey()
    {
        string playerPrefKey = "EncryptedApiKey_" + selectedBackend.ToString();

        if (PlayerPrefs.HasKey(playerPrefKey))
        {
            PlayerPrefs.DeleteKey(playerPrefKey);
            PlayerPrefs.Save();
            _decryptedApiKey = null;

            Debug.Log(selectedBackend.ToString() + " API Key has been deleted.");

            // Show the setup UI again
            UpdateUIForSelectedBackend();
            passwordInputField.text = "";
            apiKeyInputField.text = "";
            apiKeyPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("No API Key was found to delete.");
        }
    }
}