using System;
using UnityEngine;

public class BresenhamWu: MonoBehaviour
{
    public GameObject target;

    private Vector3 bStart;
    private Vector3 wStart;
    private Vector3 bEnd;
    private Vector3 wEnd;
    private Vector3 center;
    private Camera cam;
    private Ray ray;
    private RaycastHit selected;
    private Texture2D texture;
    private float h, s, v;

    void Awake()
    {
        cam = camera;
        texture = new Texture2D(512, 512);
        selected = new RaycastHit();
        ClearTexture();
        target.renderer.material.mainTexture = texture;
        target.renderer.material.mainTexture.filterMode = FilterMode.Point;
    }

    void ClearTexture()
    {
        for (var i = 0; i < texture.width; i++)
        {
            for (var j = 0; j < texture.height; j++)
            {
                texture.SetPixel(i, j, Color.white);
            }
        }
        texture.Apply();
    }

    void Update()
    {
        if (Input.GetKeyDown("escape")) Application.Quit();
        if (Input.GetKeyDown("space")) ClearTexture();
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out selected))
            {
                bStart = selected.textureCoord;
                bStart.x *= texture.width;
                bStart.y *= texture.height;
            }
        }
        if (Input.GetMouseButton(0))
        {
            if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out selected))
            {
                bEnd = selected.textureCoord;
                bEnd.x *= texture.width;
                bEnd.y *= texture.height;
            }
            BresenhamLineSwapless(Mathf.FloorToInt(bStart.x), Mathf.FloorToInt(bStart.y),
                                  Mathf.FloorToInt(bEnd.x), Mathf.FloorToInt(bEnd.y));
        }
        if (Input.GetMouseButtonDown(1))
        {
            if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out selected))
            {
                wStart = selected.textureCoord;
                wStart.x *= texture.width;
                wStart.y *= texture.height;
            }
        }
        if (Input.GetMouseButton(1))
        {
            if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out selected))
            {
                wEnd = selected.textureCoord;
                wEnd.x *= texture.width;
                wEnd.y *= texture.height;
            }
            WuLine(Mathf.FloorToInt(wStart.x), Mathf.FloorToInt(wStart.y),
                   Mathf.FloorToInt(wEnd.x), Mathf.FloorToInt(wEnd.y));
        }
        if (Input.GetMouseButton(2))
        {
            if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out selected))
            {
                center = selected.textureCoord;
                center.x *= texture.width;
                center.y *= texture.height;
            }
            BresenhamCircle(Mathf.FloorToInt(center.x), Mathf.FloorToInt(center.y), 20);
        }
    }

    public static Color ColorFromHSV(float h, float s, float v, float a = 1)
    {
        // no saturation, we can return the value across the board (grayscale)
        if (s == 0)
            return new Color(v, v, v, a);

        // which chunk of the rainbow are we in?
        float sector = h / 60;

        // split across the decimal (ie 3.87 into 3 and 0.87)
        int i = (int)sector;
        float f = sector - i;

        float p = v * (1 - s);
        float q = v * (1 - s * f);
        float t = v * (1 - s * (1 - f));

        // build our rgb color
        Color color = new Color(0, 0, 0, a);

        switch (i)
        {
            case 0:
                color.r = v;
                color.g = t;
                color.b = p;
                break;

            case 1:
                color.r = q;
                color.g = v;
                color.b = p;
                break;

            case 2:
                color.r = p;
                color.g = v;
                color.b = t;
                break;

            case 3:
                color.r = p;
                color.g = q;
                color.b = v;
                break;

            case 4:
                color.r = t;
                color.g = p;
                color.b = v;
                break;

            default:
                color.r = v;
                color.g = p;
                color.b = q;
                break;
        }

        return color;
    }

    public static void ColorToHSV(Color color, out float h, out float s, out float v)
    {
        float min = Mathf.Min(Mathf.Min(color.r, color.g), color.b);
        float max = Mathf.Max(Mathf.Max(color.r, color.g), color.b);
        float delta = max - min;

        // value is our max color
        v = max;

        // saturation is percent of max
        if (!Mathf.Approximately(max, 0))
            s = delta / max;
        else
        {
            // all colors are zero, no saturation and hue is undefined
            s = 0;
            h = -1;
            return;
        }

        // grayscale image if min and max are the same
        if (Mathf.Approximately(min, max))
        {
            v = max;
            s = 0;
            h = -1;
            return;
        }

        // hue depends which color is max (this creates a rainbow effect)
        if (color.r == max)
            h = (color.g - color.b) / delta;         	// between yellow & magenta
        else if (color.g == max)
            h = 2 + (color.b - color.r) / delta; 		// between cyan & yellow
        else
            h = 4 + (color.r - color.g) / delta; 		// between magenta & cyan

        // turn hue into 0-360 degrees
        h *= 60;
        if (h < 0)
            h += 360;
    }

    void Swap<T>(ref T lhs, ref T rhs)
    {
        var temp = lhs;
        lhs = rhs;
        rhs = temp;
    }

    void DrawRainbowPoint(int x, int y)
    {
        ColorToHSV(texture.GetPixel(x, y), out h, out s, out v);
        if (h <= 340) texture.SetPixel(x, y, ColorFromHSV(h + 20, 0.95f, 0.95f));
        else texture.SetPixel(x, y, ColorFromHSV(0, s, v));
    }

    void BresenhamLine(int x0, int y0, int x1, int y1)
    {
        var steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
        if (steep)
        {
            Swap(ref x0, ref y0);
            Swap(ref x1, ref y1);
        }
        if (x0 > x1)
        {
            Swap(ref x0, ref x1);
            Swap(ref y0, ref y1);
        }
        int dx = x1 - x0;
        int dy = Math.Abs(y1 - y0);
        int error = dx / 2;
        int ystep = (y0 < y1) ? 1 : -1;
        int y = y0;
        for (int x = x0; x <= x1; x++)
        {
            DrawRainbowPoint(steep ? y : x, steep ? x : y);
            error -= dy;
            if (error < 0)
            {
                y += ystep;
                error += dx;
            }
        }
        texture.Apply();
    }

    void BresenhamLineSwapless(int x0, int y0, int x1, int y1)
    {
        var dx = Mathf.Abs(x1 - x0);
        var dy = Mathf.Abs(y1 - y0);
        var sy = (y0 < y1) ? 1 : -1;
        var sx = (x0 < x1) ? 1 : -1;
        var err = dx - dy;
        while (true)
        {
            DrawRainbowPoint(x0, y0);
            if (x0 == x1 && y0 == y1) break;
            var e2 = 2*err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (x0 == x1 && y0 == y1)
            {
                DrawRainbowPoint(x0, y0);
                break;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
        texture.Apply();
    }

    void BresenhamCircle(int x0, int y0, int radius)
    {
        int x = radius;
        int y = 0;
        int radiusError = 1 - x;
        while (x >= y)
        {
            DrawRainbowPoint(x + x0, y + y0);
            DrawRainbowPoint(y + x0, x + y0);
            DrawRainbowPoint(-x + x0, y + y0);
            DrawRainbowPoint(-y + x0, x + y0);
            DrawRainbowPoint(-x + x0, -y + y0);
            DrawRainbowPoint(-y + x0, -x + y0);
            DrawRainbowPoint(x + x0, -y + y0);
            DrawRainbowPoint(y + x0, -x + y0);
            y++;
            if (radiusError < 0)
            {
                radiusError += 2 * y + 1;
            }
            else
            {
                x--;
                radiusError += 2 * (y - x + 1);
            }
        }
        texture.Apply();
    }

    private void WuLine(int x0, int y0, int x1, int y1)
    {
        var steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
        if (steep)
        {
            Swap(ref x0, ref y0);
            Swap(ref x1, ref y1);
        }
        if (x0 > x1)
        {
            Swap(ref x0, ref x1);
            Swap(ref y0, ref y1);
        }

        DrawDarkPoint(steep, x0, y0, 1);
        DrawDarkPoint(steep, x1, y1, 1);
        float dx = x1 - x0;
        float dy = y1 - y0;
        float gradient = dy / dx;
        float y = y0 + gradient;
        for (var x = x0 + 1; x <= x1 - 1; x++)
        {
            DrawDarkPoint(steep, x, (int)y, 1 - (y - (int)y));
            DrawDarkPoint(steep, x, (int)y + 1, y - (int)y);
            y += gradient;
        }
        texture.Apply();
    }

    void DrawDarkPoint(bool steep, int x, int y, float c)
    {
        if (!steep)
        {
            var color = texture.GetPixel(x, y);
            color.r *= (1 - c);
            color.g *= (1 - c);
            color.b *= (1 - c);
            texture.SetPixel(x, y, color);
        }
        else
        {
            var color = texture.GetPixel(y, x);
            color.r *= (1 - c);
            color.g *= (1 - c);
            color.b *= (1 - c);
            texture.SetPixel(y, x, color);
        }
    }
}
