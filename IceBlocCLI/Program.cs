using IceBloc;
using IceBloc.Utility;
using IceBlocLib.Frostbite;
using IceBlocLib.Frostbite2;
using IceBlocLib.Frostbite2.Animations.Base;
using IceBlocLib.Frostbite2.Textures;
using IceBlocLib.Utility;
using System.Globalization;
using System.Numerics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IceBlocCLI;

public class Program
{
    public static AssetListItem Selection = new("No selection", "AssetBank", InternalAssetType.Unknown, 0, ExportStatus.Error, null);

    static void Main(string[] args)
    {
        CultureInfo.CurrentCulture= CultureInfo.InvariantCulture;
        string command = "";
        while(command != "exit")
        {
            Console.Write("> ");
            command = ParseCommand();
        }
        Environment.Exit(0);
    }

    public static string ParseCommand()
    {
        string[] cmd = IO.SplitLiteral(Console.ReadLine());
        for (int i = 0; i < cmd.Length; i++)
        {
            cmd[i] = cmd[i].Replace("\"", "");
        }
        if (cmd.Length > 0)
        {
            switch (cmd[0])
            {
                case "load":
                    if (cmd[1] == "game")
                    {
                        Settings.GamePath = cmd[2];
                        IO.LoadGame();
                    }
                    else if (cmd[1] == "file")
                        IO.LoadSbFile(cmd[2], true);
                    break;
                case "dump":
                    if (!Enum.TryParse(cmd[1], out ResType type))
                        Console.WriteLine("Unrecognized RES type.");
                    Dump(type, cmd[2]);
                    break;
                case "select":
                    if (cmd.Length == 1)
                        Console.WriteLine("Current selection is: " + Selection.Name);
                    else
                        SelectAsset(cmd[1]); break;
                case "export":
                    Selection.Export(); break;
                case "setgame":
                    if (cmd.Length == 2)
                    {
                        if (!Enum.TryParse(cmd[1], out Settings.CurrentGame))
                            Console.WriteLine("Unrecognized game.");
                    }
                    else
                        Console.WriteLine("Current Game: " + Settings.CurrentGame); break;
                case "hash":
                    Console.WriteLine(Ebx.GetHashCode(cmd[1])); break;
                case "setflag":
                    if (cmd[1] == "ExportRaw")
                        Settings.ExportRaw = cmd[2] == "1";
                    if (cmd[1] == "ExportConverted")
                        Settings.ExportConverted = cmd[2] == "1";
                    break;
                case "find":
                    foreach (var a in IO.Assets)
                    {
                        if (a.Key.Item1.Contains(cmd[1]))
                            Console.WriteLine(a.Key);
                    } break;
                case "compile":
                    if (cmd[1] == "EBX")
                        Dbx.Import(cmd[2]);
                    break;
                case "decrypt":
                    IO.DecryptAndCache(cmd[1]); break;
                case "anim":
                    if (cmd.Length == 3)
                    {
                        var skel = new Dbx(cmd[2]);

                        FixupAnimation(cmd[1], skel);
                    }
                    else
                        Console.WriteLine("Fixes an animation using a skeleton");
                        Console.WriteLine("Usage: anim <path to animation> <path to target skeleton>");
                    break;
                default: break;
            }
            return cmd[0];
        }
        return "";
    }

    public static void FixupAnimation(string anim, Dbx dbx)
    {
        var boneNames = new List<string>();
        var boneHierarchy = new List<int>();
        var localPose = new List<Transform>();

        string name = (string)dbx.Prim["$"]["$"]["Name"].Value;
        var b = dbx.Prim["BoneNames"];
        foreach (var boneName in (b.Value as IceBlocLib.Frostbite.Complex).Fields)
        {
            boneNames.Add((string)boneName.Value);
        }

        b = dbx.Prim["Hierarchy"];
        foreach (var index in (b.Value as IceBlocLib.Frostbite.Complex).Fields)
        {
            boneHierarchy.Add((int)index.Value);
        }

        
    }

    public static void SelectAsset(string asset)
    {
        IO.Assets.TryGetValue((asset, InternalAssetType.RES), out Selection);
        if (Selection is null)
        {
            Selection = new("Invalid selection", "AssetBank", InternalAssetType.Unknown, 0, ExportStatus.Error, null);
        }
    }

    public static void Dump(ResType type, string path)
    {
        switch (type)
        {
            case ResType.EBX:
                var dbx = new Dbx(path);
                dbx.Dump(IO.EnsurePath(path, "_dump.txt"));
                break;
            case ResType.AssetBank:
                GenericData.DumpAssetBank(path); break;
            case ResType.DxTexture:
                {
                    using var r = new BinaryReader(File.OpenRead(path));
                    DxTexture d = new DxTexture(r);
                    IO.Dump(d, path);
                } break;
        }
    }
    
    
}