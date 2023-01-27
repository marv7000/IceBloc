# IceBloc
Asset extractor for Frostbite games.

Supported games:
- Battlefield 3

WIP:
- Battlefield Hardline
- Battlefield 4

Supported RES types:
| Type | Formats |
| --- | --- |
| MeshSet | .obj .smd .semodel .atf |
| DxTexture | .dds |
| Metadata (EBX) | .txt (dump) |
| AssetBank | .txt (dump) |

Supported EBX types:
| Type | Formats |
| --- | --- |
| SoundWaveAsset | .wav  .flac |
| SkeletonAsset | .atf |

Supported AssetBank types:
| Type | Formats |
| --- | --- |
| FrameAnimation | .atf |
| RawAnimation | .atf |
| FrameAnimation | .atf |

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

# File format info
Most of Frostbite's assets are spread all over the place and are referenced dynamically. 

To combine e.g. a Mesh, Skeleton and an animation you need all those seperate files and combine them when loading in a 3D program.
I've created a format (AssetTransferFormat / .atf) for these types of assets which can be imported together or seperately. 

You can find importers for .atf files [here](https://github.com/marv7000/AssetTransferFormat).

# Build dependencies
- NET 7.0
- VS2022
