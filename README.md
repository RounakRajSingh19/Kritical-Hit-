# 🥊 Mystic Kombat 2D

**A 2D pixel-art fighting game built with Unity.**

Choose your fighter, enter the arena, and battle using punches, kicks, special attacks, and defensive moves. Mystic Kombat 2D combines pixel-art visuals with fast-paced combat and a variety of fighters and stages.

## 🎮 Overview

Mystic Kombat 2D is a 2D fighting game developed in Unity using C#. The project focuses on character-based combat, fighter selection, arena battles, and a combination of offensive and defensive abilities.

## ✨ Features

- **14 Fighters:** Choose from a roster of playable characters.
- **6 Stages/Arena:** Battle across different arena environments.
- **Character Selection:** Select your fighter before entering combat.
- **Versus Battles:** Compete against an opponent.
- **Combat Mechanics:** Punch, kick, crouch, jump, block, and perform special attacks.
- **Advanced Attacks:** Perform uppercuts, low attacks, and airborne attacks.
- **Health System:** Track fighter health during a match.
- **Special-Power Meter:** Build power during combat and use it for special moves.
- **Round-Based Combat:** Matches use a best-of-three round format.
- **Pixel-Art Graphics:** Character sprites and stage backgrounds create a retro fighting-game aesthetic.
- **Audio:** Sound effects and background music enhance the gameplay.
- **Runtime Initialization:** Game objects and gameplay systems are initialized through code, reducing the need for manual scene setup.

## 🕹️ Controls

| Action | Player 1 | Player 2 |
|---|---|---|
| Move | `A` / `D` | `←` / `→` |
| Jump | `W` | `↑` |
| Crouch | `S` | `↓` |
| Punch | `F` | `,` |
| Kick | `G` | `.` |
| Block | `H` | `/` |
| Special | `T` | `;` |

### Advanced Moves

- **Uppercut:** Jump and punch (`W` + `F`).
- **Low Attack:** Crouch and punch (`S` + `F`).
- **Jump Attacks:** Punch or kick while airborne.
- **Special Dash:** Use the Special key to perform a dash attack, consuming special power.

### Menu Controls

- `Enter` — Confirm or start.
- `Esc` — Go back or exit the current match.

## 🛠️ Built With

- **Unity** — Game engine.
- **C#** — Gameplay programming.
- **Universal Render Pipeline (URP)** — Rendering pipeline.
- **JSON** — Fighter and stage configuration.
- **Pixel-Art Assets** — Character sprites and arena backgrounds.

## 🚀 Getting Started

### Prerequisites

- Unity Editor compatible with the project's configured Unity version.
- A Unity project configured to use the Universal Render Pipeline (URP).

### Installation

1. Clone or download this repository.
2. Open the project in Unity.
3. Allow Unity to import the assets and compile the scripts.
4. Check the Console window for compilation errors.
5. Open the appropriate Game view or scene and press **Play**.

The game is designed to initialize gameplay objects at runtime, minimizing the need for manual scene configuration.

## 📁 Project Structure

```text
Assets/
├── Editor/
│   └── PixelImport.cs
├── Resources/
│   ├── Sheets/          # Fighter sprite sheets
│   ├── Stages/          # Stage backgrounds
│   ├── Sounds/          # Music and sound effects
│   ├── chars.json       # Fighter data and animation configuration
│   └── stages.json      # Stage configuration
└── Scripts/
    ├── Game.cs          # Main game logic and combat flow
    ├── Fighter.cs       # Fighter movement, attacks and hit detection
    └── Data.cs          # Loads game data, sprites and sounds
```

## 👥 Team & Credits

Mystic Kombat 2D was developed collaboratively by:

- **Rounak Raj Singh — Game Development & Integration Lead:** Core game development, player movement, controller integration, and overall project integration.
- **Satvik — Combat Mechanics & Game Logic:** Attack mechanics, health and damage systems, hit detection, and match logic.
- **Sarah — UI/UX & Visual Assets:** User interface, combat HUD, health bars, visual assets, and overall gameplay experience.



**Mystic Kombat 2D — Choose your fighter. Master your moves. Fight for victory.** 🥊
