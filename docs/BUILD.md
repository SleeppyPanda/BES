# Build Instructions (US-069)

## Prerequisites
- Unity 6000.3.16f1 or newer (Unity 6 LTS)
- Windows 10/11 for target demo build

## First-Time Setup
1. Open project in Unity Hub
2. Run menu: **BES → Setup Project (US-001 to US-006)**
3. Wait for script compile and scene generation
4. Open `Assets/_Project/Scenes/MainMenu.unity` and press Play

## Windows Build
1. File → Build Settings
2. Scenes (in order):
   - `Assets/_Project/Scenes/MainMenu.unity`
   - `Assets/_Project/Scenes/Gameplay.unity`
   - `Assets/_Project/Scenes/PrototypeScene.unity`
3. Platform: PC, Mac & Linux Standalone → Windows
4. Build

## Performance Targets (US-066)
- 30+ FPS on integrated GPU at 1080p for demo scenes
- Reduce shadow distance and MSAA if below target

## Known MVP Limitations
- NavMesh not required; enemies use direct movement fallback
- AI dialogue uses offline fallback unless API key configured
- 3 regions are blockout scale, not full open world
