using System.Buffers.Binary;
using System.Security.Cryptography;

namespace ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

public interface IStronglyTypedGuid<TSelf> where TSelf : struct, IStronglyTypedGuid<TSelf>
{
    Guid Value { get; }
    static abstract TSelf From(Guid value);
}

public static class Uuid7
{
    public static Guid NewGuid()
    {
        Span<byte> bytes = stackalloc byte[16];
        var milliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        bytes[0] = (byte)(milliseconds >> 40);
        bytes[1] = (byte)(milliseconds >> 32);
        bytes[2] = (byte)(milliseconds >> 24);
        bytes[3] = (byte)(milliseconds >> 16);
        bytes[4] = (byte)(milliseconds >> 8);
        bytes[5] = (byte)milliseconds;
        RandomNumberGenerator.Fill(bytes[6..]);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes, bigEndian: true);
    }
}
