using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;

using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace Wat
{
    //public struct Maybe<T>
    //{
    //    public bool HasValue { get; private set; }

    //    private readonly T? value;
    //    public T Value => value ?? throw new NullReferenceException();

    //    private Maybe(bool hasValue, T? value = default)
    //    {
    //        HasValue = hasValue;
    //        this.value = value;
    //    }

    //    public static readonly Maybe<T> Nothing = new(false);

    //    public static Maybe<T> Just(T value) => new(true, value);

    //    public static Func<Maybe<T1>, Maybe<T2>> FMap<T1, T2>(Func<T1, T2> f) =>
    //        mx => mx.HasValue ? Maybe<T2>.Just(f(mx.Value)) : Maybe<T2>.Nothing;
    //}

    public readonly struct Palette
    {
        internal readonly Color32[] Colors = new Color32[256];

        private static void Init(out Color32[] colors, byte[] bytes, int index)
        {
            if (bytes.Length != 768 * 14) throw new ArgumentException();

            var paletteBytes = bytes.Skip(index * 768).Take(768);

            colors =
                paletteBytes
                    .ChunkBySize(3)
                    .Select(rgb => rgb.ToArray())
                    .Select(rgb => new Color32(rgb[0], rgb[1], rgb[2], 255))
                    .ToArray();
        }

        public Palette(byte[] bytes, int index = 0)
        {
            Init(out Colors, bytes, index);

            //if (bytes.Length != 768 * 14) throw new ArgumentException();

            //var paletteBytes = bytes.Skip(index * 768).Take(768);
            
            //Colors =
            //    paletteBytes
            //        .Batch(3)
            //        .Select(rgb => rgb.ToArray())
            //        .Select(rgb => new Color32(rgb[0], rgb[1], rgb[2], 255))
            //        .ToArray();
        }

        public Palette(Wad.Lump lump, int index = 0)
        {
            if(lump.Name != "PLAYPAL") throw new ArgumentException();
            if(index > 13) throw new ArgumentException();

            var bytes = lump.Data.Value;

            Init(out Colors, bytes, index);

            //if(bytes.Length != 768 * 14) throw new ArgumentException();

            //var paletteBytes = bytes.Skip(index * 768).Take(768);

            //Colors =
            //    paletteBytes
            //        .Batch(3)
            //        .Select(rgb => rgb.ToArray())
            //        .Select(rgb => new Color32(rgb[0], rgb[1], rgb[2], 255))
            //        .ToArray();
        }

        public Palette(Wad wad, int index) : this(wad.GetLump("PLAYPAL"), index) { }

        public readonly record struct PaletteColor(int Number)
        {
            public Color32 ToColor(Palette palette) => palette[this];
        }

        public Color32 this[PaletteColor c] => Colors[c.Number];
    }

    public readonly struct PatchImage
    {
        private readonly byte[] bytes;

        public PatchImage(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public PatchImage(Wad.Lump lump) : this(lump.Data.Value) { }

        public ushort Width => BitConverter.ToUInt16(bytes, 0);
        public ushort Height => BitConverter.ToUInt16(bytes, 2);
        public ushort LeftOffset => BitConverter.ToUInt16(bytes, 4);
        public ushort TopOffset => BitConverter.ToUInt16(bytes, 6);

        internal uint[] ColumnPtrs
        {
            get
            {
                const int arrayStart = 8;

                var columns = new uint[Width];

                for (var i = 0; i < Width; i++)
                {
                    columns[i] = BitConverter.ToUInt32(bytes, arrayStart + (i * 4));
                }

                return columns;
            }
        }

        public class Post
        {
            public readonly byte TopOffset;
            public readonly byte Length;

            private readonly byte[] data;
            public Palette.PaletteColor[] Pixels =>
                data.Select(b => new Palette.PaletteColor(b)).ToArray();

            internal Post(byte[] bytes, int offset = 0)
            {
                TopOffset = bytes[offset];
                Length = bytes[offset + 1];

                var dataOffset = offset + 3;

                data = new byte[Length];

                Array.ConstrainedCopy(bytes, dataOffset, data, 0, Length);
            }

            public static Post[] GetPosts(byte[] bytes, int startOffset)
            {
                var currentOffset = startOffset;
                var posts = new List<Post>();

                while (bytes[currentOffset] != 255)
                {
                    var post = new Post(bytes, currentOffset);
                    posts.Add(post);
                    currentOffset += (post.Length + 4);
                }

                return posts.ToArray();
            }
        }

        internal Post[][] GetColumns()
        {
            var columns = new Post[Width][];

            for (var i = 0; i < columns.Length; i++)
            {
                columns[i] = Post.GetPosts(bytes, (int)ColumnPtrs[i]);
            }

            return columns;
        }

        internal Option<Palette.PaletteColor>[] FullColumn(Post[] posts)
        {
            var pixels = new Option<Palette.PaletteColor>[Height];

            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = Option<Palette.PaletteColor>.None;

            foreach (var post in posts)
            {
                int yOffset = post.TopOffset;
                foreach (var p in post.Pixels)
                {
                    pixels[yOffset] = Option.Some(p);
                    yOffset++;
                }
            }

            //for(var i = 0; i < Height; i++)
            //{
            //    if(i < p.TopOffset) pixels[i] = Maybe<Palette.PaletteColor>.Nothing;
            //    else if(i >= (p.TopOffset + p.Length)) pixels[i] = Maybe<Palette.PaletteColor>.Nothing;
            //    else pixels[i] = Maybe<Palette.PaletteColor>.Just(p.Pixels[i - p.TopOffset]);
            //}

            return pixels;
        }

        public Option<Palette.PaletteColor>[][] GetPalettePixels() => GetColumns().Select(FullColumn).ToArray();

        public Color32[][] ToImagePixels(Palette palette) =>
            GetPalettePixels()
                .Select(c => c.Select(pc => pc.IsSome ? pc.Value.ToColor(palette) : new Color32(0, 0, 0, 0)).ToArray())
                .ToArray();

        //public Bitmap ToBitmap(Palette palette)
        //{
        //    var pixels = ToImagePixels(palette);

        //    var bitmap = new Bitmap(Width, Height);

        //    for(var x = 0; x < Width; x++)
        //    {
        //        for(var y = 0; y < Height; y++)
        //        {
        //            bitmap.SetPixel(x, y, pixels[x][y]);
        //        }
        //    }

        //    return bitmap;
        //}
    }
}
