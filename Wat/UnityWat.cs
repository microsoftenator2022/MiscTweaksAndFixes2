using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using Wat;

namespace UnityWat
{
    // TODO: Replace with compute shader?
    internal static class UnityWat
    {
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

        public static Texture2D CreateTexture(this PatchImage patch, Palette palette)
        {
            var texture = new Texture2D(patch.Width, patch.Height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            return patch.WriteToTexture(texture, palette);
        }

        public static Texture2D CreateTexture(byte[] bytes, Palette palette) =>
            CreateTexture(new PatchImage(bytes), palette);
    }
}
