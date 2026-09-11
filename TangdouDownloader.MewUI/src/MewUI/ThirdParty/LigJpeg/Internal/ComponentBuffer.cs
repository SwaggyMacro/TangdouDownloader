using System.Runtime.CompilerServices;

namespace BitMiracle.LibJpeg.Classic.Internal;

/// <summary>
/// Encapsulates buffer of image samples for one color component.
/// Supports both contiguous Buffer2D and legacy jagged array modes.
/// When provided with funny indices (see jpeg_d_main_controller for
/// explanation of what it is) uses them for non-linear row access.
/// </summary>
struct ComponentBuffer
{
    private Buffer2D<byte> m_buffer2D;
    private byte[][]? m_legacyBuffer;

    // array of funny indices
    private int[]? m_funnyIndices;

    // index of "first funny index" (used because some code uses negative
    // indices when retrieve rows)
    // see for example my_upsampler.h2v2_fancy_upsample
    private int m_funnyOffset;

    public readonly Span<byte> this[int i]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            int actualRow = m_funnyIndices != null
                ? m_funnyIndices[i + m_funnyOffset]
                : i;

            if (!m_buffer2D.IsEmpty)
            {
                return m_buffer2D.GetRow(actualRow);
            }

            return m_legacyBuffer![actualRow].AsSpan();
        }
    }

    /// <summary>
    /// Gets row as byte[] for legacy compatibility. Prefer Span indexer when possible.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte[] GetRowArray(int i)
    {
        int actualRow = m_funnyIndices != null
            ? m_funnyIndices[i + m_funnyOffset]
            : i;

        // For legacy buffer, return directly
        if (m_legacyBuffer != null)
        {
            return m_legacyBuffer[actualRow];
        }

        // For Buffer2D, we need to return the row data
        // This creates a new array - avoid in hot paths
        return m_buffer2D.GetRow(actualRow).ToArray();
    }

    public void SetBuffer(Buffer2D<byte> buf, int[]? funnyIndices = null, int funnyOffset = 0)
    {
        m_buffer2D = buf;
        m_legacyBuffer = null;
        m_funnyIndices = funnyIndices;
        m_funnyOffset = funnyOffset;
    }

    public void SetBuffer(byte[][] buf, int[]? funnyIndices = null, int funnyOffset = 0)
    {
        m_legacyBuffer = buf;
        m_buffer2D = default;
        m_funnyIndices = funnyIndices;
        m_funnyOffset = funnyOffset;
    }

    public readonly bool IsBuffer2D => !m_buffer2D.IsEmpty;
}
