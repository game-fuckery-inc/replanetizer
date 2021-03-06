﻿using ImageMagick;
using LibReplanetizer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using static LibReplanetizer.DataFunctions;

namespace RatchetEdit
{
    public partial class TextureViewer : Form
    {
        /*
         * Be aware that textures may come from different sources like from the engine or armor files
         * If other files are parsed, their textures need to be handled separately aswell
         * Since this tool is supposed to be able to mod the game, merging the textures into one is probably out of reach
         * (Though we could keep a separate list containing all textures but that is probably also a mess to maintain)
         * 
         * Loading the textures may take a few seconds, if the user closes the window in the meantime we have to delay it
         * (or find some other way to stop the grid loading)
         * Here it is done through a status variable indicating whether we still load and an event which fires after loading
         * and to which we subscribe on close
         */

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private delegate void loadingGridCloseHandler(object source, EventArgs e);

        public Main main;
        public TextureConfig conf;
        public ModelViewer mod;
        public UIViewer uiView;

        public int returnVal;
        private bool loadingGrid = false;
        private event loadingGridCloseHandler OnCloseDuringLoad;

        public List<ListViewItem> virtualCache = new List<ListViewItem>();

        public TextureViewer(Main main)
        {
            InitializeComponent();
            this.main = main;
        }

        private int GetTotalTextureCount()
        {
            int count = main.level.textures.Count;

            foreach (List<Texture> list in main.level.armorTextures)
            {
                count += list.Count;
            }

            count += main.level.gadgetTextures.Count;

            foreach (Mission mission in main.level.missions)
            {
                count += mission.textures.Count;
            }

            return count;
        }

        public void UpdateTextureList()
        {
            texAmountLabel.Text = "Texture Count: " + GetTotalTextureCount();
            UpdateTextureGrid();
        }

        public void UpdateTextureImage(Texture tex)
        {
            textureImage.Image = tex.getTextureImage();
        }

        public void UpdateTextureGrid()
        {
            textureView.Items.Clear();
            texImages.Images.Clear();
            virtualCache.Clear();

            textureView.VirtualListSize = GetTotalTextureCount();

            int index = 0;

            for (int i = 0; i < main.level.textures.Count; i++)
            {
                virtualCache.Add(new ListViewItem("tex_" + i, index++));
            }

            for (int i = 0; i < main.level.armorTextures.Count; i++)
            {
                List<Texture> textures = main.level.armorTextures[i];
                for (int j = 0; j < textures.Count; j++)
                {
                    virtualCache.Add(new ListViewItem("tex_armor" + i + "_" + j, index++));
                }
            }

            for (int i = 0; i < main.level.gadgetTextures.Count; i++)
            {
                virtualCache.Add(new ListViewItem("tex_gadget_" + i, index++));
            }

            for (int i = 0; i < main.level.missions.Count; i++)
            {
                List<Texture> textures = main.level.missions[i].textures;
                for (int j = 0; j < textures.Count; j++)
                {
                    virtualCache.Add(new ListViewItem("tex_mission" + i + "_" + j, index++));
                }
            }

            ThreadStart tstart = new ThreadStart(delegate ()
            {
                LoadForGrid();
            });

            Thread thread = new Thread(tstart);
            thread.Start();

            texImages.Disposed += (object sender, EventArgs args) => { thread.Abort(); };
        }

        private void AddImage(Image image, int index, string infix)
        {
            if (InvokeRequired)
            {
                this?.Invoke(new MethodInvoker(delegate { texImages.Images.Add("tex_" + infix + index, image); }));
            } else
            {
                texImages.Images.Add("tex_" + infix + index, image);
            }
        }

        public void LoadForGrid()
        {
            loadingGrid = true;

            for (int i = 0; i < main.level.textures.Count; i++)
            {
                Image image = main.level.textures[i].getTextureImage();
                AddImage(image, i, "");
            }

            for (int i = 0; i < main.level.armorTextures.Count; i++)
            {
                List<Texture> textures = main.level.armorTextures[i];
                string infix = "armor" + i + "_";
                for (int j = 0; j < textures.Count; j++)
                {
                    Image image = textures[j].getTextureImage();
                    AddImage(image, j, infix);
                }
            }

            for (int i = 0; i < main.level.gadgetTextures.Count; i++)
            {
                Image image = main.level.gadgetTextures[i].getTextureImage();
                AddImage(image, i, "gadget_");
            }

            for (int i = 0; i < main.level.missions.Count; i++)
            {
                List<Texture> textures = main.level.missions[i].textures;
                string infix = "mission" + i + "_";
                for (int j = 0; j < textures.Count; j++)
                {
                    Image image = textures[j].getTextureImage();
                    AddImage(image, j, infix);
                }
            }

            if (InvokeRequired)
            {
                this?.Invoke(new MethodInvoker(delegate { textureView.Refresh(); }));
            }
            else
            {
                textureView.Refresh();
            }

            loadingGrid = false;

            if (OnCloseDuringLoad != null)
                OnCloseDuringLoad(this, new EventArgs());
        }

        //Removes DDS header
        public byte[] RemoveHeader(byte[] input)
        {
            byte[] newData = new byte[input.Length - 0x80];
            Array.Copy(input, 0x80, newData, 0, newData.Length);

            return newData;
        }

        public void AddNewTexture(byte[] image, short width, short height)
        {
            main.level.textures.Add(new Texture(main.level.textures.Count, height, width, image));
            UpdateTextureList();
        }

        private int GlobalIndexToLocalIndex(int index)
        {
            if (index >= main.level.textures.Count)
            {
                index -= main.level.textures.Count;

                foreach (List<Texture> list in main.level.armorTextures)
                {
                    if (index >= list.Count)
                    {
                        index -= list.Count;
                    }
                    else
                    {
                        return index;
                    }
                }

                if (index >= main.level.gadgetTextures.Count)
                {
                    foreach (Mission mission in main.level.missions)
                    {
                        if (index >= mission.textures.Count)
                        {
                            index -= mission.textures.Count;
                        }
                        else
                        {
                            return index;
                        }
                    }
                }
            }

            return index;
        }

        private void CloseForm()
        {
            ListView.SelectedIndexCollection col = textureView.SelectedIndices;
            if (col.Count > 0)
            {
                int index = textureView.Items[col[0]].ImageIndex;

                returnVal = GlobalIndexToLocalIndex(index);
            }      
        
            if (loadingGrid)
            {
                OnCloseDuringLoad += new loadingGridCloseHandler((o, e) => 
                { 
                    if (InvokeRequired)
                    {
                        this?.Invoke(new MethodInvoker(delegate 
                        {
                            DialogResult = DialogResult.OK;
                            Close();
                        }));
                    }
                    
                });
            }
            else
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void ImportBtn_Click(object sender, EventArgs e)
        {
            if (openTextureDialog.ShowDialog() != DialogResult.OK) return;

            string fileName = openTextureDialog.FileName;
            string extension = Path.GetExtension(fileName).ToLower();

            switch (extension)
            {
                case ".bmp":
                case ".png":
                case ".jpg":
                    Logger.Info("Adding new image texture");
                    using (MagickImage image = new MagickImage(fileName))
                    {
                        image.Format = MagickFormat.Dxt5;
                        image.HasAlpha = true;
                        AddNewTexture(RemoveHeader(image.ToByteArray()), (short)image.Width, (short)image.Height);
                    }
                    break;
                case ".dds":
                    Logger.Info("Adding new DDS texture");
                    byte[] img = File.ReadAllBytes(fileName);
                    short width = ReadShort(img, 0x10);
                    short height = ReadShort(img, 0x0C);
                    AddNewTexture(RemoveHeader(img), width, height);
                    break;
            }
        }

        private void TexListView_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (main.level.textures != null && e.ItemIndex >= 0 && e.ItemIndex < virtualCache.Count)
            {
                //A cache hit, so get the ListViewItem from the cache instead of making a new one.
                e.Item = virtualCache[e.ItemIndex];
            }
            else
            {
                //A cache miss, so create a new ListViewItem and pass it back.
                int x = e.ItemIndex * e.ItemIndex;
                e.Item = new ListViewItem(x.ToString());
            }
        }

        private void TextureViewer_Load(object sender, EventArgs e)
        {
            UpdateTextureList();
        }

        private void ActionOnTextureByIndex(int index, Action<Texture> action)
        {
            if (index >= main.level.textures.Count)
            {
                index -= main.level.textures.Count;

                foreach (List<Texture> list in main.level.armorTextures)
                {
                    if (index >= list.Count)
                    {
                        index -= list.Count;
                    }
                    else
                    {
                        action(list[index]);
                        return;
                    }
                }

                if (index >= main.level.gadgetTextures.Count)
                {
                    foreach (Mission mission in main.level.missions)
                    {
                        if (index >= mission.textures.Count)
                        {
                            index -= mission.textures.Count;
                        }
                        else
                        {
                            action(mission.textures[index]);
                            return;
                        }
                    }
                } else
                {
                    action(main.level.gadgetTextures[index]);
                } 
            }
            else
            {
                action(main.level.textures[index]);
            }
        }

        private void TexListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (textureView.SelectedIndices.Count != 0)
            {
                ActionOnTextureByIndex(textureView.SelectedIndices[0], (t) => { UpdateTextureImage(t); });
            }              
        }

        private void CloseButtonClick(object sender, EventArgs e)
        {
            CloseForm();
        }

        private void button2_Click(object sender, EventArgs e)
        {   
            if (loadingGrid)
            {
                OnCloseDuringLoad += new loadingGridCloseHandler((o, a) =>
                {
                    if (InvokeRequired)
                    {
                        this?.Invoke(new MethodInvoker(delegate
                        {
                            DialogResult = DialogResult.Cancel;
                            Close();
                        }));
                    }

                });
            }
            else
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void exportBtn_Click(object sender, EventArgs e)
        {
            if (textureView.SelectedIndices.Count == 0) return;

            if (saveTextureFileDialog.ShowDialog() != DialogResult.OK) return;

            string fileName = saveTextureFileDialog.FileName;

            ActionOnTextureByIndex(textureView.SelectedIndices[0], (t) => { t.getTextureImage().Save(fileName); });
        }

        private void exportAllButton_Click(object sender, EventArgs e)
        {
            if (exportFolderBrowserDialog.ShowDialog() != DialogResult.OK) return;

            Enabled = false;
            Application.DoEvents();

            try
            {
                string path = exportFolderBrowserDialog.SelectedPath;

                for (int i = 0; i < main.level.textures.Count; i++)
                {
                    Bitmap image = main.level.textures[i].getTextureImage();
                    image.Save(path + "/" + i.ToString() + ".png");
                }

                for (int i = 0; i < main.level.armorTextures.Count; i++)
                {
                    List<Texture> textures = main.level.armorTextures[i];
                    for (int j = 0; j < textures.Count; j++)
                    {
                        Bitmap image = textures[j].getTextureImage();
                        image.Save(path + "/armor" + i + "_" + j.ToString() + ".png");
                    }
                }

                for (int i = 0; i < main.level.gadgetTextures.Count; i++)
                {
                    Bitmap image = main.level.gadgetTextures[i].getTextureImage();
                    image.Save(path + "/gadget_" + i.ToString() + ".png");
                }

                for (int i = 0; i < main.level.missions.Count; i++)
                {
                    List<Texture> textures = main.level.missions[i].textures;
                    for (int j = 0; j < textures.Count; j++)
                    {
                        Bitmap image = textures[j].getTextureImage();
                        image.Save(path + "/mission" + i + "_" + j.ToString() + ".png");
                    }
                }
            }
            finally
            {
                Enabled = true;
            }


        }
    }
}
