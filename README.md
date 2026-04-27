# Runix Launcher

**Runix Launcher** is a lightweight Linux-focused game launcher that makes it easy to manage and launch your games from one place.

It integrates with your **Steam library**, allows you to select a runtime per game, and uses tags for simple organisation. Custom (non-Steam) games are supported, and cover images can be automatically generated using the built-in overlay system.

Built with **Avalonia**, Runix Launcher is cross-platform by design, but primarily developed and tested on **Linux**.

## Features

-  Steam library integration  
-  Per-game runtime selection  
-  Tag-based organisation  
-  Automatic cover generation for imported games
-  Native sandboxing support

## Screenshots

<img width="1671" height="892" alt="Main library view" src="https://github.com/user-attachments/assets/01499658-6e39-4223-bbf6-0b074b4bf097" />
<img width="1921" height="1081" alt="Game details" src="https://github.com/user-attachments/assets/0a215d25-9bb9-4c11-a589-87af4ef61ba6" />
<img width="1920" height="1081" alt="Settings view" src="https://github.com/user-attachments/assets/f3394608-cb8f-4069-9d20-459ea2d06ec1" />

## Prerequisites

### Required

- **.NET SDK**
- **Rust**

### Overlay Support (Linux)

The image generation overlay requires `libgtk-layer-shell`:

```sh
sudo apt install libgtk-layer-shell0
```

## Installation
1. Clone the repository
```sh
git clone https://github.com/NeXx42/RunixLauncher.git --recursive
```
3. Build the AppImage
```sh
cd RunixLauncher
make publish-appimage
```
5. Run
```sh
./Build/Output/RunixLauncher.appimage
```

## Notes
While the launcher is cross-platform, functionality outside Linux may be incomplete or untested.
Steam must be installed for library integration.
