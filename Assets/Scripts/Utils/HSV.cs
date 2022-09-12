using UnityEngine;

public class HSV
{
    public float h, s, v, a;    // do we need alpha?

    public HSV() {}

    public HSV(Color color)
    {
        a = 1.0f;

        Color.RGBToHSV(color, out h, out s, out v);
    }

    public HSV Clone()
    {
        var hsv = new HSV();
        hsv.h = this.h;
        hsv.s = this.s;
        hsv.v = this.v;
        hsv.a = this.a;
        return hsv;
    }

    public HSV CloneAndDesat(float saturationFactor)
    {
        var hsv = new HSV();
        hsv.h = this.h;
        hsv.s = this.s * saturationFactor;
        hsv.v = this.v;
        hsv.a = this.a;
        return hsv;
    }

    public Color ToRGB()
    {
        return Color.HSVToRGB(h, s, v);
    }
}