using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Imaging;

namespace RL6Mans
{
    class ImageCreation
    {
        
        public static byte[] CreateQueueImage(
          int playersInQueue, int playersInVoiceQueue)
        {
            using (var bmp = new Bitmap(600, 200))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Yellow);
                    Pen pen = new Pen(Color.Black);
                    pen.Width = 1;

                    //Draw red rectangle to go behind cross
                    Rectangle rect = new Rectangle(0,0, playersInQueue * 100, 200);
                    g.FillRectangle(new SolidBrush(Color.Green), rect);
                    Rectangle rect2 = new Rectangle(playersInQueue * 100, 0, (6 - playersInQueue) * 100, 200);
                    g.FillRectangle(new SolidBrush(Color.Red), rect2);
                    Rectangle rect3 = new Rectangle(playersInQueue * 100, 0, playersInVoiceQueue * 100, 200);
                    g.FillRectangle(new SolidBrush(Color.Blue), rect3);

                    var f = new Font(FontFamily.GenericMonospace, 125.0f, FontStyle.Bold);

                    g.DrawString((playersInQueue + playersInVoiceQueue) + " / 6", f, new SolidBrush(Color.White), new PointF(10, 10));

                }

                var memStream = new MemoryStream();
                bmp.Save("queue.png", ImageFormat.Png);
                bmp.Save(memStream, ImageFormat.Png);
                return memStream.ToArray();
            }
        }

    }
}
