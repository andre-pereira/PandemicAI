using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Security.Cryptography;
using System;
using OPEN.PandemicAI; // Required for the CryptographicException
using System.Collections.Generic;
using System.IO;
using UnityEditor;

[System.Serializable]
public class GameSettings
{
    public string mode;
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

        startPlayButton.onClick.AddListener(HandleSubmission);
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

    private void HandleSubmission()
    {
        string playerMode;
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
            case nameof(PlayerMode.Human):
                GameRoot.Config.SpeechRecActivated = false;
                GameRoot.Config.UseFurhat = false;
                GameRoot.Config.UseAIContainmentSpecialist = false;
                GameRoot.Config.UseAIQuarantineSpecialist = false;
                GameRoot.Config.SimulationMode = false;
                GameRoot.Config.StepByStepSimulation = false;
                GameRoot.Config.PlayerCardsSeed = 11;
                GameRoot.Config.InfectionCardsSeed = 11;
                break;
            case nameof(PlayerMode.RuleBased):
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
            case nameof(PlayerMode.LLM):
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
            case nameof(PlayerMode.Simulation):
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

        // --- OLD LOGIC FOR API KEY HANDLING IS NOW COMMENTED OUT ---
        /*
        // If no API key or password is provided, default to LMStudio
        if (string.IsNullOrWhiteSpace(apiKeyInputField.text) && string.IsNullOrWhiteSpace(passwordInputField.text))
        {
            selectedBackend = Backend.LMStudio;
        }

        if (selectedBackend == Backend.LMStudio)
        {
            statusText.text = "Starting with LMStudio backend.";
            startPlayButton.gameObject.SetActive(false);
            OnApiKeyReady?.Invoke();
            return;
        }

        string password = passwordInputField.text;
        if (string.IsNullOrEmpty(password))
        {
            statusText.text = "Password cannot be empty for this backend.";
            return;
        }

        string playerPrefKey = ENCRYPTED_KEY_PREF + selectedBackend.ToString();

        // SCENARIO 1: First-time setup (no key saved yet)
        if (!PlayerPrefs.HasKey(playerPrefKey))
        {
            string apiKey = apiKeyInputField.text;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                statusText.text = "API Key field cannot be empty for this backend.";
                return;
            }

            // Encrypt and save
            string encryptedKey = EncryptionHelper.Encrypt(apiKey, password);
            PlayerPrefs.SetString(playerPrefKey, encryptedKey);
            PlayerPrefs.Save();

            _decryptedApiKey = apiKey;
            statusText.text = "API Key saved and encrypted!";
            apiKeyPanel.SetActive(false);
            Debug.Log($"Successfully saved key for {selectedBackend}");
            OnApiKeyReady?.Invoke();
        }
        // SCENARIO 2: Key exists, trying to unlock
        else
        {
            string encryptedKey = PlayerPrefs.GetString(playerPrefKey);
            try
            {
                _decryptedApiKey = EncryptionHelper.Decrypt(encryptedKey, password);
                statusText.text = "API Key unlocked successfully!";
                apiKeyPanel.SetActive(false);
                Debug.Log($"Successfully decrypted key for {selectedBackend}");
                OnApiKeyReady?.Invoke();
            }
            catch (CryptographicException)
            {
                statusText.text = "Wrong password. Please try again.";
                passwordInputField.text = "";
            }
            catch (Exception ex)
            {
                statusText.text = "Decryption failed. The key may be corrupt.";
                Debug.LogError("Error decrypting API key: " + ex.Message);
            }
        }
        */
        // --- END OF COMMENTED OUT SECTION ---

        // Directly proceed to start the game after loading settings
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