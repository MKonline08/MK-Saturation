namespace MkSaturation;

public static class ColorMatrix
{
    // MAGCOLOREFFECT uses row-vector multiplication: input channels are rows.
    public static float[] Saturation(int percent)
    {
        if (percent is < 0 or > 200) throw new ArgumentOutOfRangeException(nameof(percent));
        float s = percent / 100f, t = 1 - s;
        float[] w = [.2126f, .7152f, .0722f];
        var m = new float[25];
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++) m[row * 5 + col] = w[row] * t + (row == col ? s : 0);
        m[18] = m[24] = 1;
        return m;
    }
    public static bool Equal(float[] a, float[] b) => a.Length == 25 && b.Length == 25 &&
        a.Zip(b).All(x => float.IsFinite(x.First) && float.IsFinite(x.Second) && Math.Abs(x.First - x.Second) < .0001f);
    public static (byte R, byte G, byte B) Preview(int percent, byte r, byte g, byte b)
    {
        var m = Saturation(percent);
        byte Channel(int c) => (byte)Math.Clamp((int)Math.Round(r * m[c] + g * m[5 + c] + b * m[10 + c]), 0, 255);
        return (Channel(0), Channel(1), Channel(2));
    }
}
