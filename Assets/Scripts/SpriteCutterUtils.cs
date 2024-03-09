using System.Collections.Generic;
using UnityEngine;

namespace WinterCrestal.SpriteCutter
{
    internal struct CutInfo
    {
        public Vector2Int p0;
        public Vector2Int p1;
        public int xMin, xMax;
        public int yMin, yMax;
        public bool isCorner;
        public CutInfo(Vector2Int _p0, Vector2Int _p1, int texWidth, int texHeight)
        {
            p0 = _p0;
            p1 = _p1;
            if (p0.x < p1.x) { xMin = p0.x; xMax = p1.x; }
            else { xMin = p1.x; xMax = p0.x; }
            if (p0.y < p1.y) { yMin = p0.y; yMax = p1.y; }
            else { yMin = p1.y; yMax = p0.y; }

            isCorner = false;
            if (xMin == 0 && xMax < texWidth && (yMin > 0 || yMax < texHeight))
                isCorner = true;
            else if (xMax == texWidth && xMin > 0 && (yMin > 0 || yMax < texHeight))
                isCorner = true;
        }

        public override readonly string ToString()
        {
            return base.ToString() + "\nMin: (" + xMin + ", " + yMin + ")\nMax: (" + xMax + ", " + yMax + ")\nCorner: " + isCorner;
        }
    }

    public static class ColorExtensions
    {
        public static Color32 SetFade(this Color32 c, byte fade)
        {
            c.a = fade;
            return c;
        }
    }


    public static class SpriteCutterUtils
    {

        #region PUBLIC_FUNCTIONS
        public static Vector2Int WorldToSpriteLocal(this SpriteRenderer renderer, Vector2 worldPoint)
        {
            Vector2 scale = renderer.transform.localScale;
            float spriteLocalX = (worldPoint.x - renderer.transform.position.x) / scale.x;
            float spriteLocalY = (worldPoint.y - renderer.transform.position.y) / scale.y;
            Vector2 localPos = new(spriteLocalX, spriteLocalY);

            Quaternion spriteRotation = renderer.transform.rotation;
            localPos = Quaternion.Inverse(spriteRotation) * localPos;
            localPos += new Vector2(.5f, .5f);

            return new Vector2Int((int)(localPos.x * renderer.sprite.rect.width), (int)(localPos.y * renderer.sprite.rect.height));
        }

        public static Vector2 SpriteLocalToWorld(this SpriteRenderer renderer, Vector2 localPoint)
        {
            Vector2 spriteSize = new(renderer.sprite.rect.width, renderer.sprite.rect.height);
            Vector2 p = localPoint / spriteSize;

            p -= new Vector2(.5f, .5f);
            p = renderer.transform.rotation * p;

            p.x *=renderer.transform.localScale.x;
            p.y *=renderer.transform.localScale.y;
            p.x += renderer.transform.position.x;
            p.y += renderer.transform.position.y;
            return p;
        }

        public static uint IntersectLine(this SpriteRenderer renderer, Vector2 p0, Vector2 p1, out Vector2 hitPoint0, out Vector2 hitPoint1)
        {
            hitPoint0 = Vector2.zero;
            hitPoint1 = Vector2.zero;
            var bounds = renderer.localBounds;
            var offset = renderer.transform.position;
            var rotation = renderer.transform.rotation;

            Vector3[] corners = new Vector3[4];
            corners[0] = new(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y);
            corners[1] = new(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y);
            corners[2] = new(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y);
            corners[3] = new(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y);

            for (int i = 0; i < 4; i++) corners[i] = bounds.center + rotation * (corners[i] - bounds.center) + offset;

            for (int i = 0; i < 4; i++)
            {
                if (GetLineLineIntersection(p0, p1, corners[i], corners[(i + 1) % 4], out hitPoint0))
                {
                    for (int j = i + 1; j < 4; j++)
                        if (GetLineLineIntersection(p0, p1, corners[j], corners[(j + 1) % 4], out hitPoint1))
                            return 2;
                    return 1;
                }
            }
            return 0;
        }

        public static bool CutSprite(this SpriteRenderer spriteRenderer, Vector2Int point0, Vector2Int point1, out SpriteRenderer s0, out SpriteRenderer s1)
        {
            var texture = spriteRenderer.sprite.texture;
            CutInfo cutInfo = new(point0, point1, texture.width, texture.height);

            if (DivideTexture(texture, cutInfo, out var tex0, out var tex1))
            {
                if (cutInfo.isCorner)
                {
                    int diffY = cutInfo.p1.y - cutInfo.p0.y;
                    int diffX = cutInfo.p1.x - cutInfo.p0.x;
                    float m = diffY / (float)diffX;

                    bool b = false;
                    if (cutInfo.xMin == 0 && cutInfo.yMin == 0) b = true;
                    else if (cutInfo.xMin == 0 && cutInfo.yMax == texture.height) b = m >= 1;
                    else if (cutInfo.xMax == texture.width && cutInfo.yMin == 0) b = m < 1;
                    else if (cutInfo.xMax == texture.width && cutInfo.yMax == texture.height) b = false;

                    Vector2Int offset = new(cutInfo.xMin, cutInfo.yMin);
                    CutInfo tempCutInfo = new(cutInfo.p0 - offset, cutInfo.p1 - offset, tex1.width, tex1.height);
                    CutTexture(tex0, tempCutInfo, !b);
                    CutTexture(tex1, cutInfo, b);
                }
                else
                {
                    CutTexture(tex0, cutInfo, false);
                    Vector2Int offset = new(cutInfo.xMin, cutInfo.yMin);
                    CutInfo tempCutInfo = new(cutInfo.p0 - offset, cutInfo.p1 - offset, tex1.width, tex1.height);
                    CutTexture(tex1, tempCutInfo, true);
                }

                s0 = CreateSpriteRenderer(tex0, spriteRenderer.sprite.pixelsPerUnit);
                s1 = CreateSpriteRenderer(tex1, spriteRenderer.sprite.pixelsPerUnit);
                return true;
            }

            s0 = null;
            s1 = null;
            return false;
        }


        #endregion

        #region PRIVATE_FUNCTIONS

        private static bool GetLineLineIntersection(Vector2 A, Vector2 B, Vector2 C, Vector2 D, out Vector2 hit)
        {
            hit = Vector2.zero;
            Vector2 s1 = B - A;
            Vector2 s2 = D - C;

            float s = ((-s1.y) * (A.x - C.x) + s1.x * (A.y - C.y)) / ((-s2.x) * s1.y + s1.x * s2.y);
            float t = (s2.x * (A.y - C.y) - s2.y * (A.x - C.x)) / ((-s2.x) * s1.y + s1.x * s2.y);

            if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
            {
                hit = new Vector2 (A.x + (t * s1.x), A.y + (t * s1.y));
                return true;
            }

            return false;
        }

        private static bool DivideTexture(this Texture2D tex2D, CutInfo cutInfo, out Texture2D newTex2D0, out Texture2D newTex2D1)
        {
            newTex2D0 = null;
            newTex2D1 = null;

            int xMin = cutInfo.xMin;
            int xMax = cutInfo.xMax;
            int yMin = cutInfo.yMin;
            int yMax = cutInfo.yMax;

            if (xMin < 0 || yMin < 0 || xMax > tex2D.width || yMax > tex2D.height ||
                xMin >= tex2D.width || xMax <= 0 || yMin >= tex2D.height || yMax <= 0)
                return false;

            if (xMin == xMax)
            {
                if (yMin != 0 || yMax != tex2D.height)
                    return false;
            }
            if (yMin == yMax)
            {
                if (xMin != 0 || xMax != tex2D.width)
                    return false;
            }

            if (cutInfo.isCorner)
            {
                newTex2D0 = new Texture2D(xMax - xMin, yMax - yMin, tex2D.format, false);
                newTex2D1 = new Texture2D(tex2D.width, tex2D.height, tex2D.format, false);

                Graphics.CopyTexture(tex2D, 0, 0, xMin, yMin, newTex2D0.width, newTex2D0.height, newTex2D0, 0, 0, 0, 0);
                Graphics.CopyTexture(tex2D, 0, 0, 0, 0, newTex2D1.width, newTex2D1.height, newTex2D1, 0, 0, 0, 0);
            }
            else
            {
                newTex2D0 = new Texture2D(xMax, yMax, tex2D.format, false);
                newTex2D1 = new Texture2D(tex2D.width - xMin, tex2D.height - yMin, tex2D.format, false);

                Graphics.CopyTexture(tex2D, 0, 0, 0, 0, newTex2D0.width, newTex2D0.height, newTex2D0, 0, 0, 0, 0);
                Graphics.CopyTexture(tex2D, 0, 0, xMin, yMin, newTex2D1.width, newTex2D1.height, newTex2D1, 0, 0, 0, 0);
            }
            return true;
        }

        private static void CutTexture(this Texture2D tex2D, CutInfo cutInfo, bool inverted = false, int pixelDifferenceTolerance = 1)
        {
            int diffY = cutInfo.p1.y - cutInfo.p0.y;
            int diffX = cutInfo.p1.x - cutInfo.p0.x;
            if (Mathf.Abs(diffY) <= pixelDifferenceTolerance || Mathf.Abs(diffX) <= pixelDifferenceTolerance)
                return;

            float m = diffY / (float)diffX;
            float b = cutInfo.p0.y - m * cutInfo.p0.x;
            var pixels = tex2D.GetPixelData<Color32>(0);

            if (m > 0)
            {
                if (m < 1) inverted = !inverted;
                if (inverted)
                {
                    for (int y = cutInfo.yMin; y < cutInfo.yMax; y++)
                    {
                        int xLimit = (int)((y - b) / m);
                        for (int x = cutInfo.xMin; x < xLimit; x++)
                        {
                            int index = y * tex2D.width + x;
                            pixels[index] = pixels[index].SetFade(0);
                        }
                    }
                }
                else
                {
                    for (int y = cutInfo.yMin; y < cutInfo.yMax; y++)
                    {
                        int xLimit = (int)((y - b) / m);
                        for (int x = xLimit; x < cutInfo.xMax; x++)
                        {
                            int index = y * tex2D.width + x;
                            pixels[index] = pixels[index].SetFade(0);
                        }
                    }
                }
            }
            else
            {
                if (inverted)
                {
                    for (int y = cutInfo.yMin; y < cutInfo.yMax; y++)
                    {
                        int xLimit = (int)((y - b) / m);
                        for (int x = cutInfo.xMin; x < xLimit; x++)
                        {
                            int index = y * tex2D.width + x;
                            pixels[index] = pixels[index].SetFade(0);
                        }
                    }
                }
                else
                {
                    for (int y = cutInfo.yMin; y < cutInfo.yMax; y++)
                    {
                        int xLimit = (int)((y - b) / m);
                        for (int x = xLimit; x < cutInfo.xMax; x++)
                        {
                            int index = y * tex2D.width + x;
                            pixels[index] = pixels[index].SetFade(0);
                        }
                    }
                }
            }

            tex2D.SetPixelData(pixels, 0);
            tex2D.Apply(true, true);
        }

        private static SpriteRenderer CreateSpriteRenderer(this Texture2D tex2D, float ppu = 100f)
        {
            GameObject go = new GameObject();
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), new Vector2(.5f, .5f), ppu);
            return renderer;
        }

        #endregion
    }
}
