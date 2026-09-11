namespace Aprillz.MewUI;

internal sealed class ClickCountTracker
{
    private struct ClickState
    {
        public bool HasValue;
        public uint LastTimeMs;
        public int LastX;
        public int LastY;
        public int Count;
    }

    private readonly ClickState[] _states = new ClickState[5];

    public void Reset()
    {
        for (int i = 0; i < _states.Length; i++)
        {
            _states[i] = default;
        }
    }

    public int Update(MouseButton button, int x, int y, uint timeMs, uint maxDelayMs, int maxDistX, int maxDistY)
    {
        var index = (int)button;
        if ((uint)index >= (uint)_states.Length)
        {
            return 1;
        }

        var state = _states[index];
        int count;

        if (!state.HasValue)
        {
            count = 1;
        }
        else
        {
            var dt = timeMs - state.LastTimeMs; // unsigned handles wrap-around
            var dx = x - state.LastX;
            var dy = y - state.LastY;
            if (dt <= maxDelayMs && Abs(dx) <= maxDistX && Abs(dy) <= maxDistY)
            {
                count = state.Count + 1;
            }
            else
            {
                count = 1;
            }
        }

        state.HasValue = true;
        state.LastTimeMs = timeMs;
        state.LastX = x;
        state.LastY = y;
        state.Count = count;
        _states[index] = state;

        return count;
    }

    private static int Abs(int v) => v < 0 ? -v : v;
}

