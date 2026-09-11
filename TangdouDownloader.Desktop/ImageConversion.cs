using System.Runtime.InteropServices;

namespace TangdouDownloader.Desktop;

internal static unsafe partial class ImageConversion
{
    [LibraryImport("libwebp", EntryPoint = "WebPGetInfo")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WebPGetInfo(in byte data, nuint dataSize, out int width, out int height);

    [LibraryImport("libwebp", EntryPoint = "WebPDecodeRGBA")]
    private static partial IntPtr WebPDecodeRGBA(in byte data, nuint dataSize, out int width, out int height);

    [LibraryImport("libwebp", EntryPoint = "WebPFree")]
    private static partial void WebPFree(IntPtr pointer);

    public static bool TryDecodeWebPToBmp(ReadOnlySpan<byte> webp, out byte[] bmp)
    {
        bmp = [];
        try
        {
            if (webp.Length < 12 || webp[0] != (byte)'R' || webp[1] != (byte)'I' ||
                webp[2] != (byte)'F' || webp[3] != (byte)'F' || webp[8] != (byte)'W' ||
                webp[9] != (byte)'E' || webp[10] != (byte)'B' || webp[11] != (byte)'P')
                return false;

            if (!WebPGetInfo(in MemoryMarshal.GetReference(webp), (nuint)webp.Length, out var width, out var height) ||
                width <= 0 || height <= 0 || width > 8192 || height > 8192)
                return false;

            var rgba = WebPDecodeRGBA(
                in MemoryMarshal.GetReference(webp),
                (nuint)webp.Length,
                out _,
                out _);
            if (rgba == IntPtr.Zero)
                return false;

            try
            {
                bmp = CreateBmpFromRgba(rgba, width, height);
                return true;
            }
            finally
            {
                WebPFree(rgba);
            }
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException or TypeInitializationException)
        {
            return false;
        }
    }

    private static byte[] CreateBmpFromRgba(IntPtr rgba, int width, int height)
    {
        const int headerSize = 54;
        var stride = (width * 3 + 3) & ~3;
        var imageSize = stride * height;
        var result = new byte[headerSize + imageSize];

        result[0] = (byte)'B';
        result[1] = (byte)'M';
        WriteInt32(result, 2, result.Length);
        WriteInt32(result, 10, headerSize);
        WriteInt32(result, 14, 40);
        WriteInt32(result, 18, width);
        WriteInt32(result, 22, height);
        result[26] = 1;
        result[28] = 24;
        WriteInt32(result, 34, imageSize);
        WriteInt32(result, 38, 2835);
        WriteInt32(result, 42, 2835);

        unsafe
        {
            var source = (byte*)rgba;
            for (var y = 0; y < height; y++)
            {
                var destinationY = height - 1 - y;
                var destinationOffset = headerSize + destinationY * stride;
                var sourceOffset = y * width * 4;
                for (var x = 0; x < width; x++)
                {
                    var sourceIndex = sourceOffset + x * 4;
                    var destinationIndex = destinationOffset + x * 3;
                    result[destinationIndex] = source[sourceIndex + 2];
                    result[destinationIndex + 1] = source[sourceIndex + 1];
                    result[destinationIndex + 2] = source[sourceIndex];
                }
            }
        }

        return result;
    }

    private static void WriteInt32(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }
}
