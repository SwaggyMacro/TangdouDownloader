using Aprillz.MewUI.Controls;

namespace Aprillz.MewUI.Animation;

/// <summary>
/// Manages animated transitions for <see cref="PropertyValueStore"/> entries.
/// Owns <see cref="AnimationClock"/> instances and interpolates via <see cref="TypeLerp"/>.
/// This keeps animation concerns out of the core property store.
/// </summary>
internal sealed class PropertyAnimator
{
    private readonly Element _owner;
    private readonly PropertyValueStore _store;
    private Dictionary<int, AnimState>? _states;

    internal PropertyAnimator(Element owner, PropertyValueStore store)
    {
        _owner = owner;
        _store = store;
        store.StopAnimationCallback = StopAnimation;
        store.StopAllAnimationsCallback = StopAll;
    }

    /// <summary>
    /// Animates the overlay from an explicit <paramref name="from"/> to <paramref name="to"/> where
    /// the store's base value is already <paramref name="to"/> (the caller revealed it, e.g. by
    /// clearing a higher source). Only the overlay animates; the base is not re-set, and on
    /// completion the overlay clears back to the revealed base.
    /// </summary>
    internal void AnimateFromTo(MewProperty property, object from, object to, TimeSpan duration, Func<double, double> easing)
    {
        int id = property.Id;

        if (Equals(from, to) || !TypeLerp.CanLerp(property.ValueType))
        {
            // Nothing to animate: drop any overlay so the revealed base shows immediately.
            if (_states != null && _states.TryGetValue(id, out var existing))
            {
                existing.Clock?.Stop();
                _states.Remove(id);
            }
            _store.ClearAnimatedValue(id);
            return;
        }

        _states ??= new();
        if (!_states.TryGetValue(id, out var state))
        {
            state = new AnimState();
            _states[id] = state;
            state.Clock = new AnimationClock(duration, easing).AttachTo(_owner);
            state.Clock.TickCallback = progress => OnTick(id, progress);
            state.Clock.CompletedCallback = () => _states?.Remove(id);
        }
        else
        {
            state.Clock!.Stop();
            state.Clock.Duration = duration;
            state.Clock.EasingFunction = easing;
        }

        state.FromValue = from;
        state.TargetValue = to;
        state.PropertyType = property.ValueType;
        state.LerpDelegate = TypeLerp.GetDelegate(state.PropertyType);

        // Base is already `to` (revealed by the caller); only overlay the animation.
        _store.SetAnimatedValue(id, from);
        state.Clock.Start();
    }

    /// <summary>
    /// Stops all running animations and clears animated overlays.
    /// </summary>
    public void StopAll()
    {
        if (_states == null) return;

        foreach (var kv in _states)
        {
            kv.Value.Clock?.Stop();
            _store.ClearAnimatedValue(kv.Key);
        }
        _states.Clear();
    }

    private void StopAnimation(int propertyId)
    {
        if (_states == null || !_states.TryGetValue(propertyId, out var state))
            return;

        state.Clock?.Stop();
        // Animated value clearing is handled by the PropertyValueStore caller.
        _states.Remove(propertyId);
    }

    private void OnTick(int propertyId, double progress)
    {
        if (_states == null || !_states.TryGetValue(propertyId, out var state))
            return;

        if (state.FromValue == null || state.TargetValue == null || state.PropertyType == null)
            return;

        var interpolated = Interpolate(state, progress);

        if (progress >= 1.0)
        {
            // Animation complete - clear animated overlay, target value takes effect
            _store.ClearAnimatedValue(propertyId);
        }
        else
        {
            _store.SetAnimatedValue(propertyId, interpolated);
        }
    }

    /// <summary>
    /// Interpolates the animation's current value. The common visual types are computed
    /// directly (no dictionary lookup, no delegate call); anything else registered via
    /// <see cref="TypeLerp.Register{T}"/> falls back to the delegate cached at animation start.
    /// The result still has to be boxed once here since <see cref="PropertyValueStore"/> stores
    /// the animated overlay as <c>object</c>.
    /// </summary>
    private static object Interpolate(AnimState state, double progress)
    {
        var propertyType = state.PropertyType!;
        var from = state.FromValue!;
        var to = state.TargetValue!;

        if (propertyType == typeof(double))
            return Lerp.Double((double)from, (double)to, progress);
        if (propertyType == typeof(Color))
            return Lerp.Color((Color)from, (Color)to, progress);
        if (propertyType == typeof(Thickness))
            return Lerp.Thickness((Thickness)from, (Thickness)to, progress);
        if (propertyType == typeof(Point))
            return Lerp.Point((Point)from, (Point)to, progress);
        if (propertyType == typeof(float))
            return Lerp.Float((float)from, (float)to, progress);

        return state.LerpDelegate!(from, to, progress);
    }

    private sealed class AnimState
    {
        public AnimationClock? Clock;
        public object? FromValue;
        public object? TargetValue;
        public Type? PropertyType;
        public Func<object, object, double, object>? LerpDelegate;
    }
}
