# IceBloc
Asset extractor for Frostbite games.

Supported games:
- Battlefield 3
- Battlefield Hardline
- Battlefield 4

Supported asset types:
| Type | Formats |
| --- | --- |
| Mesh | .obj .smd .semodel .atf |
| Texture | .dds |
| Animation | .smd .seanim .atf |
| Sound | .wav  .flac |
| Metadata (EBX) | .txt |

# Usage
- Open the program and click the Load Game button on the bottom right.
- Select your game install folder (e.g. ``"C:/SteamLibrary/steamapps/common/Battlefield 3"``).
- Wait for all assets to load, then select the assets you wish to export and hit "Export Selection".
- If you wish to extract unsupported asset types, go to the Settings tab and enable Raw Export.

# CLI
IceBloc also comes with a console tool if you prefer to use that over the UI.

These commands are available:
- ``load game <path>`` Loads all game files.
- ``load file <path>`` Loads a single file.
- ``dump <ResType> <path>`` Dumps all contents of a raw file to a text file.
- ``setgame <name>`` Manually override the current game (See ``Settings.cs`` for valid names).
- ``hash <string>`` Returns a FNV1 hash from any given string
- ``select <name>`` Select an asset for export
- ``export`` Export the selection
# Build dependencies
- NET 7.0
- VS2022
