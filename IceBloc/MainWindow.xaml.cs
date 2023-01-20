using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Globalization;
using System.Threading;

using IceBloc.Utility;
using IceBlocLib.Export;
using System.Threading.Tasks;
using IceBlocLib.Frostbite;
using IceBlocLib.Utility;

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
        Settings.ExporterType = Exporter.GUI;
        InitializeComponent();
    }

    #region Asset Loading

    public static void LoadAssets()
    {
        var t = Task.Run(IO.LoadGame);
        while(!t.IsCompleted)
        {
            Instance.Dispatcher.Invoke(() =>
            {
                Instance.ProgressBar.Value = Settings.Progress;
                Instance.GameName.Content = Settings.CurrentGame;
            });
        }
        Instance.Dispatcher.Invoke(UpdateItems);
    }

    #endregion

    #region UI

    public static void UpdateItems()
    {
        Instance.Dispatcher.Invoke(() =>
        {
            //Instance.AssetGrid.ItemsSource = null;
            Instance.LoadedAssets.Content = "Loaded Assets: " + IO.Assets.Count;
            Instance.AssetGrid.ItemsSource = IO.Assets.Values;
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
        Instance.Dispatcher.Invoke(() =>
        {
            Instance.ConsoleOutput.Text += message;
        });
    }
    public static void WriteUIOutputLine(string message)
    {
        WriteUIOutput(message + "\n");
    }

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
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var output = new Dictionary<(string, InternalAssetType), AssetListItem>();
        if (SearchBox.Text != "")
        {
            foreach (var item in IO.Assets)
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
}

#endregion