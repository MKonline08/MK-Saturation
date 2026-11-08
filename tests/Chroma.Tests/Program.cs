using MkSaturation;
int passed = 0;
void Test(string name, Action action) { action(); Console.WriteLine("PASS " + name); passed++; }
void Assert(bool ok) { if (!ok) throw new Exception("Assertion failed"); }
void Throws(Action action) { try { action(); } catch { return; } throw new Exception("Expected exception"); }
Test("100% identity / alpha / homogeneous component", () => {
    var m = ColorMatrix.Saturation(100);
    for (int r = 0; r < 5; r++) for (int c = 0; c < 5; c++) Assert(m[r * 5 + c] == (r == c ? 1 : 0));
});
Test("0% red becomes Rec.709 gray with correct matrix orientation", () => {
    var p = ColorMatrix.Preview(0, 255, 0, 0); Assert(p.R == 54 && p.G == 54 && p.B == 54);
});
Test("gray preserved at every slider value", () => {
    for (int s = 0; s <= 200; s++) { var p = ColorMatrix.Preview(s, 127, 127, 127); Assert(p == ((byte)127, (byte)127, (byte)127)); }
});
Test("200% increases channel separation / clips safely", () => {
    var p = ColorMatrix.Preview(200, 160, 100, 80); Assert(p.R > 160 && p.B < 80);
    Assert(ColorMatrix.Preview(200, 255, 0, 0) == ((byte)255, (byte)0, (byte)0));
});
Test("reject out-of-range and nonfinite matrices", () => {
    Throws(() => ColorMatrix.Saturation(-1)); Throws(() => ColorMatrix.Saturation(201));
    var m = ColorMatrix.Saturation(100); m[0] = float.NaN; Assert(!ColorMatrix.Equal(m, m));
});
Test("apply, readback, restore baseline", () => {
    var b = new Fake(); var c = new EffectController(b); c.Apply(140); Assert(c.OwnsEffect); Assert(ColorMatrix.Equal(b.Value, ColorMatrix.Saturation(140)));
    c.Restore(); Assert(!c.OwnsEffect); Assert(ColorMatrix.Equal(b.Value, ColorMatrix.Saturation(100)));
});
Test("refuse existing competing effect", () => {
    var b = new Fake { Value = ColorMatrix.Saturation(50) }; var c = new EffectController(b);
    Throws(() => c.Apply(150)); Assert(b.Writes == 0);
});
Test("do not overwrite an external change on exit or next update", () => {
    var b = new Fake(); var c = new EffectController(b); c.Apply(130); b.Value = ColorMatrix.Saturation(70); c.Restore(); Assert(b.Writes == 1);
    b.Value = ColorMatrix.Saturation(100); c.Apply(120); b.Value = ColorMatrix.Saturation(80); Throws(() => c.Apply(160)); Assert(b.Writes == 2); Assert(!c.OwnsEffect);
});
Test("poll detects lost ownership", () => {
    var b = new Fake(); var c = new EffectController(b); c.Apply(150); b.Value = ColorMatrix.Saturation(60);
    Throws(c.CheckOwnership); Assert(!c.OwnsEffect); Assert(b.Writes == 1);
});
Test("failed write rolls back baseline", () => {
    var b = new Fake { FailNextWrite = true }; var c = new EffectController(b); Throws(() => c.Apply(170));
    Assert(!c.OwnsEffect); Assert(ColorMatrix.Equal(b.Value, ColorMatrix.Saturation(100)));
});
Test("readback mismatch rolls back", () => {
    var b = new Fake { IgnoreNextWrite = true }; var c = new EffectController(b); Throws(() => c.Apply(170)); Assert(!c.OwnsEffect);
});
Test("failed restoration preserves ownership for retry", () => {
    var b = new Fake(); var c = new EffectController(b); c.Apply(150); b.FailNextWrite = true;
    Throws(c.Restore); Assert(c.OwnsEffect); c.Restore(); Assert(!c.OwnsEffect);
});
Test("settings round-trip, clamp and corrupt fallback", () => {
    string folder = Path.Combine(Path.GetTempPath(), "mk-tests-" + Guid.NewGuid()); string path = Path.Combine(folder, "settings.json");
    try {
        var store = new SettingsStore(path); Assert(store.Load() == new Settings()); store.Save(new Settings(250, true, true));
        Assert(store.Load() == new Settings(200, true, true)); File.WriteAllText(path, "broken"); Assert(store.Load() == new Settings()); Assert(store.Warning != null);
    } finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
});
Console.WriteLine($"{passed} tests passed.");
sealed class Fake : IColorBackend
{
    public float[] Value = ColorMatrix.Saturation(100);
    public int Writes; public bool FailNextWrite, IgnoreNextWrite;
    public float[] Read() => (float[])Value.Clone();
    public void Write(float[] matrix)
    {
        Writes++;
        if (FailNextWrite) { FailNextWrite = false; throw new IOException("Fake write failure"); }
        if (IgnoreNextWrite) { IgnoreNextWrite = false; return; }
        Value = (float[])matrix.Clone();
    }
}
