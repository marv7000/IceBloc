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
    public static List<AssetListItem> Selection = new();
    public static DbObject ActiveDataBaseObject;
    public static Catalog ActiveCatalog;
    public static DbMetaData MetaData;
    public static List<AssetListItem> Assets = new();
    public static Game ActiveGame;

    public MainWindow()
    {
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

        if (isFolder)
        {
            // We just want to load the game folder.
            ActiveCatalog = new(Settings.GamePath + "\\Data\\cas.cat");
            Assets = new();
        }
        if (!isFolder && Settings.GamePath != string.Empty)
        {
            ActiveDataBaseObject = DbObject.UnpackDbObject(Settings.GamePath);
            Assets = new();
            foreach (var element in ActiveDataBaseObject.Data as List<DbObject>)
            {
                if (element.Name == "chunks" || element.Name == "bundles")
                {
                    foreach (DbObject asset in element.Data as List<DbObject>)
                    {
                        LoadDbObject(asset);
                    }
                }
            }
            LoadedAssets.Content = "Loaded Assets: " + Assets.Count;
            AssetGrid.ItemsSource = Assets;
        }
    }

    public void LoadDbObject(DbObject asset)
    {
        // If this DbObject doesn't contain any RES information, it's useless to us.
        if (asset.GetField("res") == null)
            return;
        if ((asset.GetField("res").Data as List<DbObject>).Count == 0)
            return;

        List<DbObject> resData = asset.GetField("res").Data as List<DbObject>;

        for (int i = 0; i < resData.Count; i++)
        {
            string idString = resData[i].GetField("name").Data as string;

            ResType type = (ResType)(int)resData[i].GetField("resType").Data;
            object data = resData[i].GetField("size").Data;

            long size = 0;

            // Check if we need to cast the read size to a long.
            if (data is int var) size = (int)data;
            else if (data is long var1) size = (long)data;

            List<DbObject> chunkData = new();

            Assets.Add(new AssetListItem(
                idString is null ? "" : idString, // Name
                type, // AssetType
                size, // Size
                ExportStatus.Ready, // ExportStatus
                HandleData(asset, chunkData)
                ));
        }
    }

    public List<MetaDataObject> HandleData(DbObject asset, List<DbObject> chunks)
    {
        List<MetaDataObject> metaDataObjects = new();

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunkSha = (asset.GetField("chunks").Data as List<DbObject>)[i].GetField("sha1").Data as byte[];
            Guid chunkGuid = (Guid)(asset.GetField("chunks").Data as List<DbObject>)[i].GetField("id").Data;
            long chunkSize = (long)(asset.GetField("chunks").Data as List<DbObject>)[i].GetField("size").Data;
            metaDataObjects.Add(new MetaDataObject(chunkSha, chunkGuid, chunkSize));
        }

        return metaDataObjects;
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
