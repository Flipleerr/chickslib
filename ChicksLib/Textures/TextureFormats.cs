// note: currently only supports Xbox flavored DDS
using System.Drawing;

namespace ChicksLib.Textures;

public struct DDSHeader
{
    public uint size;
    public uint flags;
    public uint width;
    public uint height;
    public uint pitchOrLinearSize;
    public uint depth;
    public uint mipMapCount;
    public uint[] reserved;
    
}
