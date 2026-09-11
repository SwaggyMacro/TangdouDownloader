namespace BitMiracle.LibJpeg.Classic.Internal;

/// <summary>
/// Boxing-free entropy decoder dispatch.
/// </summary>
internal struct jpeg_entropy_decoder_union
{
    private enum Kind : byte
    {
        None = 0,
        Huffman = 1,
    }

    private Kind m_kind;
    private huff_entropy_decoder m_huff;

    public static jpeg_entropy_decoder_union CreateHuffman(jpeg_decompress_struct cinfo)
        => new jpeg_entropy_decoder_union
        {
            m_kind = Kind.Huffman,
            m_huff = new huff_entropy_decoder(cinfo),
        };

    public void start_pass()
    {
        switch (m_kind)
        {
            case Kind.Huffman:
                m_huff.start_pass();
                return;
            default:
                return;
        }
    }

    public bool decode_mcu(JBLOCK[] mcuData)
        => m_kind switch
        {
            Kind.Huffman => m_huff.decode_mcu(mcuData),
            _ => false,
        };

    public void finish_pass()
    {
        switch (m_kind)
        {
            case Kind.Huffman:
                m_huff.finish_pass();
                return;
            default:
                return;
        }
    }
}
