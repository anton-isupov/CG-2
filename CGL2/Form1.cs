using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.IO;


namespace CGL2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            trackBar2.Maximum = 1000;
            trackBar3.Minimum = 500;
            trackBar3.Maximum = 3000;
        }

        static int Clamp(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        static Bin bin = new Bin();
        static View view = new View();

        class Bin
        {
            public static int X, Y, Z;
            public static short[] arr;
            public Bin() { }
            public void readBin(string path)
            {
                if (File.Exists(path))
                {
                    BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open));
                    X = reader.ReadInt32();
                    Y = reader.ReadInt32();
                    Z = reader.ReadInt32();
                    int arraySize = X * Y * Z;
                    arr = new short[arraySize];
                    for (int i = 0; i < arraySize; i++)
                    {
                        arr[i] = reader.ReadInt16();
                    }
                }
                return;
            }
        }

        class View
        {
            Color TransferFunction(short value, int min, int max_min)
            {
                int newVal = Clamp((value - min) * 255 / max_min, 0, 255);
                return Color.FromArgb(255, newVal, newVal, newVal);
            }
            public void SetupView(int width, int height)
            {
                GL.ShadeModel(ShadingModel.Smooth);
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(0, Bin.X, 0, Bin.Y, -1, 1);
                GL.Viewport(0, 0, width, height);
            }

            public void DrawQuads(int layerNumber, int min, int maxmin)
            {
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.Begin(BeginMode.Quads);
                for (int x_coord = 0; x_coord < Bin.X - 1; x_coord++)
                {
                    for (int y_coord = 0; y_coord < Bin.Y; y_coord++)
                    {
                        short value;

                        value = Bin.arr[x_coord + y_coord * Bin.X + layerNumber * Bin.X * Bin.Y];
                        GL.Color3(TransferFunction(value, min, maxmin));
                        GL.Vertex2(x_coord, y_coord);

                        value = Bin.arr[x_coord + (y_coord + 1) * Bin.X + layerNumber * Bin.X * Bin.Y];
                        GL.Color3(TransferFunction(value, min, maxmin));
                        GL.Vertex2(x_coord, y_coord + 1);

                        value = Bin.arr[x_coord + 1 + (y_coord + 1) * Bin.X + layerNumber * Bin.X * Bin.Y];
                        GL.Color3(TransferFunction(value, min, maxmin));
                        GL.Vertex2(x_coord + 1, y_coord + 1);

                        value = Bin.arr[x_coord + 1 + y_coord * Bin.X + layerNumber * Bin.X * Bin.Y];
                        GL.Color3(TransferFunction(value, min, maxmin));
                        GL.Vertex2(x_coord + 1, y_coord);
                    }
                }
                GL.End();
            }

            public void DrawQuadstrip(int layerNumber, int min, int max_min)
            {
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                for (int x_coord = 0; x_coord < Bin.X - 1; x_coord++)
                {
                    GL.Begin(BeginMode.QuadStrip);
                    short value;

                    value = Bin.arr[x_coord + 0 * Bin.X + layerNumber * Bin.X * Bin.Y];
                    GL.Color3(TransferFunction(value, min, max_min));
                    GL.Vertex2(x_coord, 0);

                    value = Bin.arr[x_coord + 1 + 0 * Bin.X + layerNumber * Bin.X * Bin.Y];
                    GL.Color3(TransferFunction(value, min, max_min));
                    GL.Vertex2(x_coord + 1, 0);

                    for (int y_coord = 1; y_coord < Bin.Y - 1; y_coord++)
                    {
                        value = Bin.arr[x_coord + 1 + (y_coord + 1) * Bin.X + layerNumber * Bin.X * Bin.Y];
                        GL.Color3(TransferFunction(value, min, max_min));
                        GL.Vertex2(x_coord + 1, y_coord + 1);

                        value = Bin.arr[x_coord + (y_coord + 1) * Bin.X + layerNumber * Bin.X * Bin.Y];
                        GL.Color3(TransferFunction(value, min, max_min));
                        GL.Vertex2(x_coord, y_coord + 1);
                    }
                    GL.End();
                }
            }

            Bitmap textureImage;
            int VBOTexture;
            public void Load2DTexture()
            {
                GL.BindTexture(TextureTarget.Texture2D, VBOTexture);
                BitmapData data = textureImage.LockBits(
                    new System.Drawing.Rectangle(0, 0, textureImage.Width, textureImage.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                    data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                    PixelType.UnsignedByte, data.Scan0);

                textureImage.UnlockBits(data);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                ErrorCode er = GL.GetError();
                string str = er.ToString();
            }

            public void genereateTextureImage(int layerNumber, int min, int max_min)
            {
                textureImage = new Bitmap(Bin.X, Bin.Y);
                for (int i = 0; i < Bin.X; i++)
                {
                    for (int j = 0; j < Bin.Y; j++)
                    {
                        int pixelNumber = i + j * Bin.X + layerNumber * Bin.X * Bin.Y;
                        textureImage.SetPixel(i, j, TransferFunction(Bin.arr[pixelNumber], min, max_min));
                    }
                }
            }

            public void DrawTexture()
            {
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.Enable(EnableCap.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, VBOTexture);

                GL.Begin(BeginMode.Quads);
                GL.Color3(Color.White);
                GL.TexCoord2(0f, 0f);
                GL.Vertex2(0, 0);
                GL.TexCoord2(0f, 1f);
                GL.Vertex2(0, Bin.Y);
                GL.TexCoord2(1f, 1f);
                GL.Vertex2(Bin.X, Bin.Y);
                GL.TexCoord2(1f, 0f);
                GL.Vertex2(Bin.X, 0);
                GL.End();

                GL.Disable(EnableCap.Texture2D);
            }
        }

        bool loaded = false;
        int currentLayer;
        bool needReload = false;
        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string str = dialog.FileName;
                bin.readBin(str);
                trackBar1.Maximum = Bin.Z - 1;
                view.SetupView(glControl1.Width, glControl1.Height);
                loaded = true;
                glControl1.Invalidate();
            }
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            if (loaded)
            {
                if (radioButton1.Checked == true)
                {
                    view.DrawQuadstrip(currentLayer, trackBar2.Value, trackBar3.Value);
                    glControl1.SwapBuffers();
                }
                if (radioButton3.Checked == true)
                {
                    view.DrawQuads(currentLayer, trackBar2.Value, trackBar3.Value);
                    glControl1.SwapBuffers();
                }
                if (radioButton2.Checked == true)
                {
                    if (needReload)
                    {
                        view.genereateTextureImage(currentLayer, trackBar2.Value, trackBar3.Value);
                        view.Load2DTexture();
                        needReload = false;
                    }
                    view.DrawTexture();
                    glControl1.SwapBuffers();
                }
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            currentLayer = trackBar1.Value;
            needReload = true;
        }

        void Application_Idle(object sender, EventArgs e)
        {
            while (glControl1.IsIdle)
            {
                displayFPS();
                glControl1.Invalidate();
            }
        }

        int FrameCount;
        DateTime NextFPSUpdate = DateTime.Now.AddSeconds(1);
        void displayFPS()
        {
            if (DateTime.Now >= NextFPSUpdate)
            {
                this.Text = String.Format("CT Visualizer (fps = {0})", FrameCount);
                NextFPSUpdate = DateTime.Now.AddSeconds(1);
                FrameCount = 0;
            }
            FrameCount++;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Application.Idle += Application_Idle;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            needReload = true;
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            needReload = true;
        }

        private void glControl1_Load(object sender, EventArgs e)
        {

        }
    }
}
