# MetaPlay: A Multi Game Offline Platform

A Final Year Project (BS Computer Science, University of Management and Technology, Lahore — Fall 2025 to Spring 2026) that packages 7 distinct mini-games into a single offline, ads-free Android app.

## Objective
Design and develop a multi-game Android platform that runs fully offline and ad-free, incorporating Artificial Intelligence in select games to increase realism and interactivity, all under one unified launcher/main menu.

## Built With
- Unity (Game Engine)
- Blender
- C# (Programming/Scripting)
- Sketchfab
- Mixamo
- Stockfish (Chess engine, integrated locally)

## Games Included
| Game | Genre | AI? |
|---|---|---|
| Chess AI | Strategy | Yes — Stockfish + custom bot, multiple AI modes |
| Racing Rivals | Racing | Yes — adaptive opponent behavior |
| Survival.io | Survival | No — scripted spawns |
| Pesticide Escape | Action/FPS | Yes — AI navigation for insect enemies |
| Trivia Quest | Educational/Quiz | No |
| Word Quiz | Puzzle | No |
| Land of the Dead | Action/Survival | Yes — pathfinding for zombie enemies |

## Common Platform Features
- Centralized main menu for browsing/selecting games
- Fully offline gameplay — no internet or server dependency
- Touch-optimized controls for Android
- Local progress tracking via PlayerPrefs
- No ads, no in-app purchases (academic version)

## AI Techniques Used
- **Pathfinding** — enemy movement (Land of the Dead, Pesticide Escape)
- **Game decision-making** — Chess AI opponent moves
- **Behavior adaptation** — Racing Rivals opponent speed scaling
- Enemy AI follows a rule-based state machine: Idle → Patrol → Chase → Attack, based on player proximity and line of sight

## System Requirements
- Android 7.0 (API level 24) and above
- Touchscreen input (taps, swipes, drags)
- No internet connection required

## Project Results
- **Completion:** 100% (17/17 requirements fulfilled)
- **Accuracy:** 94%
- **Correctness:** 98%

## Future Work
- AI difficulty selection (Easy/Medium/Hard)
- Additional game modules
- Local multiplayer / score sharing via Bluetooth/Wi-Fi Direct
- Optional cloud backup
- Achievements, badges, daily challenges
- Tablet and landscape UI support
- Urdu localization
- APK size and battery optimization

## How to Run
1. Clone the repo
2. Open in Unity Hub
3. Add this folder as a project
4. Open the main menu scene and press Play


