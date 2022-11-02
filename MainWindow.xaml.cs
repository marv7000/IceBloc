using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

using IceBloc.Utility;
using IceBloc.Frostbite2;

namespace IceBloc;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static List<AssetListItem>? Selection = new();
    public static DbObject? ActiveDataBaseObject;
    public static Catalog? ActiveCatalog;
    public static DbMetaData MetaData;
    public static List<AssetListItem> Assets = new();
    public static Game ActiveGame;

    public MainWindow()
    {
        /*
        using var reader = new BinaryReader(File.OpenRead(@"D:\repos\IceBloc\bin\Debug\net6.0-windows10.0.22621.0\Output\win32\weapons\ump45\ump45_soldierweaponbundle\d4f58568-f1dc-2d74-6f52-5e817688104b.chunk"));
        using var writer = new StreamWriter(File.OpenWrite(@"D:\test.obj"));
        writer.WriteLine("o test");
        while (reader.BaseStream.Position < 1344)
        {
            writer.WriteLine($"v {reader.ReadHalf()} {reader.ReadHalf()} {reader.ReadHalf()}");
            reader.BaseStream.Position += 10;
            writer.WriteLine($"vn {reader.ReadHalf()} {reader.ReadHalf()} {reader.ReadHalf()}");
            reader.BaseStream.Position += 10;
            writer.WriteLine($"vt {reader.ReadHalf()} {1f - (float)reader.ReadHalf()}");
            reader.BaseStream.Position += 12;
        }
        reader.BaseStream.Position = 1344;
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var face1 = reader.ReadUInt16();
            var face2 = reader.ReadUInt16();
            var face3 = reader.ReadUInt16();
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
        // Clear existing DbObject selection.
        ActiveDataBaseObject = null;

        if (Settings.GamePath != "")
        {
            if (isFolder)
            {
                ActiveCatalog = new(Settings.GamePath + "\\Data\\cas.cat");
                Assets = new();
            }
            else
            {
                // Read single toc file.
                ActiveDataBaseObject = DbObject.UnpackDbObject(Settings.GamePath);
                Assets = new();
                foreach (var element in ActiveDataBaseObject.Data as List<DbObject>)
                {
                    if (element.Name == "chunks" || element.Name == "bundles")
                    {
                        foreach (var asset in element.Data as List<DbObject>)
                        {
                            string idString = asset.GetField("path").Data as string;
                            AssetType type = (element.Name == "chunks") ? AssetType.Chunk : AssetType.Unknown;
                            object data = asset.GetField("totalSize").Data;

                            long size = 0;

                            // Check if we need to cast the read size to a long.
                            if (data is int var)
                                size = (int)data;
                            else if (data is long var1)
                                size = (long)data;

                            var chunks = asset.GetField("chunks").Data as List<DbObject>;
                            List<MetaDataObject> metaDataObjects = new();

                            for (int i = 0; i < chunks.Count; i++)
                            {
                                var  chunkSha = (asset.GetField("chunks").Data as List<DbObject>)[i].GetField("sha1").Data as byte[];
                                Guid chunkGuid = (Guid)(asset.GetField("chunks").Data as List<DbObject>)[i].GetField("id").Data;
                                long chunkSize = (long)(asset.GetField("chunks").Data as List<DbObject>)[i].GetField("size").Data;
                                metaDataObjects.Add(new MetaDataObject(chunkSha, chunkGuid, chunkSize));
                            }

                            Assets.Add(
                                new AssetListItem(
                                    idString is null ? "" : idString, // Name
                                    type, // AssetType
                                    size, // Size
                                    ExportStatus.Ready, // ExportStatus
                                    asset.ObjectType.ToString(), // Remarks
                                    metaDataObjects
                                    ));
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

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var assets = Assets;
        var output = new List<AssetListItem>();

        foreach(var asset in assets)
        {
            if (asset.Name.Contains(SearchBox.Text))
            {
                output.Add(asset);
            }
        }
        AssetGrid.ItemsSource = output;
    }
}
