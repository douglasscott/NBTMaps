/*----------------------------------------------------------------------------*
    Name:       NBTMaps
    Author:     Douglas Scott
    Written:    January 2016

    A utility to display maps created by the player in Minecraft outside of 
    the game.  This program will also save the maps as PNG images.
    
    This program requires fNBT.dll to read the game files.  This library
    is available on github.
 *----------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using Microsoft.Win32;

namespace NBTMaps
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //enum SortOrder { MapId, Scale };

        //private SortOrder sortOrder = SortOrder.MapId;
        private string lastSavePath = null;

        public MainWindow()
        {
            InitializeComponent();
            BuildTreeView();
            //this.KeyDown += new KeyEventHandler(Maps_KeyDown);
        }

        /// <summary>
        /// Look under %APPDATA%\.minecraft\data for a list of game directories
        /// and display this list to the user
        /// </summary>
        private void BuildTreeView()
        {
            buttonSave.IsEnabled = false;
            string path = null;
            textMessage.Text = String.Empty;
            try
            {
                path = System.Environment.ExpandEnvironmentVariables("%APPDATA%");
                if (String.IsNullOrEmpty(path))
                {
                    MessageBox.Show("APPDATA environment variable not defined");
                    Application.Current.Shutdown();
                    return;
                }
                path += @"\.minecraft\saves";
                if (!Directory.Exists(path))
                {
                    MessageBox.Show("Directory " + path + " does not exist");
                    Application.Current.Shutdown();
                    return;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }
            tvFiles.Items.Clear();
            ListGameDirectories(tvFiles, path);
        }

        /// <summary>
        /// Get a list of all the Minecraft game directories and add them to the Tree View
        /// </summary>
        /// <param name="tv">TreeView Structure to display</param>
        /// <param name="mapsPath">Path to map files</param>
        private void ListGameDirectories(TreeView tv, string mapsPath)
        {
            string[] dirs = Directory.GetDirectories(mapsPath);
            foreach (string f in dirs)
            {
                FileInfo fi = new FileInfo(f);
                TreeViewItem ti = new TreeViewItem();
                ti.Tag = fi;
                ti.Header = fi.Name;
                ti.Items.Add("*");
                ti.Expanded += ExpandMapList;
                ti.IsExpanded = false;
                tv.Items.Add(ti);
            }
        }

        /// <summary>
        /// Read the list of map files for selected Game into a TreeView from which the user can select
        /// </summary>
        /// <param name="sender">The Tree View Item object that the user clicked on</param>
        /// <param name="e">unused</param>
        public void ExpandMapList(object sender, RoutedEventArgs e)
        {
            var tvi = (TreeViewItem)sender;
            tvi.Items.Clear();
            var file = (FileInfo)tvi.Tag;
            string dataDir = file.FullName + @"\data";
            string[] dirFiles = Directory.GetFiles(dataDir);
            var nodeList = new List<Node>();
            foreach (string f in dirFiles)
            {
                var fi = new FileInfo(f);
                if (!Regex.IsMatch(fi.Name, @"map_\d+\.+dat", RegexOptions.IgnoreCase))
                    continue;
                var node = new Node(fi);
                nodeList.Add(node);
            }
            if (nodeList.Count == 0)
                tvi.Items.Add(" ");
            else
            {
                // resort the nodes so that they are in order by map id
                var SortedNodes = nodeList.OrderBy(n => n.mapId).ToList();
                foreach (Node node in SortedNodes)
                {
                    var ti = new TreeViewItem();
                    ti.Tag = node;
                    ti.Header = string.Format("{0}: {1}", node.file.Name, node.mapLevel);
                    ti.ToolTip = "Map " + node.mapId;
                    ti.MouseUp += ShowMap;
                    tvi.Items.Add(ti);
                }
            }
        }

        /// <summary>
        /// Read selected map file and convert map data to displayable image
        /// </summary>
        /// <param name="sender">TreeView item</param>
        /// <param name="e">unused</param>
        public void ShowMap(object sender, RoutedEventArgs e)
        {
            var tvi = (TreeViewItem)sender;
            var node = (Node)tvi.Tag;
            var file = node.file;

            buttonSave.IsEnabled = false;
            try
            {
                var map = new Map(file);
                if (map != null)
                {
                    textMessage.Text = file.Name;
                    MapImage.Source = map.image;
                    textFileName.Text = file.Name;
                    textScale.Text = map.Scale.ToString();
                    textCenterX.Text = map.xCenter.ToString();
                    textCenterZ.Text = map.zCenter.ToString();
                    buttonSave.IsEnabled = true;

                    textCenter.Text = string.Format("{0}  {1},{2}",
                        Path.GetFileNameWithoutExtension(file.Name),
                        map.xCenter.ToString(), map.zCenter.ToString());
                    CalculateBorders(map);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Calculate the corner coordinates
        /// </summary>
        /// <param name="map"></param>
        private void CalculateBorders(Map map)
        {
            int scale = map.Scale + 1;
            int x = map.xCenter;
            int z = map.zCenter;
            int top = x + (scale * map.Height/2);
            int bottom = x - (scale * map.Height/2);
            int left = z + (scale * map.Width/2);
            int right = z - (scale * map.Width/2);
            TopLeftXZ.Text = string.Format("{0},{1}", top.ToString(), left.ToString());
            TopRightXZ.Text = string.Format("{0},{1}", top.ToString(), right.ToString());
            BottomLeftXZ.Text = string.Format("{0},{1}", bottom.ToString(), left.ToString());
            BottomRightXZ.Text = string.Format("{0},{1}", bottom.ToString(), right.ToString());
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = Path.GetFileNameWithoutExtension(textFileName.Text);
            dlg.DefaultExt = "png";
            dlg.Filter = "PNG Documents (.png)|*.png";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                lastSavePath = Path.GetDirectoryName(dlg.FileName);
                SaveImage(dlg.FileName);
            }
        }

        /// <summary>
        /// Save image to PNG file
        /// </summary>
        /// <param name="filePath"></param>
        private void SaveImage(string filePath)
        {
            var image = new RenderTargetBitmap((int)MapImage.Width, (int)MapImage.Height, 96d, 96d, PixelFormats.Pbgra32);
            image.Render(MapImage);
            //var image = Clipboard.GetImage();
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fileStream);
                fileStream.Close();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
                FastSave();
        }

        /// <summary>
        /// Save the file immediately without the Save file dialog
        /// </summary>
        private void FastSave()
        {
            string FileName = Path.GetFileNameWithoutExtension(textFileName.Text);
            if (string.IsNullOrEmpty(lastSavePath))
            {
                string homeDrive = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%");
                string homeDir = Environment.ExpandEnvironmentVariables("%HOMEPATH%");
                FileName = string.Format(@"{0}{1}\{2}\{3}.png", homeDrive, homeDir, "Documents", FileName);
            }
            else
                FileName = string.Format(@"{0}\{1}.png", lastSavePath, FileName);
            SaveImage(FileName);
        }
    }
}


