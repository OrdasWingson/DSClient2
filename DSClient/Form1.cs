using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DSClient
{

    [Flags]
    public enum MouseEventFlags 
    {
        LEFTDOWN = 0x00000002,
        LEFTUP = 0x00000004,
        MIDDLEDOWN = 0x00000020,
        MIDDLEUP = 0x00000040,
        MOVE = 0x00000001,
        ABSOLUTE = 0x00008000,
        RIGHTDOWN = 0x00000008,
        RIGHTUP = 0x00000010
    }

    public partial class Form1 : Form
    {
        
        Thread screenThread;
        Thread commandThread;
        Socket socketSend;
        Socket socketRecive;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);


        public Form1()
        {
            InitializeComponent();            
        }

        
        private void startBtn_Click(object sender, EventArgs e)
        {
            
            socketSend = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socketRecive = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                //socket.Connect("127.0.0.1", 8080);
                socketSend.Connect("192.168.0.104", 8080);
                socketRecive.Connect("192.168.0.104", 8081);
                screenThread = new Thread(sendMes);
                screenThread.Start();
                commandThread = new Thread(recivMes);
                commandThread.Start();
                
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

        public void sendMes()
        {
            //int bufSize = 1250000;
            
            try
            {
                //socket.Connect("127.0.0.1", 8080);
                ////socket.Connect("192.168.0.4", 8080);
                while (true)
                {
                    byte[] data = ImageToByteArray(Snapshots(800, 600, false));
                    //using (MemoryStream ms = new MemoryStream(new byte[bufSize], 0, bufSize, true, true))
                    using (MemoryStream ms = new MemoryStream(new byte[data.Length], 0, data.Length, true, true))
                    {

                        BinaryReader reader = new BinaryReader(ms);
                        BinaryWriter writer = new BinaryWriter(ms);//запись в поток
                        ms.Position = 0;
                        writer.Write(data);
                        socketSend.Send(ms.GetBuffer());
                        ms.Position = 0;
                        socketSend.Receive(ms.GetBuffer());
                        byte[] test = reader.ReadBytes(bufSize);
                        
                    }
                }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString() + " SendMes " );
            }
        }

        private void recivMes()
        {                     
            int xM, yM, eventM, height, width;
            float xDiv, yDiv;
            Size resolution = Screen.PrimaryScreen.Bounds.Size;
            try
            {
                ////socket.Connect("192.168.0.4", 8080);  
                using (MemoryStream ms = new MemoryStream(new byte[512], 0, 512, true, true))
                {
                    BinaryReader reader = new BinaryReader(ms);//чтение из потока
                    BinaryWriter writer = new BinaryWriter(ms);
                    while (true)
                    {
                        ms.Position = 0;
                        socketRecive.Receive(ms.GetBuffer());              
                        xM = reader.ReadInt32();
                        yM = reader.ReadInt32();
                        eventM = reader.ReadInt32();
                        height = reader.ReadInt32();
                        width = reader.ReadInt32();
                        xDiv = (float)resolution.Width * xM / width;
                        yDiv = ((float)resolution.Height * yM / height);//
                        var outputX = xDiv * 65535 / resolution.Width;
                        var outputY = yDiv * 65535 / resolution.Height;
                        
                        mouse_event((uint)MouseEventFlags.ABSOLUTE | (uint)MouseEventFlags.MOVE, (uint)outputX, (uint)outputY, 0, 0);
                        mouse_event((uint)MouseEventFlags.ABSOLUTE | (uint)MouseEventFlags.LEFTDOWN, (uint)outputX, (uint)outputY, 0, 0);
                        mouse_event((uint)MouseEventFlags.ABSOLUTE | (uint)MouseEventFlags.LEFTUP, (uint)outputX, (uint)outputY, 0, 0);
                        
                    }

                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString() + " ReciveMes");
            }
        }

        public Image Snapshots( int width, int height, bool showCursor)
        {
            Size size = new Size(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);

            Bitmap srcImage = new Bitmap(size.Width, size.Height);
            using (Graphics srcGraphics = Graphics.FromImage(srcImage))
            {                
                    Rectangle src = new Rectangle(0, 0, size.Width, size.Height);
                    Rectangle dst = new Rectangle(0, 0, width, height);
                    Size curSize = new Size(32, 32);


                    srcGraphics.CopyFromScreen(0, 0, 0, 0, size);

                    if (showCursor)
                        Cursors.Default.Draw(srcGraphics, new Rectangle(Cursor.Position, curSize));

                    
             }
            return srcImage;
        }

        public byte[] ImageToByteArray(Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                imageIn.Save(ms, ImageFormat.Jpeg);//
                return ms.ToArray();
            }
        }       
    }


}
