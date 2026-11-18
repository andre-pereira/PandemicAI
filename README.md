# PandemicAI: Documentation

PandemicAI is a Unity implementation of a cooperative board game in which a person and a robot teammate can propose a plan, discuss it, and act on the same board. This repository contains the tabletop code used in the pilot. The game and planner can also run in simulation without a physical robot.

## Framework architecture

![The shared board supplies state to the turn planner and events to the timeline. The Furhat teammate receives plans and events, listens to the player, and sends actions back to the board. The system records both game events and robot commands.](docs/pandemicai-runtime.png)

**Follow one move.** The [planner](Assets/Scripts/AI/PandemicAI.cs) selects a destination. While announcing a move, the robot can reach a `|event(dragPawn)|` tag in its utterance. At that point the [speech callback](Assets/Scripts/AI/Furhat/FurhatSpeech.cs) reads the destination from the queued action, and the [board executor](Assets/Scripts/AI/Furhat/FurhatGameExecutor.cs) drags the digital pawn. The plan determines *where* the pawn goes; the tag determines *when* it moves.

**Where to start with your own study:**

- **Decision-making:** Change how the planner prioritizes exchanging cards versus preventing an outbreak, then run [simulated games](Assets/Scripts/AI/WinRate.cs) on the same seeds to compare outcomes before involving participants.
- **Multimodal behavior:** To test whether looking at a partner invites a reply, add a gaze cue to a plan proposal in an [authored event-to-utterance mapping](Assets/StreamingAssets/event-dialog-mapping-flow.json). Keep the wording and planner fixed. The robot log records gaze commands and recognized speech; the [game event log](Assets/Scripts/model/EventLogger.cs) records what happened next on the board.
- **Speech and dialog:** A follow-up study used the same task to compare two speech synthesizers. For studies that change how the teammate interprets or answers the player, the [dialog integration guide](LLMIntegration/llmintegration_documentation.md) describes the Structured and Generative implementations.

VR adaptations have also been used in separate studies. The map above describes the tabletop release.

## Contents

- [1) Unity Editor Setup](UnityEditor/pandemic_unity_editor.md)
- [2) Interface](Interface/pandemic_ui_doc.md)
- [3) GameAI](GameAI/gameai_documentation.md)
- [4) Furhat](Furhat/furhat_documentation.md)
- [5) LLMIntegration](LLMIntegration/llmintegration_documentation.md)
---
