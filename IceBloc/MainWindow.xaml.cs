using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Globalization;
using System.Threading;

using IceBloc.Utility;
using System.Threading.Tasks;
using IceBlocLib.Utility;
using IceBlocLib;
using System.Linq;
using IceBlocLib.Utility.Export;

namespace IceBloc;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static List<AssetListItem> Selection = new();
    public static MainWindow Instance; // We need the instance to statically output messages.

    public MainWindow()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Instance = this;
        InitializeComponent();
    }

    #region Asset Loading

    public static void LoadAssets()
    {
        var t = Task.Run(Extractor.LoadGame);
        while(!t.IsCompleted)
        {
            Instance.Dispatcher.Invoke(() =>
            {
                Instance.ProgressBar.Value = Settings.Progress;
            });
        }
        Instance.Dispatcher.Invoke(() =>
        {
            Instance.LoadedAssets.Content = "Linking all EBX...";
        });

        //var a = Task.Run(LinkAllEbx);
        //while (!a.IsCompleted)
        //{
        //    Instance.Dispatcher.Invoke(() =>
        //    {
        //        Instance.ProgressBar.Value = Settings.Progress;
        //    });
        //}

        Instance.Dispatcher.Invoke(() =>
        {
            Instance.GameName.Content = Settings.CurrentGame;
        });
        Instance.Dispatcher.Invoke(UpdateItems);
    }

    public static void LinkAllEbx()
    {
        var assets = Settings.IOClass.GetAssets();
        for (int i = 0; i < assets.Count; i++)
        {
            if (assets.ElementAt(i).Key.Item2 == InternalAssetType.EBX)
            {
                Instance.Dispatcher.Invoke(() =>
                {
                    Settings.Progress = ((double)i / (double)assets.Count) * 100.0;
                });
                assets.ElementAt(i).Value.LinkEbx();
            }
        }
    }

    #endregion

    #region UI

    public static void UpdateItems()
    {
        Instance.Dispatcher.Invoke(() =>
        {
            Instance.LoadedAssets.Content = "Loaded Assets: " + Settings.IOClass.GetAssets().Count;
            Instance.AssetGrid.ItemsSource = Settings.IOClass.GetAssets().Values;
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
    }

    public static void WriteUIOutput(string message)
    {
        Instance.Dispatcher.Invoke(() =>
        {
            Instance.ConsoleOutput.Text += message;
        });
    }
    public static void WriteUIOutputLine(string message)
    {
        WriteUIOutput(message + "\n");
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var output = new Dictionary<(string, InternalAssetType), AssetListItem>();
        if (SearchBox.Text != "")
        {
            switch (Settings.CurrentGame)
            {

            }
            foreach (var item in Settings.IOClass.GetAssets())
            {
                if (item.Key.Item1.Contains(SearchBox.Text))
                {
                    output.Add((item.Key), item.Value);
                }
            }
            AssetGrid.ItemsSource = output.Values;
        }
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

    private void LoadTypeFormatBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Settings.LoadMode = (AssetLoadMode)LoadTypeFormatBox.SelectedIndex;
    }

    private void SkeletonFormatBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MeshFormatBox.SelectedIndex == 0)
            Settings.CurrentSkeletonExporter = new SkeletonExporterSMD();
    }
    private void MeshFormatBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MeshFormatBox.SelectedIndex == 0)
            Settings.CurrentModelExporter = new ModelExporterSMD();
        if (MeshFormatBox.SelectedIndex == 1)
            Settings.CurrentModelExporter = new ModelExporterOBJ();
        if (MeshFormatBox.SelectedIndex == 2)
            Settings.CurrentModelExporter = new ModelExporterSEMODEL();
    }

    private void TextureFormatBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TextureFormatBox.SelectedIndex == 0)
            Settings.CurrentTextureExporter = new TextureExporterDDS();
    }

    private void AnimationFormatBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AnimationFormatBox.SelectedIndex == 0)
            Settings.CurrentAnimationExporter = new AnimationExporterSMD();
        if (AnimationFormatBox.SelectedIndex == 1)
            Settings.CurrentAnimationExporter = new AnimationExporterSEANIM();
    }
}

#endregion

