# Slime Hub

A portrait slime playground. You keep a living blob in a glass laboratory tank, poke it until it stretches and splits, then spend the biomass it earns on looks, missions, and three short games.

Play it in the browser: [https://gugamazzotti.github.io/slime-hub/](https://gugamazzotti.github.io/slime-hub/)

The page hosts a Unity WebGL build from the `docs` folder. The first load downloads the game, then it runs locally in the tab. Progress is saved in the browser.

## What you do

The hub is a water tank in a cartoon lab. The slime is a soft fluid: drag it, squeeze it against the glass, and pull hard enough that it tears into pieces and sticks back together. A double-click makes it spin and blink. Space bar pinches it apart when you are playing with a mouse.

Biomass is the currency. Daily missions, minigame scores, and fusion spends are stored on the device. There is no account and no network backend.

Three minigames open from the hub:

- **Tube Escape.** Steer a slime down a glass tube, split around hazards, and squeeze through the narrow gaps.
- **Critical Mass.** Hold to inflate the blob inside a ring of blades. Grow it without popping.
- **Sticky Climb.** Pull the slime up sticky walls, launch it, and stay above the rising acid.

The interface follows the phone language: Portuguese, Spanish, or English.

## Controls

| Action | Touch | Mouse and keyboard |
| --- | --- | --- |
| Drag the slime | Press and move | Hold the left button and move |
| Delight | Double-tap the slime | Double-click the slime |
| Split | Pinch | Hold Space |
| Buttons | Tap | Click |

## Project layout

The game is a Unity 6000.3.26f1 project using the Universal Render Pipeline and the Input System. Open `Assets/_Project/Scenes/0_Boot.unity` and press Play. The hub, menus, and minigames are built in code when the scenes start.

- `Assets/_Project/Scenes` — boot, terrarium, and the three minigames
- `Assets/_Project/Scripts` — services, fluid, hub, interface, and minigames
- `Assets/_Project/Art` — laboratory art, the sprite sheet, and the slime shaders
- `docs` — the WebGL build served by GitHub Pages

## Build the web player

Install the same Unity editor with the WebGL module, then either use **File > Build Profiles** with the WebGL target, or run:

```text
Unity.exe -batchmode -nographics -quit -projectPath . -buildTarget WebGL -executeMethod SlimeHub.EditorTools.WebGLBuild.Build
```

That writes a Brotli build with decompression fallback into `docs`, so a static host such as GitHub Pages can serve it without custom compression headers. Publish the `docs` folder from the `main` branch to update the play link.
