# PandemicAI — Robot Class

> This document provides an overview of how the `Furhat` class and its partials for dialog, speech, gaze, and board interaction. We used Unity version 6000.0.371f for development. We used both Windows and MacOS devices for development but recommend deploying the project as a Windows project if multitouch support is required.

---

## Contents
- [0) Return to Main Document](../pandemic_multitouch_doc.md)
- [1) Role at a Glance](#1-role-at-a-glance)
- [2) Core Components](#2-core-components)
- [3) Runtime Flow](#3-runtime-flow)
- [4) Configuration](#4-configuration)
- [5) Planning & Action Execution](#5-planning--action-execution)
- [6) Dialog Acts & Extending Behavior](#6-dialog-acts--extending-behavior)
- [7) Logging](#7-logging--debugging)

---

## 1) Role at a Glance
`Furhat` controls the robot teammate during Pandemic gameplay:
- Listens to **game events**.
- Selects a **dialog act** (utterance) from JSON mappings.
- **Speaks** and emits **custom events** embedded in dialog that trigger gaze or GUI actions.
- Drives **gaze** (user/board/aversion) with calibration.
- Manages **AI game execution** (Move/Treat/Fly/Cure/Share/Discard/End) as a simple state machine.

---

## 2) Core Components

- **Furhat (MonoBehaviour)** — Lifecycle, main loop, AI/LLM mode, planning state machine, VAD windows, LED.  
- **FurhatSpeech (partial)** — `ProcessEvent → ShouldSpeak → GetDialog → ExecuteDialogAct`, plus handlers for special events and speech‑borne custom events (`click…`, `drag…`, `lookAt…`).  
- **FurhatGaze (partial)** — Gaze targets (*UserFace, BoardRandom, BoardClick, BoardAnimation, Aversion*), idle policy, screen‑to‑head calibration.  
- **DialogActs** — Loads JSON (per event: `relevance`, `ai/user/engine` lists, optional `conditions`), keeps **per‑list circular indices** for deterministic cycling.

---

## 3) Runtime Flow
### Lifecycle

1. **Start()**
   - Waits for API key if needed; then **InitializeFurhat()**.
   - Loads dialog mappings, sets up logging, seeds per-list dialog indices from `DialogActStartingUtterance`.

2. **InitializeFurhat()**
   - Hooks SDK callbacks (sensed users, end-of-speech, custom events).
   - Initializes gaze calibration/overlays and game map geometry.
   - Resets timers and internal flags.

3. **Update()**
   - Manages listening windows (start/stop VAD), enforces silence policy.
   - Ticks gaze and LLM/AI negotiation/responding phases.
   - Advances **plan execution** if not speaking and no timeline blocks are pending.

4. **OnDestroy()/OnApplicationQuit()**
   - Unhooks callbacks; stops VAD; closes SDK connections.

---
### Event → Dialog → Speech
On each event, `ProcessEvent` first handles any special cases and returns early if one applies. Otherwise, it computes `ShouldSpeak`; if false, it exits. If true, it parses variables, then queries `DialogActs` for an utterance; if none exists, it exits. If one is found, it executes the dialog act `(furhat.Say(...))`, during which inline `|event(...)|` tags can trigger gaze, gestures or GUI actions.
```
Game event
└─► ProcessEvent(type, text) 
    ├─ check for special cases (new turn, discard forcing, game over) 
    ├─ evaluate ShouldSpeak(relevance, timeSinceLastSpeech) 
    ├─ vars = parse(text) + name 
    ├─ utterance = DialogActs.GetDialog(type, isMyTurn, vars["condition"]?) 
    └─ ExecuteDialogAct(utterance, vars) → furhat.Say(...)
```

**Custom events during speech** (inline tags executed in the middle of speech):  
`lookAtUser`, `lookAtBoard`, `clickActionButton`, `dragPawn`, `clickCard`, `clickAccept`, `clickCube`, `clickDiscard`.

### Planning → GUI Execution
- AI turn: generate/announce plan → **PlayingState** advances (e.g., *Flying → ClickCard → Accept*).  
- GUI executor consumes **Pending* targets** (click/drag names) set by custom events and state helpers.

---

## 4) Configuration

- **`FurhatConfig`** — JSON filename, start index for dialog cycling, timings (`handleEventTime`, `reactiveCommentDelay`, `planningPhase`).  
- **`GazeSettings`** — probabilities & timing ranges for idle modes, user‑lost timeout, calibration anchors (TL/TR/BL/BR), send intervals.
- **`game-mode.json`** — 

{
  "mode": "Human",
  "name": "Player Name",
  "apiKey": "your_api_key_here"
}
---

## 5) Planning & Action Execution

### AI Modes
`AIMode` governs phases: `Idle`, `GeneratingPlan`, `AskingForFeedback`, `ExecutingPlan`, `GameEnded`.  
In **scripted** mode the robot discloses the plan, listens for feedback and changes the plan priority based on the user feedback. If there is no feedback or if the user agrees it proceedes to execute the initial plan. In **free LLM** mode it also discloses the plan and discuss the plan until the LLM determines that the user is on board with the plan. If there is no feedback from the user it proceeds to to execute the initial plan.

### Playing State Machine
- **Move / Charter**: `Moving → MovingDragPawn` | `Chartering → CharteringDragPawn`
- **Fly**: `Flying → FlyingClickCard → FlyingAccept`
- **Treat**: `Treating → TreatingClickCube` (optimized paths for consecutive treats)
- **Cure**: `Curing → SelectCardOne/Two/Three/Final → Accept`
- **Share**: `Sharing → Accept`
- **Discard**: `SelectCard → Discard`
- **EndTurn**: `EndTurn`

Actions emit **Timeline events** and set **pending targets** (`PendingClickTarget`, `PendingDragTarget`) that the GUI executor consumes to perform actual UI interactions.

---

## 6) Dialog Acts & Extending Behavior
The main mode of scripting the robot beahvior is in its dialog acts.
You can see all dialog acts in JSON **[here](../Assets/StreamingAssets/event-dialog-mapping-flow.json)**.

**New dialog event**
1. Add JSON entry: `eventType`, `relevance`, and one or more of `aiTurnBehaviors`, `userTurnBehaviors`, `engineTurnBehaviors`.  
2. (Optional) add `conditions` variants.  
3. Emit the event with `eventText` (key=value pairs)

**Speech‑triggered gaze**: insert `|event(lookAtUser)|`, `|event(lookAtBoard)|` in utterance templates.
**Speech‑triggered gesture**: insert `|gesture(Smile)|`, `|gesture(Nod)|` in utterance templates.

---

## 7) Logging & Debugging

- **File log**: `Assets/Logs/furhat_log_<timestamp>.txt` with lines like `[ts]\tCommand\tDetails` (`Say`, `Gaze`, `Event`, `Start/StopMicrophone`, transitions). 

---

**Tip:** Keep dialog JSON modular; adding `conditions` can add adaptability and adding dialog acts events that do not currently have any dialog acts can add more variaty. Remember to adjust relevance so the robot is not constantly speaking.
