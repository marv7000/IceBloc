# IceBloc
Asset extractor for Frostbite 2 games.

Supported games:
- Battlefield 3
- Medal of Honor: Warfighter (Some textures are broken)

## Help, improvements and PRs are welcome!

Supported RES types:
| Type | Formats |
| --- | --- |
| MeshSet | .obj .smd .semodel |
| DxTexture | .dds |
| Metadata (EBX) | .txt (dump) |
| AssetBank (Animations) | .txt (dump) |

Supported EBX types:
| Type | Formats |
| --- | --- |
| SoundWaveAsset | .wav |
| SkeletonAsset | .smd |

Supported AssetBank types:
| Type |
| --- |
| FrameAnimation |
| RawAnimation | 
| DctAnimation | 

# Usage
- Open the program and click the Load Game button on the bottom right.
- Select your game install folder (e.g. ``"C:/SteamLibrary/steamapps/common/Battlefield 3"``).
- Wait for all assets to load, then select the assets you wish to export and hit "Export Selection".
- If you wish to extract unsupported asset types, go to the Settings tab and enable Raw Export.

# Exporting Skinned Meshes/Animations
- Select and export the SkeletonAsset you want to use
- Export the Mesh/Animation you want to export

# Notes
- Exporting animations is slow because the tool has to do a reverse lookup to find some assets needed to resolve channel names.
- Loading a game can take some time because all asset types need to be resolved prior to export

# CLI
IceBloc also comes with a (basic) console tool if you prefer to use that over the UI.

These commands are available:
- ``load game <path>`` Loads all game files.
- ``load file <path>`` Loads a single file.
- ``dump <ResType> <path>`` Dumps all contents of a raw file to a text file.
- ``setgame <name>`` Manually override the current game (See ``Settings.cs`` for valid names).
- ``hash <string>`` Returns a FNV1 hash from any given string
- ``select <name>`` Select an asset for export
- ``export`` Export the selection

# TODO
- Support Patch/DLCs
- Support NonCas bundles (e.g. voice lines in BF3)

# Build dependencies
- NET 7.0
- VS2022
