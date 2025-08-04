// note: taken from MikuMikuLibrary (https://github.com/blueskythlikesclouds/MikuMikuLibrary/)
namespace ChicksLib.IO;

public enum StringBinaryFormat
{
    Unknown,
    NullTerminated,
    FixedLength,
    PrefixedLength8,
    PrefixedLength16,
    PrefixedLength32
}