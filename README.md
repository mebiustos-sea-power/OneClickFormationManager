# One Click Formation Manager

Single-click a unit row in Sea Power's formation manager to select the unit and switch the camera, using the game's original double-click command.

## Behavior

- Works with individual units and formation members in all six formation-manager tabs.
- Selection runs when the left mouse button is released over the same unit row. Movement of five pixels or more cancels the click, including movement away and back.
- Buttons, control-mode selectors, reorder handles and scrolling controls keep their native behavior. Dragging does not trigger the added selection action.
- Formation headings retain their native double-click action. Right-click menus and existing double-click actions remain available.
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

Automated checks cover click/release behavior, drags, excluded or changed targets, repeated selections, command availability/failures and activation. A fresh process using the game's Mono runtime first installs the readiness hook with Noesis still uninitialized, then calls the game's NoesisUnity.InitCore and checks that formation hooks appear only after it returns. It also checks callback exception containment, Harmony installation/removal and the installed Bug Fixes command wrapper when available. These checks do not initialize Unity or Noesis rendering. Actual mouse routing, visual hit testing, camera movement and mod coexistence must also be checked in the running game.

## Validation status

Built and tested against Sea Power 0.8.7. The .NET regression suite passed 22 checks; the separate game-Mono suite passed 27 checks, including the early startup sequence, callback failure recovery and the locally installed Bug Fixes command wrapper. No renderer was created. In-game verification is pending.

## Implementation

During Anchor Chain startup, the mod hooks only NoesisUnity.InitCore. After the game's core initialization returns successfully, it patches the formation manager's own selection behavior lifecycle and attaches input handlers there. Managed exceptions at the mod entry points and UI callbacks are caught with ordinary catch (Exception) handlers and logged. Click detection, visual hit testing and unit selection are separate components. The native unit-selection command owns the camera action; the native map-selection transaction repairs stale selection only when required. No global camera methods or game assets are replaced.

Plugin ID: `mebiustos.oneclickformationmanager`. Version: `0.1.1`.
