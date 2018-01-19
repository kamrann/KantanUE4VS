# Kantan UE4VS #

Visual Studio extension providing various UE4 coding related functionality.

UE4 Code Elements
---
Accessed via View | Other Windows | UE4 Code Elements, default shortcut Ctrl+Alt+U; or through toolbar 'KantanUE4VS'.

Currently supports adding types (UCLASS, USTRUCT, UINTERFACE, raw C++ class/struct), empty source files shell, modules (sets up build file and basic module boilerplate code, and updates .uproject/.uplugin) and plugins (just adds a plugin folder and descriptor, after which you can add modules).

UE4 Property Visualizer
---
Does truly evil things to enable viewing of UE4 properties in the VS watch windows when debugging. This lets you examine contents of Blueprint variables inside UObjects when stepping through C++ code.

NOTE: I released this as its own extension previously. It's now updated for VS 2017 and appears to be more stable than before, but be warned there may still be issues, especially if low level changes are made to UE4 engine types in new UE4 versions. It can also be somewhat slow. Can be disabled from the extension options page.

Extension Options
---
A few configuration settings are available via Tools | Options | KantanUE4VS.

Current issues and limitations
---
- The Add Code Elements UI needs proper validation and feedback. If things aren't getting added, check the VS Output pane for any messages.
- Currently a limited list of base classes for UClass additions.
- C++ namespacing not yet supported.
- The UI is a bit rubbish. Anyone who knows anything about WPF and wants to help clean it up, or anyone who feels like making a few icons for the extension and toolbar, please get in touch!
- For now code element addition only works with game projects (you can't add elements to the UE4 project in a source engine build solution), and is tied to the current startup project.
