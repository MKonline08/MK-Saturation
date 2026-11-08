namespace MkSaturation;

public interface IColorBackend
{
    float[] Read();
    void Write(float[] matrix);
}

// Never silently fight another app. The baseline must be neutral before taking ownership.
public sealed class EffectController(IColorBackend backend)
{
    private float[]? baseline, last;
    public bool OwnsEffect => last != null;
    public void Apply(int percent)
    {
        var target = ColorMatrix.Saturation(percent);
        var current = backend.Read();
        if (last != null && !ColorMatrix.Equal(current, last))
        { last = null; baseline = null; throw new InvalidOperationException("Another app changed the fullscreen color effect. Global control paused."); }
        if (baseline == null)
        {
            if (!ColorMatrix.Equal(current, ColorMatrix.Saturation(100)))
                throw new InvalidOperationException("An existing fullscreen color effect is active. Close Magnifier or other color tools, then retry.");
            baseline = current;
        }
        // Record ownership before writing so a partially successful call can be recovered.
        last = target;
        try
        {
            backend.Write(target);
            if (!ColorMatrix.Equal(backend.Read(), target)) throw new InvalidOperationException("Windows color-matrix readback did not match.");
        }
        catch (Exception applyError)
        {
            try { backend.Write(baseline); last = null; baseline = null; }
            catch (Exception restoreError) { throw new AggregateException("Apply failed and restoration also failed. Use Restore and exit, or sign out.", applyError, restoreError); }
            throw;
        }
    }
    public void CheckOwnership()
    {
        if (last == null) return;
        if (!ColorMatrix.Equal(backend.Read(), last))
        { last = null; baseline = null; throw new InvalidOperationException("Another app changed the effect. Global control paused; its effect was left intact."); }
    }
    public void Restore()
    {
        if (last == null || baseline == null) return;
        var current = backend.Read();
        if (ColorMatrix.Equal(current, last))
        {
            backend.Write(baseline);
            if (!ColorMatrix.Equal(backend.Read(), baseline)) throw new InvalidOperationException("Restoration readback failed. Retry or sign out.");
        }
        last = null; baseline = null;
    }
}
