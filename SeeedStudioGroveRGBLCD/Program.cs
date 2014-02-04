using System.Threading;

namespace SeeedStudio.Grove.RGBLCD
{
    public class Program
    {
        // initialise the LCD display
        static LCD mylcd = new LCD();

        public static void Main()
        {
            int sleep = 1;
            byte i = 255;
            byte j = 255;
            byte k = 255;
            mylcd.SetCursor(0, 0); // column, row
            mylcd.setRGB(i, j, k);
            mylcd.noCursor();
            byte[] message = System.Text.Encoding.UTF8.GetBytes("  Hello World!");
            mylcd.write(message);

            while (true)
            {
                for (i = 255; i > 0; i--)
                {
                    mylcd.setRGB(i, j, k);
                    Thread.Sleep(sleep);
                }
                for (i = 0; i < 255; i++)
                {
                    mylcd.setRGB(i, j, k);
                    Thread.Sleep(sleep);
                }
                for (j = 255; j > 0; j--)
                {
                    mylcd.setRGB(i, j, k);
                    Thread.Sleep(sleep);
                }
                for (j = 0; j < 255; j++)
                {
                    mylcd.setRGB(i, j, k);
                    Thread.Sleep(sleep);
                }
                for (k = 255; k > 0; k--)
                {
                    mylcd.setRGB(i, j, k);
                    Thread.Sleep(sleep);
                }
                for (k = 0; k < 255; k++)
                {
                    mylcd.setRGB(i, j, k);
                    Thread.Sleep(sleep);
                }
            }
        }

    }
}