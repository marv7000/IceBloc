using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

using IceBloc.Utility;
using IceBloc.Frostbite.Texture;
using IceBloc.Frostbite.Mesh;
using IceBloc.Frostbite.Packed;

namespace IceBloc;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static List<AssetListItem>? Selection = new();
    public static DbObject? ActiveDbo;
    public static Catalog? ActiveCatalog;
    public static DbMetaData? MetaData;
    public static List<AssetListItem> Assets = new();
    public static Game ActiveGame;

    public MainWindow()
    {
        /*
        using var reader = new BinaryReader(File.OpenRead(@"D:\Tools\Dumps\BF3\chunks\e713f92f-d49e-f0e7-87fa-57a356ce7f11.chunk"));
        using var reader2 = new BinaryReader(File.OpenRead(@"D:\indices.dat"));
        using var writer = new StreamWriter(File.OpenWrite(@"D:\test.obj"));
        writer.WriteLine("o test");
        while (reader.BaseStream.Position < 289536L)
        {
            writer.WriteLine($"v {reader.ReadHalf()} {reader.ReadHalf()} {reader.ReadHalf()}");
            reader.BaseStream.Position += 10;
            writer.WriteLine($"vn {reader.ReadHalf()} {reader.ReadHalf()} {reader.ReadHalf()}");
            reader.BaseStream.Position += 10;
            writer.WriteLine($"vt {reader.ReadHalf()} {reader.ReadHalf()}");
            reader.BaseStream.Position += 12;
        }

        while (reader2.BaseStream.Position < reader2.BaseStream.Length)
        {
            var face1 = reader2.ReadUInt16();
            var face2 = reader2.ReadUInt16();
            var face3 = reader2.ReadUInt16();
            writer.WriteLine($"f {face1 + 1}/{face1 + 1}/{face1 + 1} {face2 + 1}/{face2 + 1}/{face2 + 1} {face3 + 1}/{face3 + 1}/{face3 + 1}");
        }
        */
        InitializeComponent();
    }

    // UI

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Selection = AssetGrid.SelectedItems as List<AssetListItem>;
    }

    public void LoadAssets(bool isFolder)
    {
        ActiveDbo = null;
        if (Settings.GamePath != "")
        {
            if(isFolder)
            {
                // We probably want to read all files.
                using (var reader = new BinaryReader(File.OpenRead(Settings.GamePath + "\\Data\\cas.cat")))
                    ActiveCatalog = new Catalog(reader);
                Assets = new();
            }
            else
            {
                // Read single toc file.
                ActiveDbo = DbObject.UnpackDbObject(Settings.GamePath);
                Assets = new();
                foreach (var element in ActiveDbo.Data as List<DbObject>)
                {
                    if (element.Name == "chunks" || element.Name == "bundles")
                    {
                        foreach (var asset in element.Data as List<DbObject>)
                        {
                            string idString = asset.GetField("id").Data.ToString();
                            AssetType type = (element.Name == "chunks") ? AssetType.Chunk : AssetType.Unknown;
                            object data = asset.GetField("size").Data;

                            long size = 0;

                            if (data is int var)
                                size = (long)(int)data;
                            else if (data is long var1)
                                size = (long)data;

                            Assets.Add(new AssetListItem(idString, type, size, ExportStatus.Ready, asset.ObjectType.ToString(), null));
                        }
                    }
                }
            }
            LoadedAssets.Content = "Loaded Assets: " + Assets.Count;
            AssetGrid.ItemsSource = Assets;
        }
    }

    public void UpdateItems()
    {
        AssetGrid.ItemsSource = null;
        AssetGrid.ItemsSource = Assets;
    }

    private void LoadInstall_Click(object sender, RoutedEventArgs e)
    {
        CommonOpenFileDialog dialog = new CommonOpenFileDialog();
        dialog.InitialDirectory = "C:\\Users";
        dialog.IsFolderPicker = true;
        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            Settings.GamePath = dialog.FileName;
        }
        LoadAssets(true);
    }

    private void LoadSingle_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new OpenFileDialog();
        dialog.InitialDirectory = "C:\\Users";
        if (dialog.ShowDialog() == true)
        {
            Settings.GamePath = dialog.FileName;
        }
        LoadAssets(false);
    }

    private void ExportAsset_Click(object sender, RoutedEventArgs e)
    {
        foreach(AssetListItem selected in AssetGrid.SelectedItems)
        {
            selected.Export();
        }

        UpdateItems();
    }
}
