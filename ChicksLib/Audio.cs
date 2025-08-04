// note: this class is based on vgmstream's Wwise implementation.
using System.IO;
using System.Text;
using System.Text.Encodings;
using ChicksLib.IO;

namespace ChicksLib.Audio;

public class Wwise
{
    private static Endianness mEndianness;
    private ulong FileSize;
    private int Prefetch;

    private long FmtOffset;
    private ulong FmtSize;
    private long DataOffset;
    private ulong DataSize;
    private long SeekOffset;
    private ulong SeekSize;
    private long MetaOffset;
    private ulong MetaSize;

    private int Format;
    private int Channels;
    private int SampleRate;
    private int BitsPerSample;
    private int LoopFlag;
    private int LoopStart;
    private int LoopEnd;

    public static void Read(FileStream fs)
    {
        byte[] signatureBytes = new byte[4];
        fs.Read(signatureBytes, 0, 4);
        string signature = ASCIIEncoding.ASCII.GetString(signatureBytes);

        if (signature == "RIFX")
        {
            mEndianness = Endianness.BigEndian;
        }
        else if (signature == "RIFF")
        {
            mEndianness = Endianness.LittleEndian;
        }
        else
        {
            throw new InvalidDataException("invalid signature (RIFF or RIFX expected)");
        }

        var reader = new EndianBinaryReader(fs, mEndianness);

        /// i have no motivation to finish this at the moment and i am losing my mind, sorry :sob:
        /// needs the actual decoding blob to function.
    }
}
