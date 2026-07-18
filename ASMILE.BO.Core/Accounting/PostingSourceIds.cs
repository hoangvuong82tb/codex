namespace ASMILE.BO.Core.Accounting;

public static class PostingSourceIds
{
    public static Guid FromInt32(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Source id must be greater than zero.");
        }

        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes[..4], id);
        return new Guid(bytes);
    }

    public static bool TryToInt32(Guid sourceId, out int id)
    {
        var bytes = sourceId.ToByteArray();
        id = BitConverter.ToInt32(bytes, 0);
        for (var i = 4; i < bytes.Length; i++)
        {
            if (bytes[i] != 0)
            {
                id = 0;
                return false;
            }
        }

        return id > 0;
    }
}
