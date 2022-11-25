﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

using IceBloc.Utility;
using IceBloc.Frostbite;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Shapes;
using IceBloc.Export;

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
    public static Dictionary<Guid, byte[]> ChunkTranslations = new();

    public static MainWindow Instance; // We need the instance to statically output messages.

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
    }

    public static void LoadAssets()
    {
        // Clear existing DbObject selection.
        ActiveDataBaseObject = null;

        // We just want to load the game folder.
        ActiveCatalog = new(Settings.GamePath + "\\Data\\cas.cat");
        Assets = new();

        string[] sbFiles = Directory.GetFiles(Settings.GamePath + "\\Data\\Win32\\", "*", SearchOption.AllDirectories);
        for (int i = 0; i < sbFiles.Length; i++)
        {
            if (!(sbFiles[i].Contains("en.toc") || sbFiles[i].Contains("en.sb")))
                LoadSbFile(sbFiles[i]);

            Instance.Dispatcher.Invoke(() => {
                Instance.ProgressBar.Value = ((double)i / (double)sbFiles.Length) * 100.0;
            });
        }
        Instance.Dispatcher.Invoke(() =>
        {
            UpdateItems();
        });
    }

    public static void LoadSbFile(string path)
    {
        ActiveDataBaseObject = DbObject.UnpackDbObject(path);
        foreach (var element in ActiveDataBaseObject.Data as List<DbObject>)
        {
            if (element.Name == "bundles")
            {
                foreach (DbObject asset in element.Data as List<DbObject>)
                {
                    LoadDbObject(asset, false);
                }
            }
            // If we have pure chunks.
            else if (element.Name == "chunks")
            {
                LoadDbObject(element, true);
            }
        }
        UpdateItems();
    }

    public static void LoadDbObject(DbObject asset, bool isChunks)
    {
        if (!isChunks)
        {
            // If we have RES information, use it.
            if (!(asset.GetField("res") == null || (asset.GetField("res").Data as List<DbObject>).Count == 0))
            {
                HandleResData(asset);
            }
            // If we have EBX information, use it.
            if (!(asset.GetField("ebx") == null || (asset.GetField("ebx").Data as List<DbObject>).Count == 0))
            {
                HandleEbxData(asset);
            }
            // If we have ChunkBundle information, use it.
            if (!(asset.GetField("chunks") == null || (asset.GetField("chunks").Data as List<DbObject>).Count == 0))
            {
                HandleChunkData(asset.GetField("chunks"));
            }
        }

        else
        {
            HandleChunkData(asset);
        }
    }

    public static void HandleEbxData(DbObject asset)
    {
        //TODO
    }

    public static void HandleResData(DbObject asset)
    {
        List<DbObject> resData = asset.GetField("res").Data as List<DbObject>;

        for (int i = 0; i < resData.Count; i++)
        {
            string idString = resData[i].GetField("name").Data as string;

            ResType type = (ResType)(int)resData[i].GetField("resType").Data;
            object data = resData[i].GetField("size").Data;
            byte[] sha = resData[i].GetField("sha1").Data as byte[];

            // Check if we need to cast the read size to a long.
            long size = 0;
            if (data is int var) size = (int)data;
            else if (data is long var1) size = (long)data;

            var item = new AssetListItem(idString, type, size, ExportStatus.Ready, sha);

            // Check if we already have an asset with that name (Some RES are defined multiple times).
            item.GetHashCode();
            if ((Assets.Find(x => x.Name == idString) == null))
                Assets.Add(item);
        }
    }

    public static void HandleChunkData(DbObject asset)
    {
        var chunks = asset.Data as List<DbObject>;
        for (int i = 0; i < chunks.Count; i++)
        {
            try
            {
                var chunkSha = chunks[i].GetField("sha1").Data as byte[];
                Guid chunkGuid = (Guid)chunks[i].GetField("id").Data;

                // Add the chunk to the database. If we fail, it means that we have a duplicate chunk.
                // In this case, check if the new one is larger. If yes, replace it.
                if(!ChunkTranslations.TryAdd(chunkGuid, chunkSha))
                {

                }
            }
            catch(Exception e)
            {
                WriteUIOutputLine(e.Message);
            }
        }
    }

    #region UI

    public static void UpdateItems()
    {
        Instance.Dispatcher.Invoke(() =>
        {
            Instance.AssetGrid.ItemsSource = null;
            Instance.LoadedAssets.Content = "Loaded Assets: " + Assets.Count;
            Instance.AssetGrid.ItemsSource = Assets;
            Instance.AssetGrid.Refresh();
        });
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Selection = AssetGrid.SelectedItems as List<AssetListItem>;
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
        Thread thr = new Thread(LoadAssets);
        thr.Start();
    }

    private void ExportAsset_Click(object sender, RoutedEventArgs e)
    {
        foreach (AssetListItem selected in Instance.AssetGrid.SelectedItems)
        {
            selected.Export();
        }
        string word = Instance.AssetGrid.SelectedItems.Count == 1 ? "Asset" : "Assets";
        MessageBox.Show($"Exported {Instance.AssetGrid.SelectedItems.Count} {word}.", "Success", MessageBoxButton.OK);
        UpdateItems();
    }

    public static void WriteUIOutput(string message)
    {
        Instance.ConsoleOutput.Text += message;
    }
    public static void WriteUIOutputLine(string message)
    {
        WriteUIOutput("\n" + message);
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
    #endregion

    #region Settings
    private void ExportConvertedBox_Checked(object sender, RoutedEventArgs e)
    {
        Settings.ExportConverted = true;
    }
    private void ExportConvertedBox_UnChecked(object sender, RoutedEventArgs e)
    {
        Settings.ExportConverted = false;
    }
    private void ExportRawBox_Checked(object sender, RoutedEventArgs e)
    {
        Settings.ExportRaw = true;
    }
    private void ExportRawBox_UnChecked(object sender, RoutedEventArgs e)
    {
        Settings.ExportRaw = false;
    }
    #endregion

    private void MeshFormatBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MeshFormatBox.SelectedIndex == 0)
            Settings.CurrentModelExporter = new ModelExporterOBJ(); // OBJ
        if (MeshFormatBox.SelectedIndex == 1)
            Settings.CurrentModelExporter = new ModelExporterSMD(); // SMD
    }

    private void TextureFormatBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

        if (TextureFormatBox.SelectedIndex == 0)
            Settings.CurrentTextureExporter = new TextureExporterDDS();
    }
}
