using System;
using System.Numerics;

namespace ClearMinimap;

internal sealed class MapMotion
{
    private struct Sample
    {
        public long identity;
        public Vector2 position;
        public int frame;
        public float seconds;
    }
    private readonly Sample[] samples = new Sample[97];
    private int count;

    public Vector2 sample(long identity, Vector2 target, int frame, float seconds)
    {
        if (!float.IsFinite(target.LengthSquared()) || !float.IsFinite(seconds))
            throw new ArgumentOutOfRangeException(nameof(target));
        int index = -1;
        for (int i = 0; i < count; i++)
            if (samples[i].identity == identity) { index = i; break; }
        if (index < 0)
        {
            if (count < samples.Length) index = count++;
            else
            {
                index = 0;
                for (int i = 1; i < count; i++)
                    if (samples[i].frame < samples[index].frame) index = i;
            }
            samples[index].identity = identity;
            samples[index].position = target;
            samples[index].frame = frame;
            samples[index].seconds = seconds;
            return target;
        }
        if (samples[index].frame == frame) return samples[index].position;
        float elapsed = seconds - samples[index].seconds;
        if (elapsed <= 0 || elapsed > 0.25f || Vector2.DistanceSquared(samples[index].position, target) > 900f)
            samples[index].position = target;
        else
            samples[index].position = Vector2.Lerp(samples[index].position, target, 1f - MathF.Exp(-elapsed / 0.045f));
        samples[index].frame = frame;
        samples[index].seconds = seconds;
        return samples[index].position;
    }

    public void reset()
    {
        count = 0;
    }
}
