using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace Wat
{   
    public static class UnityWat
    {
        //internal static Palette GetPalette(int number = 0) => new(Properties.Resources.PLAYPAL, number);
        
        // TODO: Replace with compute shader?
        public static Texture2D WriteToTexture(this PatchImage patch, Texture2D texture, Palette palette)
        {
            if (patch.Width > texture.width || patch.Height > texture.height) throw new ArgumentException();

            var pixels = patch.ToImagePixels(palette);

            var rawTexture = texture.GetRawTextureData<Color32>();

            var textureIndex = 0;
            for (var y = 0; y < patch.Height; y++)
                for (var x = 0; x < patch.Width; x++)
                {
                    //texture.SetPixel(x, patch.Height - 1 - y, pixels[x][y]);
                    rawTexture[textureIndex++] = pixels[x][patch.Height - 1 - y];
                }

            texture.Apply();

            return texture;
        }

        //internal static Texture2D WriteToTexture(this PatchImage patch, Texture2D texture) =>
        //    WriteToTexture(patch, texture, GetPalette());

        public static Texture2D CreateTextureWithBackround(this PatchImage patch, PatchImage background, Palette palette, int xOffset = -1, int yOffset = -1)
        {
            if (background.Width < patch.Width || background.Height < patch.Height) throw new ArgumentException();

            var texture = new Texture2D(patch.Width, patch.Height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            var patchPixels = patch.ToImagePixels(palette);
            var backgroundPixels = background.ToImagePixels(palette);

            if (xOffset < 0 && background.Width > patch.Width)
                xOffset = (background.Width - patch.Width) / 2;

            if (yOffset < 0 && background.Height > patch.Height)
                yOffset = (background.Height - patch.Height);

            var rawTexture = texture.GetRawTextureData<Color32>();
            var textureIndex = 0;
            for (var y = 0; y < patch.Height; y++)
                for (var x = 0; x < patch.Width; x++)
                {
                    var pixelColor = patchPixels[x][patch.Height - 1 - y];
                    if (pixelColor.a == 0f)
                        pixelColor = backgroundPixels[x + xOffset][y + yOffset];

                    rawTexture[textureIndex++] = pixelColor;
                }

            texture.Apply();

            return texture;
        }

        //internal static Texture2D CreateTextureWithBackround(this PatchImage patch, PatchImage background) =>
        //    CreateTextureWithBackround(patch, background, GetPalette());

        public static Texture2D CreateTexture(this PatchImage patch, Palette palette)
        {
            var texture = new Texture2D(patch.Width, patch.Height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            patch.WriteToTexture(texture, palette);

            return texture;
        }

        //internal static Texture2D CreateTexture(this PatchImage patch) => CreateTexture(patch, GetPalette());

        public static Texture2D CreateTexture(byte[] bytes, Palette palette) =>
            CreateTexture(new PatchImage(bytes), palette);

        //internal static Texture2D CreateTexture(byte[] bytes) => CreateTexture(bytes, GetPalette());
    }
}
