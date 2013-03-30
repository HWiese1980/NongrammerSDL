using SdlDotNet.Core;
using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Primitives;
using SdlDotNet.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Font = SdlDotNet.Graphics.Font;

namespace NongrammerSDL
{
    public class Game
    {
        private static Surface screen;

        [STAThread]
        public static void Main(string[] args)
        {
            MessageBox.Show("Start!");
            var files = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Bitmaps"), "*.bmp").ToArray();
            var file = files[Playfield.Rnd.Next(0, files.Length)];
            var path = Path.Combine(Assembly.GetExecutingAssembly().Location, @"Bitmaps", file);
            p = new Playfield(10);
            screen = Video.SetVideoMode(1280, 900, 32, false, false, false, true);
            for (int i = 0; i < buff.Length; i++)
            {
                buff[i] = new Surface(screen.Width, screen.Height);
            }
            Events.Quit += Events_Quit;
            Events.MouseButtonUp += EventsOnMouseButtonUp;
            Events.Tick += EventsOnTick;
            Events.Run();
        }

        private static void EventsOnMouseButtonUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (!p.Won) p.ToggleField(mouseButtonEventArgs.X, mouseButtonEventArgs.Y, mouseButtonEventArgs.Button == MouseButton.PrimaryButton);
        }

        private static Playfield p;

        private static Surface[] buff = new Surface[2];

        private static void EventsOnTick(object sender, TickEventArgs tickEventArgs)
        {
            for (int i = 0; i < 1; i++)
            {
                p.Render(buff[i]);
                screen.Blit(buff[i]);
                screen.Update();
            }
        }

        private static void Events_Quit(object sender, QuitEventArgs e)
        {
            Events.QuitApplication();
        }
    }

    public class Playfield
    {
        public static Random Rnd = new Random((int)DateTime.Now.Ticks);
        private int size;

        public bool Won
        {
            get { return bWon; }
        }

        public void ToggleField(int x, int y, bool valid)
        {
            var lx = ((x - homeArea) / _fwidth);
            var ly = ((y - homeArea) / _fheight);
            if (valid)
            {
                _pfields[lx, ly] = !(_pfields[lx, ly]);
                if (_pfields[lx, ly]) _ufields[lx, ly] = false;
            }
            else
            {
                _ufields[lx, ly] = !(_ufields[lx, ly]);
                if (_ufields[lx, ly]) _pfields[lx, ly] = false;
            }
        }

        private Font f;

        public Playfield(int size, string bitmap = "")
        {
            var fpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "Arial.ttf");
            f = new Font(fpath, 10);
            this.size = size;
            _fields = new bool[size, size];
            _pfields = new bool[size, size];
            _ufields = new bool[size, size];

            if (!String.IsNullOrEmpty(bitmap))
            {
                Bitmap bmp = new Bitmap(bitmap);
                _fields = new bool[bmp.Width, bmp.Height];
                _pfields = new bool[bmp.Width, bmp.Height];
                _ufields = new bool[bmp.Width, bmp.Height];

                this.size = bmp.Width;

                for (int row = 0; row < bmp.Height; row++)
                {
                    for (int col = 0; col < bmp.Width; col++)
                    {
                        _fields[col, row] = (bmp.GetPixel(col, row).GetBrightness() > 0.3);
                    }
                }
            }
            else
            {
                for (int row = 0; row < size; row++)
                {
                    for (int col = 0; col < size; col++)
                    {
                        if (Rnd.NextDouble() > 0.4D) _fields[col, row] = true;
                    }
                }
            }
        }

        private readonly bool[,] _fields;
        private readonly bool[,] _pfields;
        private bool[,] _ufields;
        private int _fwidth;
        private int _fheight;

        private const int homeArea = 70;

        private bool bWon = false;

        public void Render(Surface sfc)
        {
            var equal = _pfields.Rank == _fields.Rank &&
                        Enumerable.Range(0, _pfields.Rank).All(dim => _pfields.GetLength(dim) == _fields.GetLength(dim)) &&
                        _pfields.Cast<bool>().SequenceEqual(_fields.Cast<bool>());

            if (!Won && equal)
            {
                bWon = true;
                return;
            }

            var digitHeight = f.SizeText("10").Height;
            var digitWidth = f.SizeText("10").Width;

            sfc.Fill(Color.Black);
            _fwidth = ((sfc.Width - homeArea) / size);
            _fheight = ((sfc.Height - homeArea) / size);
            for (int i = 0; i < size && !Won; i++)
            {
                var col = (i % 5 == 0) ? Color.OrangeRed : Color.White;
                var x = (short)(homeArea + (i * _fwidth));
                var y = (short)(homeArea + (i * _fheight));
                var lh = new Line(x, 0, x, (short)sfc.Height);
                var lv = new Line(0, y, (short)sfc.Width, y);
                lh.Draw(sfc, col, true);
                lv.Draw(sfc, col, true);
            }

            var colcounts = new Dictionary<int, List<int>>();
            var rowcounts = new Dictionary<int, List<int>>();
            for (int col = 0; col < size; col++)
            {
                colcounts[col] = new List<int>();
                bool counting = false;
                int count = 0;
                for (int row = 0; row < size; row++)
                {
                    if (!counting && _fields[col, row]) counting = true;
                    if (counting && !_fields[col, row])
                    {
                        counting = false;
                        colcounts[col].Add(count);
                        count = 0;
                    }
                    if (counting && _fields[col, row]) count++;
                }
                if (counting) colcounts[col].Add(count);
            }

            for (int row = 0; row < size; row++)
            {
                rowcounts[row] = new List<int>();
                bool counting = false;
                int count = 0;
                for (int col = 0; col < size; col++)
                {
                    if (!counting && _fields[col, row]) counting = true;
                    if (counting && !_fields[col, row])
                    {
                        counting = false;
                        rowcounts[row].Add(count);
                        count = 0;
                    }
                    if (counting && _fields[col, row]) count++;
                }
                if (counting) rowcounts[row].Add(count);
            }

            for (int col = 0; col < size && !Won; col++)
            {
                var colc = colcounts[col];
                for (int colcIdx = 0; colcIdx < colc.Count; colcIdx++)
                {
                    var colcSfc = f.Render(String.Format("{0}", colc[colcIdx]), Color.White, true);
                    sfc.Blit(colcSfc, new Rectangle(homeArea + col * _fwidth + 10, colcIdx * (digitHeight) + 3, digitWidth, digitHeight));
                }
            }

            for (int row = 0; row < size && !Won; row++)
            {
                var rowc = rowcounts[row];
                for (int rowcIdx = 0; rowcIdx < rowc.Count; rowcIdx++)
                {
                    var rowcSfc = f.Render(String.Format("{0}", rowc[rowcIdx]), Color.White, true);
                    sfc.Blit(rowcSfc, new Rectangle(rowcIdx * (digitWidth) + 3, homeArea + row * _fheight + 3, digitWidth, digitHeight));
                }
            }

            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                {
                    var x1 = c * _fwidth;
                    var y1 = r * _fheight;
                    if (_ufields[c, r] && !Won)
                    {
                        Line r1 = new Line((short)(x1 + homeArea), (short)(y1 + homeArea), (short)(x1 + _fwidth + homeArea), (short)(y1 + _fheight + homeArea));
                        Line r2 = new Line((short)(x1 + homeArea), (short)(y1 + _fheight + homeArea), (short)(x1 + _fwidth + homeArea), (short)(y1 + homeArea));
                        r1.Draw(sfc, Color.Tomato, true);
                        r2.Draw(sfc, Color.Tomato, true);
                    }
                    else
                    {
                        var col = (_pfields[c, r]) ? ((_fields[c, r]) ? Color.Green : Color.Yellow) : Color.Black;
                        var x = (short)((homeArea + x1) + 1);
                        var y = (short)((homeArea + y1) + 1);

                        Box b = new Box(x, y, (short)((x + _fwidth - 2)), (short)(y + _fheight - 2));
                        b.Draw(sfc, col, true, true);
                    }
                }
        }
    }
}