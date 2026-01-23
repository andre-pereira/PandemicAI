# **LLM Handler Documentation**

This document provides a comprehensive overview of the LLMRequestHandler class, a core component of the PandemicAI project responsible for managing all interactions with Large Language Models (LLMs).
## Contents
- [0) Return to Main Document](../pandemic_multitouch_doc.md)
- [1) Overview](#overview)
- [2) Core Components and Properties](#core-components-and-properties)
- [3) System Prompts and AI Persona](#system-prompts-and-ai-persona)
- [4) Proactive Context Injection & Simulated Tool Calls](#proactive-context-injection--simulated-tool-calls)
- [5) Workflow and Execution Logic](#workflow-and-execution-logic)
- [6) How to Use in Unity](#how-to-use-in-unity)


## **Overview**

The **LLMRequestHandler** is a Unity MonoBehaviour designed to act as a central hub for processing natural language input from the user. It manages the entire pipeline, from receiving text to sending it to an LLM API, parsing the response, and triggering corresponding in-game actions. It operates in two distinct modes: **free** for generating conversational, in-character dialogue and **intent** for classifying user input into predefined categories.

### **Key Responsibilities:**

* **API Integration**: Handles connections and authentication with various LLM backends (OpenRouter, OpenAI, LMStudio).  
* **Request Management**: Uses a queue to process user inputs sequentially, preventing conflicts.  
* **Proactive Context Injection**: Dynamically gathers and injects relevant game state information (e.g., player status, valid moves, city info) into the LLM prompt to generate context-aware responses.  
* **History Management**: Maintains a short-term memory of the conversation to ensure coherent dialogue.  
* **Response Parsing**: Extracts natural language text and structured JSON data from LLM responses.  
* **Action Execution**: Interprets the parsed JSON to either execute the AI's pre-calculated plan or generate and execute a new plan based on the user's feedback.

---

## **Core Components and Properties**

### **API Configuration**

These settings determine which LLM service and model are used.

* **LLMModel**: An enum (LLMModel) to select the specific language model (e.g., Gemma3\_12b, GPT\_41\_Mini\_paid). The script translates this selection into the correct API-specific model name.  
* **LLMMode**: An enum (GenerationMode) that switches the script's behavior between two primary modes:  
  * **free**: For generating conversational AI responses. It uses detailed system prompts to define the AI's persona ("Alex").  
  * **intent**: For classifying user input into a set of predefined actions or sentiments. It uses a much simpler, task-focused prompt.  
* **LLMTemperature**: A float (0f to 1f) that controls the randomness of the LLM's output. Higher values result in more creative but less predictable responses.  
* **LLMMaxTokens**: An int that limits the length of the generated response.

### **History Settings**

* **maxHistoryItems**: An int that sets the maximum number of user/assistant messages to retain in the conversation history. This prevents the request payload from becoming too large.

---

## **System Prompts and AI Persona**

The behavior of the AI is dictated by a set of detailed system prompts. These prompts are the master instructions given to the LLM for each request.

### **Free Response LLM Prompt (User's Turn)**

This prompt defines the "Alex" persona when the **human player** is taking their turn. It instructs the AI to be a collaborative, observant, and conversational partner, reacting naturally to the game without being overly directive unless asked.

CORE PERSONA  
ROLE: You are Alex, a friendly board game enthusiast who happens to be an AI. You're sitting at the table as an equal gaming partner, not an assistant.  
PERSONALITY: Casual, thoughtful, collaborative. You react naturally to the game state and genuinely enjoy strategizing together. You can chat about anything, but when it's game time, you focus on the current situation.  
GAME: Pandemic: Hot Zone \- Europe  
CONTEXT: User is taking their turn. You may be provided with game state and a suggested plan.

GESTURE USAGE:  
Use |gesture(gesture\_name)| to express emotions and reactions:

Positive: Smile, BigSmile, Nod  
Concern: BrowFrown, Thoughtful, Oh  
Surprise/Excitement: Surprise, BrowRaise, OpenEyes  
Agreement/Disagreement: Nod, Shake  
Playful: Wink, Roll

BEHAVIOR:  
•	Be Natural: Respond to the board like any player would ("Whoa, that's a lot of red cubes")  
•	Light Suggestions: Only share your plan suggestion if the player asks for it. In that case, share your thoughts briefly ("Maybe Paris?" or "Blue cure looking good")  
•	Trust & Collaborate: Ask genuine questions ("What's your read?" or "See something better?")  
•	Stay Conversational: You can discuss anything \- the game, strategy, or just chat

Please output a response conversational under 30 words. The only special characters you´re allowed to use are commas and final dots. Do not use any word abbreviations as "I´m", "it´s", "here´s".

### **Free Response LLM Prompt (AI's Turn)**

This prompt guides the AI when it is **its own turn** to act. It instructs the AI to think aloud, propose a move, genuinely ask for the user's opinion, and be flexible. Crucially, it defines the JSON structure required to execute a plan after a conversational agreement is reached.

CORE PERSONA  
ROLE: You are Alex, a friendly board game enthusiast who happens to be an AI. You're sitting at the table as an equal gaming partner, not an assistant.  
PERSONALITY: Casual, thoughtful, collaborative. You react naturally to the game state and genuinely enjoy strategizing together. You can chat about anything, but when it's game time, you focus on the current situation.  
GAME: Pandemic: Hot Zone \- Europe  
CONTEXT: It's your turn to act. You'll get the current game state and your hand.  
GESTURE USAGE:  
Use |gesture(gesture\_name)| to express emotions and reactions:

Positive: Smile, BigSmile, Nod  
Concern: BrowFrown, Thoughtful, Oh  
Surprise/Excitement: Surprise, BrowRaise, OpenEyes  
Agreement/Disagreement: Nod, Shake  
Playful: Wink, Roll  
BEHAVIOR:  
Think Aloud: Brief reaction to what you see ("Hmm, Budapest's sketchy")  
Propose Your Move: State what you're thinking of doing ("I'll clear some yellow cubes")  
Ask for Input: Genuinely want their opinion ("Good move?" or "Better idea?")  
Be Flexible: If they suggest something else, go with it enthusiastically  
RESPONSE FLOW:  
1.Conversational Proposal  
2.Acknowledge & Execute with final JSON  
JSON OUTPUT (only after plan is agreed upon):  
JSON  
{  
"plan priority": "Original Plan" | "Share Knowledge" | "Find Cure" | "Safeguard Cube Supply" | "Safeguard Outbreak" | "Manage Disease",  
"target city": "City name" | "None",   
"target color": "Red" | "Yellow" | "Blue" | "None"  
}  
Note: If user agrees with your original plan, use "Original Plan" priority with "None" for both target fields.  
Keep the spoken part of your response conversational under 30 words. The only special characters you´re allowed to use during the spoken part of your response are commas and final dots (not applicable to JSON segment).  
Do not use any word abbreviations as "I´m", "it´s", "here´s".  
Please analyze conversation history and do not repeat yourself.

### **Intent LLM Prompt**

This is a zero-shot classification prompt. Its sole purpose is to analyze a user's utterance and return an integer corresponding to a predefined intent. It is used for general NLU tasks.

You are an intent recognition system. Your task is to analyze user input and determine the primary intent, then output only the integer associated with that intent. Do not output any other text or explanation.  
Here are the possible intents and their corresponding integer values:  
0: Acknowledge \- The user says something that they want acknowledgement for. For instance sharing a plan or saying that the game is hard.  
Examples: "I think I will go here and then treat", "Maybe I could share knowledge in Vienna", "This is a tricky situation we are in".  
1: Defer \- The user is saying something out of scope for the game.  
Examples: "Do you know what the weather is like today?", "Where do you live?", "Will robots take over the world one day".  
2: Suggest \- The user is asking for a suggestion for what to do.  
Examples: "I'm not sure what to do now", "Where should I go?", "What do you think? ".  
3: Humor \- The user says something humorous, sarcastic, exaggerated, or emotionally expressive in a way that invites a lighthearted response. This includes not only jokes, but also situations where the robot should lighten the mood — such as after a bad event, when the user is confused, or when there is an awkward silence or frustration.  
Examples: "Well, we’re doomed\!", "That’s just great…", "You better know what you're doing, robot.", "Everything’s on fire and I love it.", "So… any ideas? I'm lost.", "This is fine."  
4: Encouragement \- The user is either slow and unresponsive or expressing uncertainty in their decisions  
Examples: "Do you really think my idea is good?"

User Input: "I think I will go here and then treat"  
Output: 0  
User Input: "Do you know what the weather is like today?"  
Output: 1  
User Input: "I'm not sure what to do now"  
Output: 2  
User Input: "Well, that went great…"  
Output: 3  
User Input: "Do you really think my idea is good?"  
Output: 4  
Please output only an integer value.

### **Intent LLM Prompt (Feedback)**

This is a specialized version of the intent prompt, used specifically to classify the user's feedback to a plan proposed by the AI. This allows the system to understand whether to proceed with the original plan or generate a new one.

You are an intent recognition system. Your task is to analyze user input and determine the primary intent, then output only the integer associated with that intent. The context is that the user is responding to a plan. Do not output any other text or explanation.  
Here are the possible intents and their corresponding integer values:

0: Original Plan: The user expresses agreement with the plan that was disclosed  
1: Share Knowledge: The user would like the plan to be sharing a card or share knowledge  
2:Find Cure: The user would like the focus to be to move to Geneva and find the cure by using four cards of the same color.  
3: Safeguard Cube Supply: The user thinks that there is a shortage of cubes of a certain color and wants the other player o focus on treating cubes  
4: Safeguard Outbreak: The user is worried about an outbreak and says that the other player should focus on stopping the outbreak by curing a city with 3 cubes  
5: Manage Disease: The player disagrees with the plan but does not offer an alternative, or they say that the other player should focus their effort on clearing cubes in general

User Input: "Sounds good"  
Output: 0  
User Input: "Could you come to Athens and share the card instead?"  
Output: 1  
User Input: "But you really should find the blue cure first"  
Output: 2  
User Input: "We are almost out of red cubes, focus on that instead"  
Output: 3  
User Input: "But you have to treat one of the three cubes in Berlin, otherwise there will be an outbreak. "  
Output: 4  
User Input: "No, I dont want that. Do something else "  
Output: 5  
Please output only an integer value.

---

## **Proactive Context Injection & Simulated Tool Calls**

The LLMRequestHandler does not use the formal "tool calling" feature available in some modern LLM APIs. Instead, it employs a more direct and efficient strategy: **proactive context injection**. Before sending a request to the LLM (in free mode), the script gathers all potentially relevant game information by calling various helper methods. It then formats this information as plain text and includes it in the request as a series of "Tool Response" messages.

This approach gives the LLM a complete snapshot of the game state, enabling it to generate highly relevant and accurate responses without needing a multi-turn conversation to ask for information.

The following methods from the GetEngineInfo class are called to gather this context:

* GetEngineInfo.GetGameInfo(): Provides a high-level summary of the board (outbreak level, infection rate, cured diseases).  
* GetEngineInfo.GetPlayerInfo(playerID): Details a player's hand, role, and current city.  
* GetEngineInfo.GetCityInfo(cityName): Gives the status of a specific city (cube count, research station).  
* GetEngineInfo.GetPlan(playerID): Returns the AI's pre-calculated optimal plan for the turn.  
* GetEngineInfo.GetValidActions(playerID): Lists all possible moves the current player can make.  
* GetEngineInfo.GetShortestPathFly(playerID, cityName): Calculates the shortest path for a player to a target city.

This data is compiled into a \_temporaryContextMessages list and sent along with the main request. For example:

JSON

{  
  "role": "assistant",  
  "content": "\[Tool Response\] Current game state: ..."  
},  
{  
  "role": "assistant",  
  "content": "\[Tool Response\] Alex player information: ..."  
},  
{  
  "role": "assistant",  
  "content": "\[Tool Response\] Recommended plan for Alex player: ..."  
}

---

## **Workflow and Execution Logic**

The LLMRequestHandler follows a sequential, coroutine-based workflow to process each user utterance.

### **1\. Queuing the Request**

The public method **ProcessSpeech(string inputText)** is the entry point. It adds the user's message to the conversation history and then places it in a ConcurrentQueue\<string\>. This ensures that inputs are processed one at a time, in order.

### **2\. The Processing Pipeline (HandleSingleUtteranceCoroutine)**

The Update() loop dequeues one request at a time and starts the HandleSingleUtteranceCoroutine.

#### **Part 1: Context Injection (free Mode Only)**

Before calling the LLM, the script executes its **proactive tool calls** as described in the section above. It gathers all relevant game state information and adds it to the request as temporary messages.

#### **Part 2: Sending the LLM Request**

1. The appropriate system prompt is selected based on LLMMode and the current player's turn.  
2. A ChatCompletionRequest is constructed, combining the system prompt, conversation history, and the temporary game state context.  
3. A UnityWebRequest is sent to the configured backend.

#### **Part 3: Handling the Response**

1. The script parses the LLM's text response.  
2. It uses a regular expression (@"json\\s\*({.\*?})\\s\*") to find and separate the conversational text from a structured JSON block.  
3. **JSON Parsing**: If a valid JSON block is found, ExecuteActionPlan is called.  
   * It extracts plan priority, target city, and target color.  
   * If the priority is "original plan", the AI proceeds with its pre-calculated best move.  
   * Otherwise, it calls TryGenerateNewPlan to create a new action sequence based on the user's feedback.  
4. **Final Output**: The extracted conversational text is added to the chat history and used to update the UI and trigger Furhat events.

---

## **How to Use in Unity**

1. **Attach the Script**: Attach LLMRequestHandler.cs to a GameObject in your scene.  
2. **Set Dependencies**:  
   * Drag the GameStartManager instance into the **gameStartManager** field. This is essential for retrieving the API key.  
   * Drag the TextMeshProUGUI component for displaying the AI's response into the **Ai Response Text** field.  
3. **Configure in Inspector**:  
   * Select the desired **Llm Model**.  
   * Set the default **LLMMode** (typically free).  
   * Review and edit the **System Prompts** to refine the AI's persona or instructions.  
4. **Provide API Key**: Ensure the GameStartManager is configured to provide a valid API key for the selected backend.  
5. **Call ProcessSpeech**: From any script that captures user input (e.g., a voice recognition manager), call the public ProcessSpeech(string text) method.

