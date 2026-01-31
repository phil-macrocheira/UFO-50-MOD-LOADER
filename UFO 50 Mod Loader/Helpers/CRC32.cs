public static class CRC32
{
    private static readonly uint[] Table = GenerateTable();
    public static uint Compute(Stream stream)
    {
        uint crc = 0xFFFFFFFF;
        byte[] buffer = new byte[128 * 1024];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0) {
            for (int i = 0; i < read; i++)
                crc = (crc >> 8) ^ Table[(crc & 0xFF) ^ buffer[i]];
        }
        return crc ^ 0xFFFFFFFF;
    }

    private static uint[] GenerateTable()
    {
        const uint polynomial = 0xEDB88320;
        var table = new uint[256];
        for (uint i = 0; i < 256; i++) {
            uint c = i;
            for (int j = 0; j < 8; j++) {
                c = (c & 1) != 0 ? (polynomial ^ (c >> 1)) : (c >> 1);
            }
            table[i] = c;
        }
        return table;
    }
}