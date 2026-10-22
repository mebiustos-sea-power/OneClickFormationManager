# One Click Formation Manager

Single-click a unit row or formation heading in Sea Power's formation manager to select the unit or formation leader and switch the camera, using the game's original selection command.

## Behavior

- Works with individual units and formation members in all six formation-manager tabs.
- Selection runs when the left mouse button is released over the same unit row. Movement of five pixels or more cancels the click, including movement away and back.
- Buttons, control-mode selectors, reorder handles and scrolling controls keep their native behavior. Dragging does not trigger the added selection action.
- Single-click the name area of a formation heading to select its leader and switch the camera. The heading stays highlighted and keeps its expanded or collapsed state.
- Double-click the name area of a formation heading to expand or collapse its members. The first click still selects the leader and switches the camera.
- Expansion arrows only expand or collapse the list, including repeated clicks. Right-click menus and existing double-click actions on unit rows remain available.
- A repeated click can restore the unit selection after a map contact has been selected, even when the camera already follows that unit.
- The mod works independently of Bug Fixes by MebiusTos. When that mod is enabled, its existing selection command is called and the map selection is synchronized only if still needed.

## Installation

Requires Anchor Chain. Copy the `OneClickFormationManager` folder from the package into `Sea Power_Data/StreamingAssets`. Enable Anchor Chain and One Click Formation Manager in the game's mod manager, then restart Sea Power. Disable this mod and restart to restore the original single-click behavior.

## Build and verification

Requires a .NET 10 SDK, the installed game and Anchor Chain. Copy `Directory.Build.local.props.example` to `Directory.Build.local.props` and set the dependency directories. No NuGet downloads are required.

This repository contains the mod source and build scripts only. Game assemblies and third-party dependency binaries are not included; the build references your locally installed copies.

```powershell
.\build.ps1 -Test
```

The completed package is written to `dist/OneClickFormationManager`. To install it locally while the game is closed:

```powershell
.\build.ps1 -Test -InstallLocal
```

Automated checks cover click/release behavior, drags, excluded or changed targets, repeated selections, command availability/failures and activation. A fresh process using the game's Mono runtime first installs the readiness hook with Noesis still uninitialized, then calls the game's NoesisUnity.InitCore and checks that formation hooks appear only after it returns. It also checks that the heading handler is left untouched until native UI attachment, callback exception containment, Harmony installation/removal and the installed Bug Fixes command wrapper when available. These checks do not initialize Unity or Noesis rendering. Actual mouse routing, visual hit testing, camera movement and mod coexistence must also be checked in the running game.

## Validation status

Built and tested against Sea Power 0.8.7. The .NET regression suite passed 22 checks; the separate game-Mono suite passed 27 checks, including the early startup sequence, callback failure recovery and the locally installed Bug Fixes command wrapper. No renderer was created by the automated tests. Manual in-game verification of version 0.1.3 also passed.

## Implementation

During Anchor Chain startup, the mod hooks only NoesisUnity.InitCore. After the game's core initialization returns successfully, it patches the formation manager's own selection behavior lifecycle and attaches input handlers there. The heading double-click handler is installed after the game's first native formation-list attachment, when its heading handler's game state is available. Managed exceptions at the mod entry points and UI callbacks are caught with ordinary catch (Exception) handlers and logged. Click detection, visual hit testing and unit selection are separate components. The native unit-selection command owns the camera action; the native map-selection transaction repairs stale selection only when required. No global camera methods or game assets are replaced.

Plugin ID: `mebiustos.oneclickformationmanager`. Version: `0.1.3`.
