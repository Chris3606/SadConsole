﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SadConsole;
using SadRogue.Primitives;

namespace SadConsole.Readers
{
    /// <summary>
    /// Playscii converter. Check this excellent ascii editor out at http://vectorpoem.com/playscii/
    /// </summary>
    public class Playscii
    {
        /// <summary>
        /// Palette file extensions supported by the Playscii format.
        /// </summary>
        readonly string[] PALETTE_EXTENSIONS;

        /// <summary>
        /// Maximum amount of colors supported by the Playscii format.
        /// </summary>
        const int MAX_COLORS = 1024;

        /// <summary>
        /// Name of the font file.
        /// </summary>
        public string charset;

        /// <summary>
        /// Hold all the animation frames.
        /// </summary>
        public Frame[] frames;

        /// <summary>
        /// Surface height.
        /// </summary>
        public int height;

        /// <summary>
        /// Name of the palette file.
        /// </summary>
        public string palette;

        /// <summary>
        /// Surface width.
        /// </summary>
        public int width;

        /// <summary>
        /// Basic class constructor.
        /// </summary>
        public Playscii()
        {
            PALETTE_EXTENSIONS = new string[] { "png", "gif", "bmp" };
        }

        /// <summary>
        /// Json frame object in the <see cref="Playscii"/> file.
        /// </summary>
        public struct Frame
        {
            /// <summary>
            /// Duration for this frame.
            /// </summary>
            public float delay;

            /// <summary>
            /// <see cref="Playscii"/> frame layers that will be converted to <see cref="ScreenSurface"/>.
            /// </summary>
            public Layer[] layers;

            /// <summary>
            /// Converts the <see cref="Playscii"/> frame to SadConsole <see cref="ScreenSurface"/>.
            /// </summary>
            /// <param name="width">Width of the <see cref="ScreenSurface"/>.</param>
            /// <param name="height">Height of the <see cref="ScreenSurface"/>.</param>
            /// <param name="font"><see cref="IFont"/> to be used when creating the <see cref="ScreenSurface"/>.</param>
            /// <param name="colors"><see cref="Palette"/> of colors converted from the <see cref="Playscii"/> format.</param>
            /// <returns><see cref="ScreenSurface"/> containing the image from the first animation frame.</returns>
            public ScreenSurface ToScreenSurface(int width, int height, IFont font, Palette colors)
            {
                var output = new ScreenSurface(width, height) { Font = font };
                if (layers.Length > 0)
                    foreach (var layer in layers)
                        layer.ToSurface(output, colors);
                return output;
            }
        }

        /// <summary>
        /// Json layer in the <see cref="Playscii"/> file.
        /// </summary>
        public struct Layer
        {
            /// <summary>
            /// Layer name.
            /// </summary>
            public string name;

            /// <summary>
            /// <see cref="Playscii"/> tiles that will be converted to <see cref="ColoredGlyph"/>.
            /// </summary>
            public Tile[] tiles;

            /// <summary>
            /// Visibility of this layer.
            /// </summary>
            public bool visible;

            /// <summary>
            /// Converts the <see cref="Playscii"/> layer to a SadConsole <see cref="ScreenSurface"/>.
            /// </summary>
            /// <param name="parent"><see cref="ScreenSurface"/> that represents Playscii frame holding this layer.</param>
            /// <param name="colors"><see cref="Palette"/> of colors converted from the <see cref="Playscii"/> format.</param>
            /// <returns><see cref="ScreenSurface"/> containg the given <see cref="Playscii"/> layer.</returns>
            public ScreenSurface ToSurface(ScreenSurface parent, Palette colors)
            {
                if (parent.Surface.Count != tiles.Length) throw new ArgumentException("Surface size must match number of tiles.");

                var output = new ScreenSurface(parent.Surface.Width, parent.Surface.Height) { Parent = parent, Font = parent.Font, IsVisible = visible };
                for (int y = 0; y < output.Surface.Height; y++)
                    for (int x = 0; x < output.Surface.Width; x++)
                    {
                        int tileIndex = y * output.Surface.Width + x;
                        if (tileIndex < tiles.Length)
                        {
                            var coloredGlyph = tiles[tileIndex].ToColoredGlyph(output.Font, colors);
                            output.Surface.SetCellAppearance(x, y, coloredGlyph);
                        }
                    }

                return output;
            }
        }

        /// <summary>
        /// Json tile in the <see cref="Playscii"/> file.
        /// </summary>
        public struct Tile
        {
            /// <summary>
            /// Tile background color.
            /// </summary>
            public int bg;

            /// <summary>
            /// Tile character index.
            /// </summary>
            [JsonProperty("char")]
            public int glyph;

            /// <summary>
            /// Tile foreground color.
            /// </summary>
            public int fg;

            /// <summary>
            /// Tile rotation and mirror.
            /// </summary>
            public byte xform;

            /// <summary>
            /// Converts the <see cref="Playscii"/> tile to a SadConsole <see cref="ColoredGlyph"/>.
            /// </summary>
            /// <param name="font"><see cref="IFont"/> to be used when creating the <see cref="ScreenSurface"/>.</param>
            /// <param name="colors"><see cref="Palette"/> of colors converted from the <see cref="Playscii"/> format.</param>
            /// <returns><see cref="ColoredGlyph"/> equivalent of the <see cref="Playscii"/> tile.</returns>
            public ColoredGlyph ToColoredGlyph(IFont font, Palette colors)
            {
                if (bg < 0 || fg < 0 || bg >= colors.Length || fg >= colors.Length) throw new IndexOutOfRangeException("Glyph color out of palette range.");
                if (glyph < 0 || glyph >= font.TotalGlyphs) throw new IndexOutOfRangeException("Glyph index out of font range.");

                Mirror mirror = xform switch
                {
                    // 1 => Rotation.90
                    // 2 => Rotation.180
                    2 => Mirror.Vertical,
                    // 3 => Rotation.270
                    4 => Mirror.Horizontal,
                    5 => Mirror.Vertical,
                    _ => Mirror.None
                };
                Color foreground = colors[fg];
                Color background = colors[bg];
                return new ColoredGlyph(foreground, background, glyph, mirror);
            }
        }

        /// <summary>
        /// Converts <see cref="Playscii"/> palette file to a SadConsole <see cref="Palette"/>.
        /// </summary>
        /// <param name="fileName">Name and path of the palette file.</param>
        /// <returns><see cref="Palette"/> of <see cref="Playscii"/> colors.</returns>
        static Palette GetPalette(string fileName)
        {
            var colors = new List<Color>() { new Color(0, 0, 0, 0) };
            using (ITexture image = GameHost.Instance.GetTexture(fileName))
            {
                Point imageSize = (image.Width, image.Height);

                for (var y = 0; y < imageSize.Y; y++)
                {
                    for (var x = 0; x < imageSize.X; x++)
                    {
                        if (colors.Count >= MAX_COLORS)
                            throw new ArgumentException("Palette file contains more colors than the Playscii format allows.");

                        var color = image.GetPixel(new Point(x, y));
                        if (!colors.Contains(color))
                            colors.Add(color);
                    }
                }
            }
            return new Palette(colors);
        }

        /// <summary>
        /// Reads the <see cref="Playscii"/> Json file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>Deserialised object containing <see cref="Playscii"/> save file data.</returns>
        static Playscii GetPlayscii(string fileName)
        {
            using (StreamReader file = File.OpenText(fileName))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JObject o2 = (JObject)JToken.ReadFrom(reader);
                return (Playscii)o2.ToObject(typeof(Playscii));
            }
        }

        /// <summary>
        /// Converts a <see cref="Playscii"/> file to a SadConsole <see cref="ScreenSurface"/>.
        /// </summary>
        /// <param name="fileName">Name and path of the <see cref="Playscii"/> file.</param>
        /// <param name="font"><see cref="IFont"/> to be used when converting the <see cref="Playscii"/> file.</param>
        /// 
        /// <remarks>SadConsole does not support all the Playscii features at the moment, so the conversion will not be perfect.<br></br>
        /// Use only tile flip (mirror in SadConsole), but not the rotation. Set Z-Depth to 0 on all Playscii layers.<br></br>
        /// Transparent glyph foreground is fine, but it will not cut through the <see cref="ColoredGlyph"/> background like it does in Playscii.</remarks>
        /// 
        /// <returns><see cref="ScreenObject"/> containing the first frame from the <see cref="Playscii"/> file.</returns>
        public static ScreenSurface ToScreenSurface(string fileName, IFont font)
        {
            ScreenSurface output = null;

            // get the dir name from the file name
            string dirName = Path.GetDirectoryName(fileName);

            // read playscii json file
            if (GetPlayscii(fileName) is Playscii p)
            {
                // read pallete file and generate colors
                Palette palette = GetPalette($"{dirName}/{p.palette}.png");

                // convert the first frame to a screen surface
                if (p.frames.Length > 0)
                    output = p.frames[0].ToScreenSurface(p.width, p.height, font, palette);
            }

            return output;
        }
    }
}
