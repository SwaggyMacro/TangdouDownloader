namespace Aprillz.MewUI.Animation;

/// <summary>
/// A single keyframe with a time, value, and optional per-segment easing.
/// </summary>
public readonly struct Keyframe<T>
{
    /// <summary>
    /// The time of this keyframe in milliseconds (absolute).
    /// </summary>
    public required double Time { get; init; }

    /// <summary>
    /// The value at this keyframe.
    /// </summary>
    public required T Value { get; init; }

    /// <summary>
    /// Easing function applied from this keyframe to the next.
    /// <c>null</c> means linear interpolation.
    /// </summary>
    public Func<double, double>? Easing { get; init; }
}

/// <summary>
/// Evaluates interpolated values across a sorted sequence of keyframes.
/// Each segment between keyframes can have its own easing function.
/// </summary>
public sealed class KeyframeTrack<T>
{
    private readonly Keyframe<T>[] _keyframes;
    private readonly LerpFunc<T> _lerp;

    public KeyframeTrack(LerpFunc<T> lerp, params Keyframe<T>[] keyframes)
    {
        ArgumentNullException.ThrowIfNull(lerp);

        if (keyframes.Length < 2)
        {
            throw new ArgumentException("At least 2 keyframes are required.", nameof(keyframes));
        }

        _lerp = lerp;
        _keyframes = new Keyframe<T>[keyframes.Length];
        Array.Copy(keyframes, _keyframes, keyframes.Length);
        Array.Sort(_keyframes, (a, b) => a.Time.CompareTo(b.Time));

        TotalDuration = _keyframes[^1].Time;
    }

    /// <summary>
    /// The time of the last keyframe (ms).
    /// </summary>
    public double TotalDuration { get; }

    /// <summary>
    /// Evaluates the interpolated value at the given time (ms).
    /// </summary>
    public T Evaluate(double timeMs)
    {
        var kf = _keyframes;

        // Before first keyframe
        if (timeMs <= kf[0].Time)
        {
            return kf[0].Value;
        }

        // After last keyframe
        if (timeMs >= kf[^1].Time)
        {
            return kf[^1].Value;
        }

        // Find segment: binary search for the last keyframe with Time <= timeMs
        int lo = 0, hi = kf.Length - 2;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) >> 1;
            if (kf[mid].Time <= timeMs)
                lo = mid;
            else
                hi = mid - 1;
        }

        ref readonly var from = ref kf[lo];
        ref readonly var to = ref kf[lo + 1];

        double segmentDuration = to.Time - from.Time;
        if (segmentDuration <= 0)
        {
            return to.Value;
        }

        double localT = (timeMs - from.Time) / segmentDuration;
        double easedT = from.Easing != null ? from.Easing(localT) : localT;

        return _lerp(from.Value, to.Value, easedT);
    }
}

/// <summary>
/// A keyframe track that snaps to the most recent keyframe value without interpolation.
/// Used for discrete transitions (e.g. on/off opacity).
/// </summary>
public sealed class DiscreteKeyframeTrack<T>
{
    private readonly (double Time, T Value)[] _keyframes;

    public DiscreteKeyframeTrack(params (double Time, T Value)[] keyframes)
    {
        if (keyframes.Length < 1)
        {
            throw new ArgumentException("At least 1 keyframe is required.", nameof(keyframes));
        }

        _keyframes = new (double, T)[keyframes.Length];
        Array.Copy(keyframes, _keyframes, keyframes.Length);
        Array.Sort(_keyframes, (a, b) => a.Time.CompareTo(b.Time));

        TotalDuration = _keyframes[^1].Time;
    }

    /// <summary>
    /// The time of the last keyframe (ms).
    /// </summary>
    public double TotalDuration { get; }

    /// <summary>
    /// Returns the value of the most recent keyframe at or before the given time.
    /// </summary>
    public T Evaluate(double timeMs)
    {
        var kf = _keyframes;

        if (timeMs <= kf[0].Time)
        {
            return kf[0].Value;
        }

        if (timeMs >= kf[^1].Time)
        {
            return kf[^1].Value;
        }

        // Binary search for the last keyframe with Time <= timeMs
        int lo = 0, hi = kf.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) >> 1;
            if (kf[mid].Time <= timeMs)
                lo = mid;
            else
                hi = mid - 1;
        }

        return kf[lo].Value;
    }
}
