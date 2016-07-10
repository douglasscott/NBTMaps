using fNbt;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace NBTMaps
{
    /// <summary>
    /// Class for holding data from a Minecraft NBT map file
    /// </summary>
    public class Map
    {
        private const int ColorSize = 16384;
        private const int channels = 4;
        private string fullName { get; set; }
        private string shortName { get; set; }
        public byte Scale { get; set; }
        public byte Dimension { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public int xCenter { get; set; }
        public int zCenter { get; set; }
        public byte[] Colors { get; set; }
        private Bitmap bitmap { get; set; }
        public BitmapImage image { get; set; }

        /// <summary>
        /// Create a new instance of Map class
        /// </summary>
        /// <param name="fi">Map file to store in this node</param>
        public Map(FileInfo fi)
        {
            fullName = null;
            shortName = null;
            bitmap = null;
            image = null;
            Load(fi);
        }

        /// <summary>
        /// Return the file name for this map
        /// </summary>
        /// <returns>File name</returns>
        public override string ToString()
        {
            if (shortName != null)
                return shortName;
            return base.ToString();
        }

        /// <summary>
        /// Load a map file into memory and display information
        /// </summary>
        /// <param name="fi">The map file to load</param>
        private void Load(FileInfo fi)
        {
            NbtFile f = new NbtFile();
            fullName = fi.FullName;
            shortName = fi.Name;
            try
            {
                f.LoadFromFile(fullName);
            }
            catch (Exception ex)
            {
                string s = string.Format("Error loading {0}: {1}", fi.Name, ex.Message);
                MessageBox.Show(s);
                return;
            }
            try
            {
                var dataTag = f.RootTag["data"];
                Scale = dataTag["scale"].ByteValue;
                xCenter = dataTag["xCenter"].IntValue;
                zCenter = dataTag["zCenter"].IntValue;
                Height = dataTag["height"].IntValue;
                Width = dataTag["width"].IntValue;
                Dimension = dataTag["dimension"].ByteValue;
                Colors = dataTag["colors"].ByteArrayValue;
                MCMapToBitmap();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Clear all the map information
        /// </summary>
        public void Clear()
        {
            fullName = null;
            shortName = null;
            Scale = 0;
            Dimension = 0;
            Height = 0;
            Width = 0;
            xCenter = 0;
            zCenter = 0;
            if (bitmap != null)
                bitmap.Dispose();
            bitmap = null;
            image = null;
        }

        /// <summary>
        /// Convert the Mincraft map data into a bitmap and then convert the bitmap into a displayable image
        /// </summary>
        private void MCMapToBitmap()
        {
            byte[] Arry = new byte[ColorSize * channels];
            for (int i = 0, x = 0; i < Colors.Length; i++, x += channels)
            {
                Arry[x] = (byte)mapColors[Colors[i], 3];
                Arry[x + 1] = (byte)mapColors[Colors[i], 2];
                Arry[x + 2] = (byte)mapColors[Colors[i], 1];
                Arry[x + 3] = (byte)mapColors[Colors[i], 0];
            }
            bitmap = ToBitmap(Arry);
            image = BitmapToImageSource(bitmap);
        }

        /// <summary>
        /// Convert the byte array into a bitmap
        /// </summary>
        /// <param name="array">A list of bytes that will define the new bitmap image</param>
        /// <returns>Bitmap of the map data</returns>
        private Bitmap ToBitmap(byte[] array)
        {
            if (array == null || array.Length == 0)
                return null;
            Bitmap bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, bmp.PixelFormat);
            IntPtr pBmp = data.Scan0;
            Marshal.Copy(array, 0, pBmp, Width * Height * channels);
            bmp.UnlockBits(data);
            return bmp;
        }

        /// <summary>
        /// Convert bitmap into an image that can be displayed on the screen.
        /// </summary>
        /// <param name="bitmap">Bitmap to convert</param>
        /// <returns>Bitmap image of map data</returns>
        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        //public BitmapImage ToImage(byte[] array)
        //{
        //    if (array == null || array.Length == 0)
        //        return null;
        //    var image = new BitmapImage();
        //    using (var ms = new MemoryStream(array, 0, array.Length))
        //    {
        //        ms.Position = 0;
        //        image.BeginInit();
        //        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        //        image.CacheOption = BitmapCacheOption.OnLoad;
        //        image.UriSource = null;
        //        image.StreamSource = ms;
        //        image.EndInit();
        //    }
        //    image.Freeze();
        //    return image;
        //}

        //public static BitmapSource ToWpfBitmap(Bitmap bitmap)
        //{
        //    using (MemoryStream stream = new MemoryStream())
        //    {
        //        bitmap.Save(stream, ImageFormat.Bmp);

        //        stream.Position = 0;
        //        BitmapImage result = new BitmapImage();
        //        result.BeginInit();
        //        // According to MSDN, "The default OnDemand cache option retains access to the stream until the image is needed."
        //        // Force the bitmap to load right now so we can dispose the stream.
        //        result.CacheOption = BitmapCacheOption.OnLoad;
        //        result.StreamSource = stream;
        //        result.EndInit();
        //        result.Freeze();
        //        return result;
        //    }
        //}

        /// <summary>
        /// Map colors used by Minecraft.  These colors can be calculated from the base colors instead of using this table if you prefer.
        /// </summary>
        private int[,] mapColors = new int[144, channels]
        {
            {  0,  0,  0,  0},      // Transparent
            {  0,  0,  0,  0},      // Transparent
            {  0,  0,  0,  0},      // Transparent
            {  0,  0,  0,  0},      // Transparent
            {255, 88,124, 39},      // Grass
            {255,108,151, 47},      // Grass
            {255,125,176, 55},      // Grass
            {255, 66, 93, 29},      // Grass
            {255,172,162,114},      // Sand/Gravel
            {255,210,199,138},      // Sand/Gravel
            {255,244,230,161},      // Sand/Gravel
            {255,128,122, 85},      // Sand/Gravel
            {255,138,138,138},      // Other
            {255,169,169,169},      // Other
            {255,197,197,197},      // Other
            {255,104,104,104},      // Other
            {255,178,  0,  0},      // Lava/TNT
            {255,217,  0,  0},      // Lava/TNT
            {255,252,  0,  0},      // Lava/TNT
            {255,133,  0,  0},      // Lava/TNT
            {255,111,111,178},      // Ice/Packed Ice
            {255,136,136,217},      // Ice/Packed Ice
            {255,158,158,252},      // Ice/Packed Ice
            {255, 83, 83,133},      // Ice/Packed Ice
            {255,116,116,116},      // Metal
            {255,142,142,142},      // Metal
            {255,165,165,165},      // Metal
            {255, 87, 87, 87},      // Metal
            {255,  0, 86,  0},      // Plants
            {255,  0,105,  0},      // Plants
            {255,  0,123,  0},      // Plants
            {255,  0, 64,  0},      // Plants
            {255,178,178,178},      // hite Wool/Carpet/Stained Clay
            {255,217,217,217},      // hite Wool/Carpet/Stained Clay
            {255,252,252,252},      // hite Wool/Carpet/Stained Clay
            {255,133,133,133},      // hite Wool/Carpet/Stained Clay
            {255,114,117,127},      // lay
            {255,139,142,156},      // lay
            {255,162,166,182},      // Clay
            {255, 85, 87, 96},      // Clay
            {255,105, 75, 53},      // Dirt
            {255,128, 93, 65},      // Dirt
            {255,149,108, 76},      // Dirt
            {255, 78, 56, 39},      // Dirt
            {255, 78, 78, 78},      // Stone/Cobblestone/Ore
            {255, 95, 95, 95},      // Stone/Cobblestone/Ore
            {255,111,111,111},      // Stone/Cobblestone/Ore
            {255, 58, 58, 58},      // Stone/Cobblestone/Ore
            {255, 44, 44,178},      // Water
            {255, 54, 54,217},      // Water
            {255, 63, 63,252},      // Water
            {255, 33, 33,133},      // Water
            {255, 99, 83, 49},      // Log/Tree/Wood
            {255,122,101, 61},      // Log/Tree/Wood
            {255,141,118, 71},      // Log/Tree/Wood
            {255, 74, 62, 38},      // Log/Tree/Wood
            {255,178,175,170},      // Diorite
            {255,217,214,208},      // Diorite
            {255,252,249,242},      // Diorite
            {255,133,131,127},      // Diorite
            {255,150, 88, 36},      // Orange Wool/Carpet/Stained Clay, Pumpkin, Hardened Clay, Acacia Items
            {255,184,108, 43},      // Orange Wool/Carpet/Stained Clay, Pumpkin, Hardened Clay, Acacia Items
            {255,213,125, 50},      // Orange Wool/Carpet/Stained Clay, Pumpkin, Hardened Clay, Acacia Items
            {255,113, 66, 27},      // Orange Wool/Carpet/Stained Clay, Pumpkin, Hardened Clay, Acacia Items
            {255,124, 52,150},      // Magenta Wool/Carpet/Stained Clay
            {255,151, 64,184},      // Magenta Wool/Carpet/Stained Clay
            {255,176, 75,213},      // Magenta Wool/Carpet/Stained Clay
            {255, 93, 39,113},      // Magenta Wool/Carpet/Stained Clay
            {255, 71,107,150},      // Light Blue Wool/Carpet/Stained Clay
            {255, 87,130,184},      // Light Blue Wool/Carpet/Stained Clay
            {255,101,151,213},      // Light Blue Wool/Carpet/Stained Clay
            {255, 53, 80,113},      // Light Blue Wool/Carpet/Stained Clay
            {255,159,159, 36},      // Yellow Wool/Carpet/Stained Clay, Sponge
            {255,195,195, 43},      // Yellow Wool/Carpet/Stained Clay, Sponge
            {255,226,226, 50},      // Yellow Wool/Carpet/Stained Clay, Sponge
            {255,120,120, 27},      // Yellow Wool/Carpet/Stained Clay, Sponge
            {255, 88,142, 17},      // Lime Wool/Carpet/Stained Clay, Melon
            {255,108,174, 21},      // Lime Wool/Carpet/Stained Clay, Melon
            {255,125,202, 25},      // Lime Wool/Carpet/Stained Clay, Melon
            {255, 66,107, 13},      // Lime Wool/Carpet/Stained Clay, Melon
            {255,168, 88,115},      // Pink Wool/Carpet/Stained Clay
            {255,206,108,140},      // Pink Wool/Carpet/Stained Clay
            {255,239,125,163},      // Pink Wool/Carpet/Stained Clay
            {255,126, 66, 86},      // Pink Wool/Carpet/Stained Clay
            {255, 52, 52, 52},      // Grey Wool/Carpet/Stained Clay, Cauldron
            {255, 64, 64, 64},      // Grey Wool/Carpet/Stained Clay, Cauldron
            {255, 75, 75, 75},      // Grey Wool/Carpet/Stained Clay, Cauldron
            {255, 39, 39, 39},      // Grey Wool/Carpet/Stained Clay, Cauldron
            {255,107,107,107},      // Light Grey Wool/Carpet/Stained Clay
            {255,130,130,130},      // Light Grey Wool/Carpet/Stained Clay
            {255,151,151,151},      // Light Grey Wool/Carpet/Stained Clay
            {255, 80, 80, 80},      // Light Grey Wool/Carpet/Stained Clay
            {255, 52, 88,107},      // Cyan Wool/Carpet/Stained Clay
            {255, 64,108,130},      // Cyan Wool/Carpet/Stained Clay
            {255, 75,125,151},      // Cyan Wool/Carpet/Stained Clay
            {255, 39, 66, 80},      // Cyan Wool/Carpet/Stained Clay
            {255, 88, 43,124},      // Purple Wool/Carpet/Stained Clay, Mycelium
            {255,108, 53,151},      // Purple Wool/Carpet/Stained Clay, Mycelium
            {255,125, 62,176},      // Purple Wool/Carpet/Stained Clay, Mycelium
            {255, 66, 33, 93},      // Purple Wool/Carpet/Stained Clay, Mycelium
            {255, 36, 52,124},      // Blue Wool/Carpet/Stained Clay
            {255, 43, 64,151},      // Blue Wool/Carpet/Stained Clay
            {255, 50, 75,176},      // Blue Wool/Carpet/Stained Clay
            {255, 27, 39, 93},      // Blue Wool/Carpet/Stained Clay
            {255, 71, 52, 36},      // Brown Wool/Carpet/Stained Clay, Soul Sand, Dark Oak Items
            {255, 87, 64, 43},      // Brown Wool/Carpet/Stained Clay, Soul Sand, Dark Oak Items
            {255,101, 75, 50},      // Brown Wool/Carpet/Stained Clay, Soul Sand, Dark Oak Items
            {255, 53, 39, 27},      // Brown Wool/Carpet/Stained Clay, Soul Sand, Dark Oak Items
            {255, 71, 88, 36},      // Green Wool/Carpet/Stained Clay, End Portal Frame
            {255, 87,108, 43},      // Green Wool/Carpet/Stained Clay, End Portal Frame
            {255,101,125, 50},      // Green Wool/Carpet/Stained Clay, End Portal Frame
            {255, 53, 66, 27},      // Green Wool/Carpet/Stained Clay, End Portal Frame
            {255,107, 36, 36},      // Red Wool/Carpet/Stained Clay, Huge Red Mushroom, Brick Items
            {255,130, 43, 43},      // Red Wool/Carpet/Stained Clay, Huge Red Mushroom, Brick Items
            {255,151, 50, 50},      // Red Wool/Carpet/Stained Clay, Huge Red Mushroom, Brick Items
            {255, 80, 27, 27},      // Red Wool/Carpet/Stained Clay, Huge Red Mushroom, Brick Items
            {255, 17, 17, 17},      // Black Wool/Carpet/Stained Clay, Dragon Egg, Block of Coal
            {255, 21, 21, 21},      // Black Wool/Carpet/Stained Clay, Dragon Egg, Block of Coal
            {255, 25, 25, 25},      // Black Wool/Carpet/Stained Clay, Dragon Egg, Block of Coal
            {255, 13, 13, 13},      // Black Wool/Carpet/Stained Clay, Dragon Egg, Block of Coal
            {255,174,166, 53},      // Block of Gold, Hay Bale, Weighted Pressure Plate (Light)
            {255,212,203, 65},      // Block of Gold, Hay Bale, Weighted Pressure Plate (Light)
            {255,247,235, 76},      // Block of Gold, Hay Bale, Weighted Pressure Plate (Light)
            {255,130,125, 39},      // Block of Gold, Hay Bale, Weighted Pressure Plate (Light)
            {255, 63,152,148},      // Block of Diamond, Prismarine, Prismarine Bricks, Dark Prismarine, Beacon
            {255, 78,186,181},      // Block of Diamond, Prismarine, Prismarine Bricks, Dark Prismarine, Beacon
            {255, 91,216,210},      // Block of Diamond, Prismarine, Prismarine Bricks, Dark Prismarine, Beacon
            {255, 47,114,111},      // Block of Diamond, Prismarine, Prismarine Bricks, Dark Prismarine, Beacon
            {255, 51, 89,178},      // Lapis Lazuli Block
            {255, 62,109,217},      // Lapis Lazuli Block
            {255, 73,129,252},      // Lapis Lazuli Block
            {255, 39, 66,133},      // Lapis Lazuli Block
            {255,  0,151, 39},      // Block of Emerald
            {255,  0,185, 49},      // Block of Emerald
            {255,  0,214, 57},      // Block of Emerald
            {255,  0,113, 30},      // Block of Emerald
            {255, 90, 59, 34},      // Obsidian, Enchantment Table, End Portal (portal)
            {255,110, 73, 41},      // Obsidian, Enchantment Table, End Portal (portal)
            {255,127, 85, 48},      // Obsidian, Enchantment Table, End Portal (portal)
            {255, 67, 44, 25},      // Obsidian, Enchantment Table, End Portal (portal)
            {255, 78,  1,  0},      // Netherrack, Quartz Ore, Nether Wart, Nether Brick Items
            {255, 95,  1,  0},      // Netherrack, Quartz Ore, Nether Wart, Nether Brick Items
            {255,111,  2,  0},      // Netherrack, Quartz Ore, Nether Wart, Nether Brick Items
            {255, 58,  1,  0}       // Netherrack, Quartz Ore, Nether Wart, Nether Brick Items
        };

        /// <summary>
        /// Base colors used by Minecraft as of version 1.8
        /// </summary>
        private int[,] baseColors = new int[37, channels]
        {
            {   0,   0,   0,   0 },
            { 255, 127, 178,  56 },
            { 255, 247, 233, 163 },
            { 255, 167, 167, 167 },
            { 255, 255,   0,   0 },
            { 255, 160, 160, 255 },
            { 255, 167, 167, 167 },
            { 255,   0, 124,   0 },
            { 255, 255, 255, 255 },
            { 255, 164, 168, 184 },
            { 255, 183, 106,  47 },
            { 255, 112, 112, 112 },
            { 255,  64,  64, 255 },
            { 255, 104,  83,  50 },
			//new 1.7 colors (13w42a/13w42b)
			{ 255, 255, 252, 245 },
            { 255, 216, 127,  51 },
            { 255, 178,  76, 216 },
            { 255, 102, 153, 216 },
            { 255, 229, 229,  51 },
            { 255, 127, 204,  25 },
            { 255, 242, 127, 165 },
            { 255,  76,  76,  76 },
            { 255, 153, 153, 153 },
            { 255,  76, 127, 153 },
            { 255, 127,  63, 178 },
            { 255,  51,  76, 178 },
            { 255, 102,  76,  51 },
            { 255, 102, 127,  51 },
            { 255, 153,  51,  51 },
            { 255,  25,  25,  25 },
            { 255, 250, 238,  77 },
            { 255,  92, 219, 213 },
            { 255,  74, 128, 255 },
            { 255,   0, 217,  58 },
            { 255,  21,  20,  31 },
            { 255, 112,   2,   0 },
			//new 1.8 colors
            { 255, 126,  84,  48 }
        };
    }
}
