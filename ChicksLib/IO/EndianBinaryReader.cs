using System.Buffers.Binary;
using System.Text;

namespace ChicksLib.IO
{
    public class EndianBinaryReader : BinaryReader
    {
        private StringBuilder mStringBuilder;
        private Endianness mEndianness;
        private Encoding mEncoding;
        private Stack<long> mOffsets;
        private Stack<long> mBaseOffsets;

        public Endianness Endianness
        {
            get => mEndianness;
            set
            {
                SwapBytes = value != (BitConverter.IsLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian);
                mEndianness = value;
            }
        }

        public bool SwapBytes { get; private set; }

        public Encoding Encoding
        {
            get => mEncoding;
            set => mEncoding = value;
        }

        public long Position
        {
            get => BaseStream.Position;
            set => BaseStream.Position = value;
        }

        // overrides
        public override short ReadInt16() =>
            SwapBytes ? BinaryPrimitives.ReverseEndianness(base.ReadInt16()) : base.ReadInt16();

        public override ushort ReadUInt16() =>
            SwapBytes ? BinaryPrimitives.ReverseEndianness(base.ReadUInt16()) : base.ReadUInt16();

        public override int ReadInt32() =>
            SwapBytes ? BinaryPrimitives.ReverseEndianness(base.ReadInt32()) : base.ReadInt32();

        public override uint ReadUInt32() =>
            SwapBytes ? BinaryPrimitives.ReverseEndianness(base.ReadUInt32()) : base.ReadUInt32();

        public override long ReadInt64() =>
            SwapBytes ? BinaryPrimitives.ReverseEndianness(base.ReadInt64()) : base.ReadInt64();

        public string ReadString(StringBinaryFormat format, int FixedLength = -1)
        {
            mStringBuilder.Clear();

            switch (format)
            {
                case StringBinaryFormat.NullTerminated:
                    {
                        char b;

                        while ((b = ReadChar()) != 0)
                            mStringBuilder.Append(b);

                        break;
                    }

                case StringBinaryFormat.FixedLength:
                    {
                        if (FixedLength == -1)
                            throw new ArgumentException("invalid fixed length specified");

                        var bytes = ReadBytes(FixedLength);

                        int index = Array.IndexOf(bytes, (byte)0);
                        return index == -1 ? mEncoding.GetString(bytes) : mEncoding.GetString(bytes, 0, index);
                    }

                case StringBinaryFormat.PrefixedLength8:
                    {
                        byte length = ReadByte();

                        for (int i = 0; i < length; i++)
                            mStringBuilder.Append(ReadChar());

                        break;
                    }

                case StringBinaryFormat.PrefixedLength16:
                    {
                        ushort length = ReadUInt16();

                        for (int i = 0; i < length; i++)
                            mStringBuilder.Append(ReadChar());

                        break;
                    }

                case StringBinaryFormat.PrefixedLength32:
                    {
                        uint length = ReadUInt32();

                        for (int i = 0; i < length; i++)
                            mStringBuilder.Append(ReadChar());

                        break;
                    }

                default:
                    throw new ArgumentException("unknown string format: ", nameof(format));
            }

            return mStringBuilder.ToString();
        }

        public string PeekString(StringBinaryFormat format, int FixedLength = -1)
        {
            string result;

            long current = Position;
            {
                result = ReadString(format, FixedLength);
            }
            SeekBegin(current);

            return result;
        }

        public override float ReadSingle() =>
            BitConverter.Int32BitsToSingle(ReadInt32());

        public override double ReadDouble() =>
            BitConverter.Int64BitsToDouble(ReadInt64());

        public override Half ReadHalf() =>
            BitConverter.Int16BitsToHalf(ReadInt16());

        // read multiples
        public short[] ReadInt16s(int count)
        {
            var array = new short[count];

            for (int i = 0; i < array.Length; i++)
                array[i] = ReadInt16();

            return array;
        }

        public ushort[] ReadUInt16s(int count)
        {
            var array = new ushort[count];

            for (int i = 0; i < array.Length; i++)
                array[i] = ReadUInt16();

            return array;
        }

        public int[] ReadInt32s(int count)
        {
            var array = new int[count];

            for (int i = 0; i < array.Length; i++)
                array[i] = ReadInt32();

            return array;
        }

        public uint[] ReadUInt32s(int count)
        {
            var array = new uint[count];

            for (int i = 0; i < array.Length; i++)
                array[i] = ReadUInt32();

            return array;
        }

        public long[] ReadInt64s(int count)
        {
            var array = new long[count];

            for (int i = 0; i < array.Length; i++)
                array[i] = ReadUInt32();

            return array;
        }

        public ulong[] ReadUInt64s(int count)
        {
            var array = new ulong[count];

            for (int i = 0; i < array.Length; i++)
                array[i] = ReadUInt64();

            return array;
        }

        public float[] ReadSingles(int count)
        {
            var array = new float[count];

            for (int i = 0; i < array.Length; i++)
                array[i] = ReadSingle();

            return array;
        }

        public double[] ReadDoubles(int count)
        {
            var array = new double[count];

            for (int i = 0; i < array.Length; i++)
                array[i] = ReadDouble();

            return array;
        }

        public Half[] ReadHalves(int count)
        {
            var array = new Half[count];

            for (int i = 0; i < array.Length; i++)
                array[i] = ReadHalf();

            return array;
        }
        // note: consider adding a DEBUG build variant of this like MikuMikuLibrary
        public void SkipNulls(int length)
        {
            SeekCurrent(length);
        }

        public void Seek(long offset, SeekOrigin origin) =>
            BaseStream.Seek(offset, origin);

        public void SeekBegin(long offset) =>
            BaseStream.Seek(offset, SeekOrigin.Begin);

        public void SeekCurrent(long offset) =>
            BaseStream.Seek(offset, SeekOrigin.Current);

        public void SeekEnd(long offset) =>
            BaseStream.Seek(offset, SeekOrigin.End);

        public void PushOffset(long offset) =>
            mOffsets.Push(offset);

        public void PushOffset() =>
            mOffsets.Push(Position);

        public long PeekOffset() =>
            mOffsets.Peek();

        public long PopOffset() =>
            mOffsets.Pop();

        // constructors
        private void Init(Encoding encoding, Endianness endianness)
        {
            mEncoding = encoding;
            Endianness = endianness;
            mOffsets = new Stack<long>();
            mBaseOffsets = new Stack<long>();
            mStringBuilder = new StringBuilder();
        }

        public EndianBinaryReader(Stream input, Endianness endianness)
            : base(input)
        {
            Init(Encoding.Default, endianness);
        }

        public EndianBinaryReader(Stream input, Endianness endianness, Encoding encoding)
            : base(input, encoding)
        {
            Init(encoding, endianness);
        }
    }
}
