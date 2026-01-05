# 🐍 Snake 2D Co-Op

A polished **2D Snake game built in Unity**, supporting **single-player and local co-op**, with modern gameplay features such as power-ups, head-to-head collision rules, animations, particles, and a robust UI flow.

---

## 🎮 Gameplay Video

▶️ **Watch Gameplay Here:**  
(https://youtu.be/3-QJ4FmgIxU)

---

## 🎮 Game Modes

### Single Player
- Control the snake using **WASD or Arrow keys**
- Reach the target score to win
- Avoid walls, self-collision, and penalties

### Two Player (Local Co-Op)
- **Player 1**: WASD  
- **Player 2**: Arrow keys  
- Competitive gameplay with **head-to-head collision rules**

---

## ✨ Core Features

### 🧠 Movement & Controls
- Grid-based snake movement
- Buffered input system (prevents instant 180° turns)
- Smooth body growth and shrink mechanics

### 🛡️ Power-Ups
- **Shield** – Blocks one fatal collision
- **Score Boost** – Doubles food score temporarily
- **Speed Boost** – Increases snake speed for a short duration

---

## ⚔️ Head-to-Head Collision Rules

When both snakes collide head-to-head:

- If **one snake has a shield**, the **other snake dies**
- If **no snake has a shield**, the **higher score wins**
- If **scores are tied**, **both snakes die**
- Head-to-head collisions are tracked centrally for accurate result resolution

---

## 🎥 Visual & Feedback Systems

### Head Hit Animation
- Plays on collisions with wall, self, or other snake
- Triggers **regardless of shield state**

### Dizzy Stars Particle Effect
- Plays on every collision
- Provides immediate visual feedback even when shield absorbs damage

- Correct sprite rotation after collision recovery

---

## 🔊 Audio System

- Collision / head-hit sound
- Shield block sound
- Player death sound
- Win sound
- UI button click sound

---

## 🧾 UI & Game Flow

- Dynamic HUD for single-player and two-player modes
- Reliable **Result Screen** with win / lose / draw states
- Fixed button interaction issues
- Prevents duplicate result screen triggers

---

## 🏗️ Architecture Overview

### Key Systems
- **GameController** – Central game state, scoring, collision resolution
- **SnakeController** – Movement, input, collisions, power-ups
- **GameResultController** – Result UI and winner resolution

---

## 🛠️ Tech Stack

- Unity (2D)
- C#
- TextMeshPro
- Animator Controller
- Particle System
- AudioSource-based Sound Manager

---

## 🚧 Polish Branch Highlights

- Improved single-player input handling
- Collision animation and particle feedback
- Head-to-head rule stabilization
- Result menu reliability fixes
- Audio feedback consistency improvements

---

## 👤 Author

**Viresh Jadhav**  
Game Developer | Unity | C#
