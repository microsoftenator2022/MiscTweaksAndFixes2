using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Wat.Extensions;

using static Kingmaker.Cheats.StateExplorer.CustomIndexers;

namespace Wat
{
    internal class Stack<T> : IEnumerable<T>
    {
        private readonly List<T> collection = new();

        public IEnumerable<T> Items => collection;

        public int Count => collection.Count;

        public void Push(T item) => collection.Add(item);

        public T Pop()
        {
            if(collection.Count == 0) throw new InvalidOperationException();

            var lastIndex = collection.Count - 1;

            var item = collection[lastIndex];
            collection.RemoveAt(lastIndex);
            
            return item;
        }

        public T Peek()
        {
            if (collection.Count == 0) throw new InvalidOperationException();

            return collection[collection.Count - 1];
        }

        public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class Wad : IDisposable
    {
        public enum WadType
        {
            IWAD,
            PWAD
        }
                     
        /// <summary>
        /// Wad header data (wadinfo_t)
        /// </summary>
        /// <param name="WadType">
        /// The ASCII characters "IWAD" or "PWAD".<br/>
        /// <code>
        /// Name:   identification
        /// Offset:           0x00
        /// Length:              4
        /// </code>
        /// </param>
        /// <param name="LumpCount">
        /// An integer specifying the number of lumps in the WAD.<br />
        /// <code>
        /// Name:         numlumps
        /// Offset:           0x04
        /// Length:              4
        /// </code>
        /// </param>
        /// <param name="DirectoryPtr">
        /// An integer holding a pointer to the location of the directory.<br />
        /// <code>
        /// Name:     infotableofs
        /// Offset:           0x08
        /// Length:              4
        /// </code>
        /// </param>
        public readonly record struct WadHeader(WadType WadType, int LumpCount, int DirectoryPtr);

        /// <summary>
        /// Directory entry data (filelump_t)
        /// </summary>
        /// <param name="LumpPtr">
        /// An integer holding a pointer to the start of the lump's data in the file.<br />
        /// <code>
        /// Name:        filepos
        /// Offset:         0x00
        /// Length:            4
        /// </code>
        /// </param>
        /// <param name="Size">
        /// An integer representing the size of the lump in bytes.
        /// <code>
        /// Name:       numlumps
        /// Offset:         0x04
        /// Length:            4
        /// </code>
        /// </param>
        /// <param name="Name">
        /// An ASCII string defining the lump's name.<br />
        /// The name has a limit of 8 characters and should be null padded if less than 8 characters long.<br />
        /// <code>
        /// Name:   infotableofs
        /// Offset:         0x08
        /// Length:            8
        /// </code>
        /// </param>
        public readonly record struct DirectoryEntry(int LumpPtr, int Size, string Name);

        internal Stream stream;

        public Wad(string path)
        {
            stream = new FileStream(path, FileMode.Open);
        }

        public Wad(Stream s)
        {
            stream = s;
        }

        public static Wad Open(string path) => new Wad(path);

        private WadHeader? header;

        internal WadHeader GetHeaderFromFile()
        {
            stream.Seek(0, SeekOrigin.Begin);

            var wadType = stream.ReadString(4);
            var lumpCount = stream.ReadInt();
            var directoryOffset = stream.ReadInt();

            if (wadType != "IWAD" && wadType != "PWAD") throw new FormatException();

            return new WadHeader
            (
                WadType: wadType == "IWAD" ? WadType.IWAD : WadType.PWAD,
                LumpCount: lumpCount,
                DirectoryPtr: directoryOffset
            );
        }

        public WadHeader Header => header ??= GetHeaderFromFile();

        internal DirectoryEntry GetEntryFromFile(int index)
        {
            if(index > Header.LumpCount) throw new ArgumentException();

            var offset = Header.DirectoryPtr + (index * 16);

            stream.Seek(offset, SeekOrigin.Begin);

            var lumpPtr = stream.ReadInt();
            var size = stream.ReadInt();
            var name = stream.ReadString(8);

            return new DirectoryEntry(LumpPtr: lumpPtr, Size: size, Name: name);
        }

        public class WadDirectory : IEnumerable<DirectoryEntry>
        {
            private readonly Func<int, DirectoryEntry> getDirectoryEntry;

            internal DirectoryEntry[] Entries;
            
            internal WadDirectory(Func<int, DirectoryEntry> getDirectoryEntry, int entryCount)
            {
                this.getDirectoryEntry = getDirectoryEntry;
                Entries = new DirectoryEntry[entryCount]; 
            }

            public int Count => Entries.Length;

            public IEnumerable<DirectoryEntry> GetEntries()
            {
                for (var i = 0; i < Count; i++)
                    yield return this[i];
            }

            public DirectoryEntry this[int index]
            {
                get
                {
                    if(index > Entries.Length) throw new ArgumentException();

                    if (Entries[index] == default)
                    {
                        Entries[index] = getDirectoryEntry(index);
                    }

                    return Entries[index];
                }
            }

            public IEnumerator<DirectoryEntry> GetEnumerator() => GetEntries().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }

        private WadDirectory? directory = null;
        public WadDirectory Directory => directory ??= new(GetEntryFromFile, Header.LumpCount);

        public readonly record struct Lump(List<string> Namespaces, string Name, Lazy<byte[]> Data);

        internal byte[] GetLumpBytes(DirectoryEntry entry)
        {
            stream.Seek(entry.LumpPtr, SeekOrigin.Begin);

            if(entry.Size == 0) return new byte[0];

            var bytes = new byte[entry.Size];

            stream.Read(bytes, 0, entry.Size);

            return bytes;
        }

        public IEnumerable<Lump> GetLumps()
        {
            var nss = new Stack<string>();

            foreach (var entry in Directory)
            {
                if(entry.Name.EndsWith("_START"))
                    nss.Push(entry.Name.Replace("_START", ""));
                if(nss.Any() && entry.Name.StartsWith($"{nss.Peek()}_END"))
                    nss.Pop();

                yield return new Lump(Namespaces: nss.ToList(), entry.Name, new Lazy<byte[]>(() => GetLumpBytes(entry)));
            }
        }

        public Lump GetLump(IEnumerable<string> nss, string name) =>
            GetLumps().First(l =>
                l.Namespaces.Count == nss.Count()
                && (nss.Count() == 0 || nss.Zip(l.Namespaces, String.Equals).All(id => id))
                && l.Name == name);

        public Lump GetLump(string name) => GetLump(Enumerable.Empty<string>(), name);

        public void Dispose()
        {
            var stream = this.stream;
            if (stream == null) return;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            this.stream = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            stream.Dispose();
        }
    }
}
