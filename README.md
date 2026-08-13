# MetaPlay

A Unity-based multi-game offline platform for Android that packages 7 distinct mini-games into a single app under one unified launcher.

## Description
MetaPlay lets players browse and switch between multiple 2D and 3D games from a single main menu, all running fully offline with no ads or in-app purchases. Several games integrate Artificial Intelligence to increase realism and interactivity.

## Built With
- Unity (Game Engine)
- Blender
- C#
- Sketchfab
- Mixamo
- Stockfish (Chess engine, integrated locally)

## Games Included
| Game | Genre | AI |
|---|---|---|
| Chess AI | Strategy | Stockfish + custom bot, multiple AI modes |
| Racing Rivals | Racing | Adaptive opponent behavior |
| Survival.io | Survival | Scripted spawns (no AI) |
| Pesticide Escape | Action / FPS | AI navigation for insect enemies |
| Trivia Quest | Educational / Quiz | None |
| Word Quiz | Puzzle | None |
| Land of the Dead | Action / Survival | Pathfinding for zombie enemies |

## Platform Features
- Centralized main menu for browsing and selecting games
- Fully offline gameplay — no internet or server dependency
- Touch-optimized controls for Android
- Local progress tracking via PlayerPrefs
- Ad-free, no in-app purchases

## AI Techniques Used
- **Pathfinding** — enemy movement (Land of the Dead, Pesticide Escape)
- **Game decision-making** — Chess AI opponent moves
- **Behavior adaptation** — Racing Rivals opponent speed scaling
- Enemy AI follows a rule-based state machine: Idle → Patrol → Chase → Attack, based on player proximity and line of sight

## System Requirements
- Android 7.0 (API level 24) and above
- Touchscreen input (taps, swipes, drags)
- No internet connection required

## Roadmap
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

## Status
Completed — actively maintained and improved.
