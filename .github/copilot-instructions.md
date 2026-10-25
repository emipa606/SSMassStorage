# GitHub Copilot Instructions for [SS] Mass Storage (Continued)
## Mod Overview and Purpose
[SS] Mass Storage (Continued) is an endgame storage solution for RimWorld. It allows players to store up to 2 147 483 647 items of the same kind in a single building. This mod is a continuation and update of Spdskatrs' original mod.
## Key Features and Systems
- **Massive Storage Capacity**: [SS] Mass Storage can store up to 2 147 483 647 items of the same kind internally and an additional 300 items on top of itself.
- **Power Consumption**: The power consumption of the storage device increases exponentially as more items are stored in it.
- **Integration with Stockpiles**: The storage device can be placed on top of a stockpile and will automatically suck in items that are allowed into it.
- **Settings Customization**: Players can customize the storage settings of the device to control which items are accepted.
## Coding Patterns and Conventions
### C# Files
- **OnStartup.cs**: This file contains the initialization code for the mod, including the Harmony patches.
- **Building_MassStorageDevice.cs**: This file contains the code for the storage device itself, including methods for accepting items, checking if storage is invalid, collecting applicable items, despawning, dropping all items, exposing data, getting gizmos, getting inspect string, getting parent store settings, getting store settings, getting internal stored def, items stored externally, max count, notifying settings changed, post-make, power trader comp, residing zone, storage tab visible, stored is rottable, tick, tick rottables, ticks until rot at current temp, and trying to spawn items at output.
### XML Files
- **Research.xml**: This file contains the research project definition for the storage device.
- **Buildings.xml**: This file contains the building definition for the storage device.
- **ImpliedDefs.xml**: This file contains implied definitions for the storage device.
## XML Integration
The mod uses XML files to define the research project and building for the storage device. The XML files are located in the **TranslationTemplate** folder and are named **Research.xml**, **Buildings.xml**, and **ImpliedDefs.xml**.
## Harmony Patching
The mod uses Harmony patches to modify the behavior of the game. The Harmony patches are located in the **OnStartup.cs** file and are named **Patcher** and **resourceCounterPostfix**.
## Suggestions for Copilot
When using GitHub Copilot with [SS] Mass Storage (Continued), it is recommended to:
- Focus on the C# files, as they contain the most complex code.
- Pay attention to the XML files, as they define the mod's behavior and integration with the game.
- Use the Harmony patches as a reference for modifying the game's behavior.
- Use the mod's research and building definitions as a reference for creating new research projects and buildings.
- Use the mod's code as a reference for creating new features and systems.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

