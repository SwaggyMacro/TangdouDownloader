namespace BitMiracle.LibJpeg.Classic.Internal;

/// <summary>
/// Boxing-free upsampler dispatch.
/// </summary>
internal struct jpeg_upsampler_union
{
    private enum Kind : byte
    {
        None = 0,
        MyUpsampler = 1,
        MyMergedUpsampler = 2,
    }

    private Kind m_kind;
    private my_upsampler m_myUpsampler;
    private my_merged_upsampler m_myMergedUpsampler;

    public static jpeg_upsampler_union CreateMyUpsampler(jpeg_decompress_struct cinfo)
        => new jpeg_upsampler_union
        {
            m_kind = Kind.MyUpsampler,
            m_myUpsampler = new my_upsampler(cinfo),
        };

    public static jpeg_upsampler_union CreateMyMergedUpsampler(jpeg_decompress_struct cinfo)
        => new jpeg_upsampler_union
        {
            m_kind = Kind.MyMergedUpsampler,
            m_myMergedUpsampler = new my_merged_upsampler(cinfo),
        };

    public bool NeedContextRows()
        => m_kind switch
        {
            Kind.MyUpsampler => m_myUpsampler.NeedContextRows(),
            Kind.MyMergedUpsampler => m_myMergedUpsampler.NeedContextRows(),
            _ => false,
        };

    public void start_pass()
    {
        switch (m_kind)
        {
            case Kind.MyUpsampler:
                m_myUpsampler.start_pass();
                return;
            case Kind.MyMergedUpsampler:
                m_myMergedUpsampler.start_pass();
                return;
            default:
                return;
        }
    }

    public void upsample(
        ComponentBuffer[] input_buf,
        ref int in_row_group_ctr,
        int in_row_groups_avail,
        byte[][] output_buf,
        ref int out_row_ctr,
        int out_rows_avail)
    {
        switch (m_kind)
        {
            case Kind.MyUpsampler:
                m_myUpsampler.upsample(input_buf, ref in_row_group_ctr, in_row_groups_avail, output_buf, ref out_row_ctr, out_rows_avail);
                return;
            case Kind.MyMergedUpsampler:
                m_myMergedUpsampler.upsample(input_buf, ref in_row_group_ctr, in_row_groups_avail, output_buf, ref out_row_ctr, out_rows_avail);
                return;
            default:
                return;
        }
    }
}

