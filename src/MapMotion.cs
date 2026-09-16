using System;
using System.Numerics;

namespace ClearMinimap;

internal sealed class MapMotion
{
    private readonly long[] identities = new long[97];
    private readonly Vector2[] positions = new Vector2[97];
    private readonly int[] frames = new int[97];
    private readonly float[] times = new float[97];
    private int count;

    public Vector2 sample(long identity, Vector2 target, int frame, float seconds)
    {
        if (!float.IsFinite(target.LengthSquared()) || !float.IsFinite(seconds))
            throw new ArgumentOutOfRangeException(nameof(target));
        int index = -1;
        for (int i = 0; i < count; i++)
            if (identities[i] == identity) { index = i; break; }
        if (index < 0)
        {
            if (count < identities.Length) index = count++;
            else
            {
                index = 0;
                for (int i = 1; i < count; i++)
                    if (frames[i] < frames[index]) index = i;
            }
            identities[index] = identity;
            positions[index] = target;
            frames[index] = frame;
            times[index] = seconds;
            return target;
        }
        if (frames[index] == frame) return positions[index];
        float elapsed = seconds - times[index];
        if (elapsed <= 0 || elapsed > 0.25f || Vector2.DistanceSquared(positions[index], target) > 900f)
            positions[index] = target;
        else
            positions[index] = Vector2.Lerp(positions[index], target, 1f - MathF.Exp(-elapsed / 0.045f));
        frames[index] = frame;
        times[index] = seconds;
        return positions[index];
    }

    public void reset()
    {
        count = 0;
    }
}
