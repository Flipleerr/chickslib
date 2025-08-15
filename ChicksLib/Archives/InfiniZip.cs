// technically, there is a custom field with file counts and offsets but it's nothing important

using ChicksLib.IO;
using System;
using System.IO;
using System.IO.Compression;

namespace ChicksLib.Archives;

public class InfiniZip
{
    public static int LastCentralDirOffset { get; private set; }
    public record Entry(string FileName, uint LocalHeaderOffset, uint CompressedSize, uint UncompressedSize, byte[] MD5Hash);

    public static List<Entry> ParseCentralDirectory(Stream stream)
    {
        // should not always assume archive is little endian
        var reader = new EndianBinaryReader(stream, Endianness.LittleEndian);
        var entries = new List<Entry>();

        long eocdPosition = -1;
        for (long i = stream.Length - 22; i >= 0; i--)
        {
            stream.Seek(i, SeekOrigin.Begin);
            if (reader.ReadInt32() == 0x06054b50)
            {
                eocdPosition = i;
                break;
            }
        }

        if (eocdPosition < 0)
            throw new InvalidDataException("EOCD not found");

        stream.Seek(eocdPosition, SeekOrigin.Begin);
        reader.ReadInt32(); // skipping signature
        reader.ReadInt16s(3); // skipping a bunch of split archive stuff
        var totalCentralDirRecords = reader.ReadInt16();
        _ = reader.ReadInt32(); // so microsoft stops screaming at me
        var centralDirOffset = reader.ReadInt32();
        LastCentralDirOffset = centralDirOffset;

        stream.Seek(centralDirOffset, SeekOrigin.Begin);

        for (int i = 0; i < totalCentralDirRecords; i++)
        {
            var signature = reader.ReadInt32();
            if (signature != 0x02014b50)
                throw new InvalidDataException($"invalid central directory entry signature: 0x{signature}");

            reader.ReadInt16s(6);

            var crc32 = reader.ReadUInt32(); // Xbox uses MD5 instead
            var compressedSize = reader.ReadUInt32();
            var uncompressedSize = reader.ReadUInt32();
            var fileNameLength = reader.ReadInt16();
            var extraFieldLength = reader.ReadInt16();
            var fileCommentLength = reader.ReadInt16();
            reader.ReadInt16s(2); // disk num and file attributes
            reader.ReadInt32(); // external file attributes
            var localHeaderOffset = reader.ReadUInt32();

            var fileName = reader.ReadString(StringBinaryFormat.FixedLength, fileNameLength);

            byte[]? md5Hash = null;
            if (extraFieldLength > 0)
            {
                var extraFieldStart = stream.Position;
                var extraFieldEnd = extraFieldStart + extraFieldLength;

                while (stream.Position < extraFieldEnd)
                {
                    var fieldID = reader.ReadUInt16();
                    var fieldSize = reader.ReadUInt16();

                    if (fieldID == 0x464B && fieldSize == 19)
                    {
                        reader.ReadBytes(3);
                        md5Hash = reader.ReadBytes(16);
                    }
                    else
                    {
                        reader.ReadBytes(fieldSize); // skip extra field in case there isn't one
                    }
                }
            }

            if (fileCommentLength > 0)
                reader.ReadBytes(fileCommentLength);

            entries.Add(new Entry(
                fileName,
                localHeaderOffset,
                compressedSize,
                uncompressedSize,
                md5Hash
            ));
        }

        return entries;
    }

    public static void ExtractZip(Stream stream)
    {
        var reader = new EndianBinaryReader(stream, Endianness.LittleEndian);
        var entries = ParseCentralDirectory(stream);

        foreach (var (entry, index) in entries.Select((e, i) => (e, i)))
        {
            stream.Seek(entry.LocalHeaderOffset, SeekOrigin.Begin);

            var localHeaderSig = reader.ReadUInt32();
            reader.ReadBytes(22);

            var fileNameLength = reader.ReadInt16();
            var extraFieldLength = reader.ReadInt16();

            reader.ReadBytes(fileNameLength + extraFieldLength);

            var compressedData = reader.ReadBytes((int)entry.CompressedSize);

            byte[]? decompressed = null;

            try
            {
                using var compressedStream = new MemoryStream(compressedData);
                using var zlibStream = new ZLibStream(compressedStream, CompressionMode.Decompress);
                using var output = new MemoryStream();
                zlibStream.CopyTo(output);
                decompressed = output.ToArray();

                if (decompressed.Length != entry.UncompressedSize)
                    decompressed = null;
            }
            catch { decompressed = null; }

            if (decompressed == null)
            {
                try
                {
                    using var compressedStream = new MemoryStream(compressedData);
                    using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
                    using var output = new MemoryStream();
                    deflateStream.CopyTo(output);
                    decompressed = output.ToArray();

                    if (decompressed.Length != entry.UncompressedSize)
                        decompressed = null;
                }
                catch { decompressed = null; }
            }

            if (decompressed == null)
                throw new InvalidDataException($"Could not decompress {entry.FileName}");

            Console.WriteLine($"Successfully extracted {entry.FileName} ({decompressed.Length} bytes)");

            var directory = Path.GetDirectoryName(entry.FileName);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Console.WriteLine($"Creating directory: {directory}");
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(entry.FileName, decompressed);
        } 
    }
}