using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using fNbt;

namespace NBTMaps
{
    /// <summary>
    /// Class for Nodes to hold map collections in a TreeView
    /// </summary>
    class Node
    {
        private string FullName;            // full name of file
        public FileInfo file {get; set; }   // File object
        public int mapId { get; set; }      // Map ID number
        public int mapLevel { get; set; }   // Level of this map
        public int xCenter { get; set; }
        public int zCenter { get; set; }
        
        /// <summary>
        /// Create an instance of Node to hold specified File information
        /// </summary>
        /// <param name="fi"></param>
        public Node(FileInfo fi)
        {
            FullName = fi.Name;
            file = fi;
            string result = Regex.Match(FullName, @"\d+").Value;
            mapId = result == null ? 0 : Int32.Parse(result);
            GetFileInfo(fi);
        }

        /// <summary>
        /// Read the information from a file and load it into Node.  The file is expected to hold Minecraft map data in NBT format.
        /// </summary>
        /// <param name="fi">Object that holds File information</param>
        public void GetFileInfo(FileInfo fi)
        {
            NbtFile f = null;
            try
            {
                f = new NbtFile();
                f.LoadFromFile(fi.FullName);
                var dataTag = f.RootTag["data"];
                if (dataTag != null)
                {
                    mapLevel = dataTag["scale"] == null ? -1 : dataTag["scale"].ByteValue; ;
                    xCenter = dataTag["xCenter"] == null ? -1 : dataTag["xCenter"].IntValue;
                    zCenter = dataTag["zCenter"] == null ? -1 : dataTag["zCenter"].IntValue;
                }
                Debug.WriteLine(string.Format("{0} {1}", fi.Name, dataTag["scale"] == null ? -1 : dataTag["scale"].ByteValue));
            }
            catch (Exception ex)
            {
                string s = string.Format("{0} error: {1}", fi.Name, ex.Message);
                throw new Exception(s);
            }
        }
    }
}
