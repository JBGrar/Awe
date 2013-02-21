#region File Description
//-----------------------------------------------------------------------------
// MainForm.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace AweEditor
{
    /// <summary>
    /// Custom form provides the main user interface for the program.
    /// In this sample we used the designer to fill the entire form with a
    /// ModelViewerControl, except for the menu bar which provides the
    /// "File / Open..." option.
    /// </summary>
    public partial class MainForm : Form
    {
        ContentBuilder contentBuilder;
        ContentManager contentManager;

        /// <summary>
        /// Constructs the main form.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            contentBuilder = new ContentBuilder();

            contentManager = new ContentManager(modelViewerControl.Services,
                                                contentBuilder.OutputDirectory);

            /// Automatically bring up the "Load Model" dialog when we are first shown.
            ///this.Shown += OpenMenuClicked;
        }


        /// <summary>
        /// Event handler for the Exit menu option.
        /// </summary>
        void ExitMenuClicked(object sender, EventArgs e)
        {
            Close();
        }


        /// <summary>
        /// Event handler for the Import Model menu option.
        /// </summary>
        void ImportModelMenuClicked(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.InitialDirectory = ContentPath();

            fileDialog.Title = "Load Model";

            fileDialog.Filter = "Model Files (*.fbx;*.x)|*.fbx;*.x|" +
                                "FBX Files (*.fbx)|*.fbx|" +
                                "X Files (*.x)|*.x|" +
                                "All Files (*.*)|*.*";

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadModel(fileDialog.FileName);
            }
        }

        private static string ContentPath()
        {
            // Default to the directory which contains our content files.
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string relativePath = Path.Combine(assemblyLocation, "../../../../Content");
            string contentPath = Path.GetFullPath(relativePath);
            return contentPath;
        }

        /// <summary>
        /// Loads a new minecraft terrain file into the TerrainViewerControl.
        /// </summary>
        private void ImportVoxelTerrainMenuClicked(object sender, EventArgs e)
        {
            // TODO: Import the file
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.InitialDirectory = ContentPath();

            fileDialog.Title = "Load Terrain";

            fileDialog.Filter = "Minecraft Files (*.nbt;*.mca)|*.nbt;*.mca";

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadTerrain(fileDialog.FileName);
            }
        }


        /// <summary>
        /// Loads a new 3D model file into the ModelViewerControl.
        /// </summary>
        void LoadModel(string fileName)
        {
            Cursor = Cursors.WaitCursor;

            // Switch to the Model tab pane
            tabControl1.SelectedIndex = 1;

            // Unload any existing model.
            modelViewerControl.Model = null;
            contentManager.Unload();

            // Tell the ContentBuilder what to build.
            contentBuilder.Clear();
            contentBuilder.Add(fileName, "Model", null, "ModelProcessor");

            // Build this new model data.
            string buildError = contentBuilder.Build();

            if (string.IsNullOrEmpty(buildError))
            {
                // If the build succeeded, use the ContentManager to
                // load the temporary .xnb file that we just created.
                modelViewerControl.Model = contentManager.Load<Model>("Model");
            }
            else
            {
                // If the build failed, display an error message.
                MessageBox.Show(buildError, "Error");
            }

            Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// Handles the loading of the voxel based terrain
        /// </summary>
        /// <param name="filename">The name of the terrain file to load</param>
        void LoadTerrain(string filename)
        {
            // Read entire contents of region file into byte array
            byte[] uncompressedFile = File.ReadAllBytes(filename);
            byte[][] decompressedChunkData = new byte[1024][];

            for (int i = 0; i < 200; i++)
            {
                // Find length of the chunk's sector and if it's zero assume the chunk is not populated and continue
                byte locationLength = uncompressedFile.Skip(i * 4 + 3).Take(1).ToArray()[0];
                if (locationLength == 0)
                {
                    decompressedChunkData[i] = null;
                    continue;
                }

                // Find the location of the chunk and the size of the chunk's sector (in bytes)
                byte[] locationData = uncompressedFile.Skip(i * 4).Take(3).ToArray();
                Array.Reverse(locationData);
                int chunkLocation = (locationData[0] + (locationData[1] << 8) + (locationData[2] << 16)) * 4096;
                int sectorLength = locationLength * 4096;

                // Find the length of the compressed chunk data (in bytes)
                byte[] temp = uncompressedFile.Skip(chunkLocation).Take(4).ToArray();
                Array.Reverse(temp);
                int chunkLength = BitConverter.ToInt32(temp, 0);

                // Find the compression type of the chunk data and if we can't handle it, set that chunk to null and move on
                byte compressionType = uncompressedFile.Skip(chunkLocation + 4).Take(1).ToArray()[0];
                if (compressionType != 2)
                {
                    decompressedChunkData[i] = null;
                    continue;
                }

                // Decompress the chunk data and store it in an array with the other chunk data
                byte[] dataToDecomp = uncompressedFile.Skip(chunkLocation + 7).Take(chunkLength - 4).ToArray();
                decompressedChunkData[i] = Decompress(dataToDecomp);
            }
            foreach (byte[] b in decompressedChunkData)
            {
                if (b != null)
                    ParseChunk(b);
            }
            tabControl1.SelectedIndex = 3;
        }

        private void ParseChunk(byte[] data)
        {
            int[] tagSize = { 0, 1, 2, 4, 8, 4, 8, -1, -2, -3, -4, -5 };
            Tag currentParent = null;
            for (int i = 0; i < data.Length; i++)
            {
                byte[] sizeBytes;
                short size;
                string name;
                byte[] nameBytes;

                int tempData = data[i];
                switch (tempData)
                {
                    case 0:
                        break;
                    case 1:
                        break;
                    case 2:
                        break;
                    case 3:
                        break;
                    case 4:
                        break;
                    case 5:
                        break;
                    case 6:
                        break;
                    case 7:
                        break;
                    case 8:
                        break;
                    case 9:
                        sizeBytes = data.Skip(i + 1).Take(2).ToArray();
                        Array.Reverse(sizeBytes);
                        size = BitConverter.ToInt16(sizeBytes, 0);

                        if (size != 0)
                        {
                            nameBytes = data.Skip(i + 3).Take(size).ToArray();
                            name = ByteArrayToString(nameBytes);
                        }
                        else
                            name = null;

                        int type = data.Skip(i + 3 + size).Take(1).ToArray()[0];
                        TAG_List tag_l = new TAG_List(name, type, currentParent);

                        byte[] tempArr = data.Skip(i + 3 + size + 1).Take(4).ToArray();
                        Array.Reverse(tempArr);
                        int sizeOfList = BitConverter.ToInt32(tempArr, 0);
                        int sizeOfData = tagSize[type];
                        if (sizeOfData == 0)
                            throw new InvalidOperationException();
                        else if (sizeOfData < 1)
                        {

                        }

                        // Sets the index to one before the next tag id
                        i = i + 3 + size + 5 - 1;

                        break;
                    case 10:
                        sizeBytes = data.Skip(i + 1).Take(2).ToArray();
                        Array.Reverse(sizeBytes);
                        size = BitConverter.ToInt16(sizeBytes, 0);

                        if (size != 0)
                        {
                            nameBytes = data.Skip(i + 3).Take(size).ToArray();
                            name = ByteArrayToString(nameBytes);
                        }
                        else
                            name = null;

                        TAG_Compound tag_c = new TAG_Compound(name, currentParent);
                        currentParent = tag_c;
                        i = i + 3 + size - 1;

                        break;
                    case 11:
                        break;
                }
            }
        }

        private void ImportImageClicked(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();

            fd.InitialDirectory = ContentPath();

            fd.Title = "Import Image";

            fd.Filter = "Image Files (*.bmp;*.dds;*.dib;*.hdr;*.jpg;*.pfm;*.png;*.ppm;*.tga)|*.bmp;*.dds;*.dib;*.hdr;*.jpg;*.pfm;*.png;*.ppm;*.tga|" +
                                "Bitmap (*.bmp)|*.bmp|" +
                                "Portable network Graphic (*.png)|*.png|" +
                                "All Files (*.*)|*.*";

            if (fd.ShowDialog() == DialogResult.OK)
            {
                LoadTexture(fd.FileName);
            }

        }

        protected void LoadTexture(string fileName)
        {
            Cursor = Cursors.WaitCursor;

            // Switch to the Texture tab pane
            tabControl1.SelectedIndex = 5;

            // Unload any existing texture.
            textureViewerControl.Texture = null;
            contentManager.Unload();

            // Tell the ContentBuilder what to build.
            contentBuilder.Clear();
            contentBuilder.Add(fileName, "Texture", null, "TextureProcessor");

            // Build this new texture data.
            string buildError = contentBuilder.Build();

            if (string.IsNullOrEmpty(buildError))
            {
                // If the build succeeded, use the ContentManager to
                // load the temporary .xnb file that we just created.
                textureViewerControl.Texture = contentManager.Load<Texture2D>("Texture");
            }
            else
            {
                // If the build failed, display an error message.
                MessageBox.Show(buildError, "Error");
            }


            Cursor = Cursors.Arrow;
        }

        private static byte[] Decompress(byte[] data)
        {
            using (DeflateStream stream = new DeflateStream(new MemoryStream(data), CompressionMode.Decompress))
            {
                const int size = 1024 * 128;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        private string ByteArrayToString(byte[] arr)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetString(arr);
        }
    }
}
