using System.Collections.Generic;
using UnityEngine;

namespace WinterCrestal.SpriteCutter
{
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

        public static bool CutSprite(this Sprite sprite, Vector2Int p1, Vector2Int p2, out SpriteRenderer sp1, out SpriteRenderer sp2, out Rect r1, out Rect r2)
        {
            sp1 = null; sp2 = null;
            if (!DivideRectangle(sprite.rect, p1, p2, out r1, out r2) ||
                r1.width == 0 || r1.height == 0 || r2.width == 0 || r2.height == 0)
                return false;

            var tex1 = GenerateNewTextureFragment(sprite.texture, r1);
            var tex2 = GenerateNewTextureFragment(sprite.texture, r2);

            float xMin = Mathf.Min(p1.x, p2.x);
            float xMax = Mathf.Max(p1.x, p2.x);
            float yMin = Mathf.Min(p1.y, p2.y);
            float yMax = Mathf.Max(p1.y, p2.y);
            float h = yMax - yMin;
            float w = xMax - xMin;
            float m = h / w;
            float altM = (p2.y - p1.y) / (float)(p2.x - p1.x);
            bool invSlope = Mathf.Sign(m) != Mathf.Sign(altM);

            if (CheckPointsOppositeVertical(sprite.rect, p1, p2))
            {
                if (invSlope)
                {
                    Color[] pxl1 = tex1.GetPixels((int)xMin, (int)yMin, (int)w, (int)h);
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < (int)(y / m); x++)
                            pxl1[y * (int)w + ((int)w - x - 1)] = Color.clear;
                    }
                    tex1.SetPixels((int)xMin, (int)yMin, (int)w, (int)h, pxl1);
                    tex1.Apply();

                    Color[] pxl2 = tex2.GetPixels(0, 0, (int)w, (int)h);
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = (int)(y / m); x < w; x++)
                            pxl2[y * (int)w + ((int)w - x - 1)] = Color.clear;
                    }
                    tex2.SetPixels(0, 0, (int)w, (int)h, pxl2);
                    tex2.Apply();
                }

                else
                {
                    Color[] pxl1 = tex1.GetPixels((int)xMin, (int)yMin, (int)w, (int)h);
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = (int)(y / m); x < w; x++)
                            pxl1[y * (int)w + x] = Color.clear;
                    }
                    tex1.SetPixels((int)xMin, (int)yMin, (int)w, (int)h, pxl1);
                    tex1.Apply();

                    Color[] pxl2 = tex2.GetPixels(0, 0, (int)w, (int)h);
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < (int)(y / m); x++)
                            pxl2[y * (int)w + x] = Color.clear;
                    }
                    tex2.SetPixels(0, 0, (int)w, (int)h, pxl2);
                    tex2.Apply();
                }


            }

            else if (CheckPointsOppositeHorizontal(sprite.rect, p1, p2))
            {
                if (invSlope)
                {
                    Color[] pxl1 = tex1.GetPixels((int)xMin, (int)yMin, (int)w, (int)h);
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < (int)(y / m); x++)
                            pxl1[y * (int)w + ((int)w - x - 1)] = Color.clear;
                    }
                    tex1.SetPixels((int)xMin, (int)yMin, (int)w, (int)h, pxl1);
                    tex1.Apply();

                    Color[] pxl2 = tex2.GetPixels(0, 0, (int)w, (int)h);
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = (int)(y / m); x < w; x++)
                            pxl2[y * (int)w + ((int)w - x - 1)] = Color.clear;
                    }
                    tex2.SetPixels(0, 0, (int)w, (int)h, pxl2);
                    tex2.Apply();
                }
                else
                {
                    Color[] pxl1 = tex1.GetPixels((int)xMin, (int)yMin, (int)w, (int)h);
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < (int)(y / m); x++)
                            pxl1[y * (int)w + x] = Color.clear;
                    }
                    tex1.SetPixels((int)xMin, (int)yMin, (int)w, (int)h, pxl1);
                    tex1.Apply();

                    Color[] pxl2 = tex2.GetPixels(0, 0, (int)w, (int)h);
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = (int)(y / m); x < w; x++)
                            pxl2[y * (int)w + x] = Color.clear;
                    }
                    tex2.SetPixels(0, 0, (int)w, (int)h, pxl2);
                    tex2.Apply();
                }

            }

            else
            {
                if (invSlope)
                {
                    if (r1.x == 0)
                    {
                        Color[] pxl1 = tex1.GetPixels(0, 0, (int)w, (int)h);
                        for (int y = 0; y < h; y++)
                        {
                            for (int x = 0; x < (int)(y / m); x++)
                                pxl1[y * (int)w + ((int)w - x - 1)] = Color.clear;
                        }
                        tex1.SetPixels(0, 0, (int)w, (int)h, pxl1);
                        tex1.Apply();

                        Color[] pxl2 = tex2.GetPixels((int)xMin, (int)yMin, (int)w, (int)h);
                        for (int y = 0; y < h; y++)
                        {
                            for (int x = (int)(y / m); x < w; x++)
                                pxl2[y * (int)w + ((int)w - x - 1)] = Color.clear;
                        }
                        tex2.SetPixels((int)xMin, (int)yMin, (int)w, (int)h, pxl2);
                        tex2.Apply();
                    }

                    else
                    {
                        Color[] pxl1 = tex1.GetPixels(0, 0, (int)w, (int)h);
                        for (int y = 0; y < h; y++)
                        {
                            for (int x = (int)(y / m); x < w; x++)
                                pxl1[y * (int)w + ((int)w - x - 1)] = Color.clear;
                        }
                        tex1.SetPixels(0, 0, (int)w, (int)h, pxl1);
                        tex1.Apply();

                        Color[] pxl2 = tex2.GetPixels((int)xMin, (int)yMin, (int)w, (int)h);
                        for (int y = 0; y < h; y++)
                        {
                            for (int x = 0; x < (int)(y / m); x++)
                                pxl2[y * (int)w + ((int)w - x - 1)] = Color.clear;
                        }
                        tex2.SetPixels((int)xMin, (int)yMin, (int)w, (int)h, pxl2);
                        tex2.Apply();
                    }


                }
                else
                {
                    if (p1.x > p2.x) (p2, p1) = (p1, p2);
                    if (p1.x == 0)
                    {
                        Color[] pxl1 = tex1.GetPixels(0, 0, (int)w, (int)h);
                        for (int y = 0; y < h; y++)
                        {
                            for (int x = (int)(y / m); x < w; x++)
                            {
                                pxl1[y * (int)w + x] = Color.clear;
                            }
                        }
                        tex1.SetPixels(0, 0, (int)w, (int)h, pxl1);
                        tex1.Apply();

                        Color[] pxl2 = tex2.GetPixels((int)xMin, (int)yMin, (int)w, (int)h);
                        for (int y = 0; y < h; y++)
                        {
                            for (int x = 0; x < (int)(y / m); x++)
                                pxl2[y * (int)w + x] = Color.clear;
                        }
                        tex2.SetPixels((int)xMin, (int)yMin, (int)w, (int)h, pxl2);
                        tex2.Apply();
                    }

                    else
                    {
                        Color[] pxl1 = tex1.GetPixels(0, 0, (int)w, (int)h);
                        for (int y = 0; y < h; y++)
                        {
                            for (int x = 0; x < (int)(y / m); x++)
                            {
                                pxl1[y * (int)w + x] = Color.clear;
                            }
                        }
                        tex1.SetPixels(0, 0, (int)w, (int)h, pxl1);
                        tex1.Apply();

                        Color[] pxl2 = tex2.GetPixels((int)xMin, (int)yMin, (int)w, (int)h);
                        for (int y = 0; y < h; y++)
                        {
                            for (int x = (int)(y / m); x < w; x++)
                                pxl2[y * (int)w + x] = Color.clear;
                        }
                        tex2.SetPixels((int)xMin, (int)yMin, (int)w, (int)h, pxl2);
                        tex2.Apply();
                    }

                }


            }

            GameObject n1 = new(sprite.name + "_0");
            sp1 = n1.AddComponent<SpriteRenderer>();
            sp1.sprite = Sprite.Create(tex1, new Rect(0, 0, tex1.width, tex1.height), new Vector2(.5f, .5f), sprite.pixelsPerUnit);

            GameObject n2 = new(sprite.name + "_1");
            sp2 = n2.AddComponent<SpriteRenderer>();
            sp2.sprite = Sprite.Create(tex2, new Rect(0, 0, tex2.width, tex2.height), new Vector2(.5f, .5f), sprite.pixelsPerUnit);
            return true;
        }

        #endregion

        #region PRIVATE_FUNCTIONS
        private static Texture2D GenerateNewTextureFragment(Texture2D texture, RectInt rect)
        {
            Texture2D partialTex2D = new(rect.width, rect.height);
            var pixels = texture.GetPixels(rect.x, rect.y, rect.width, rect.height);
            partialTex2D.SetPixels(pixels);
            partialTex2D.Apply();

            return partialTex2D;
        }

        private static Texture2D GenerateNewTextureFragment(Texture2D texture, Rect rect)
        {
            RectInt r = new((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            return GenerateNewTextureFragment(texture, r);
        }

        private static bool CheckPointsOppositeVertical(Rect rect, Vector2 p1,  Vector2 p2)
        {
            return ((p1.y == 0 && p2.y == rect.height) || (p1.y == rect.height && p2.y == 0));
        }

        private static bool CheckPointsOppositeHorizontal(Rect rect, Vector2 p1, Vector2 p2)
        {
            return ((p1.x == 0 && p2.x == rect.width) || (p1.x == rect.width && p2.x == 0));
        }

        private static bool ReturnEmptyRects(out Rect r1, out Rect r2)
        {
            r1 = new();
            r2 = new();
            return false;
        }

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

        private static bool DivideRectangle(Rect rect, Vector2 p1,  Vector2 p2, out Rect rect1, out Rect rect2)
        {
            if(p1.x == p2.x)
            {
                float yMin = Mathf.Min(p1.y, p2.y);
                float yMax = Mathf.Max(p1.y, p2.y);
                if(yMin != 0 || yMax != rect.height)
                    return ReturnEmptyRects(out rect1, out rect2);
                else
                {
                    rect1 = new Rect(rect.x, rect.y, p1.x, rect.height);
                    rect2 = new Rect(rect.x + p1.x, rect.y, rect.width - p1.x, rect.height);
                    return true;
                }
            }

            else if(p1.y == p2.y)
            {
                float xMin = Mathf.Min(p1.x, p2.x);
                float xMax = Mathf.Max(p1.x, p2.x);
                if (xMin != 0 || xMax != rect.width)
                    return ReturnEmptyRects(out rect1, out rect2);
                else
                {
                    rect1 = new Rect(rect.x, rect.y, rect.width, p1.y);
                    rect2 = new Rect(rect.x, rect.y + p1.y, rect.width, rect.height - p1.y);
                    return true;
                }
            }

            if (CheckPointsOppositeVertical(rect, p1, p2))
            {
                if (p1.x > p2.x) (p2, p1) = (p1, p2);
                rect1 = new Rect(rect.x, rect.y, p2.x, rect.height);
                rect2 = new Rect(rect.x + p1.x, rect.y, rect.width - p1.x, rect.height);
                return true;
            }

            else if (CheckPointsOppositeHorizontal(rect, p1, p2))
            {
                if (p1.y > p2.y) (p2, p1) = (p1, p2);
                rect1 = new Rect(rect.x, rect.y, rect.width, p2.y);
                rect2 = new Rect(rect.x, rect.y + p1.y, rect.width, rect.height - p1.y);
                return true;
            }

            else
            {
                float xMin = Mathf.Min(p1.x, p2.x);
                float xMax = Mathf.Max(p1.x, p2.x);
                float yMin = Mathf.Min(p1.y, p2.y);
                float yMax = Mathf.Max(p1.y, p2.y);

                if(xMin == 0)
                {
                    if(yMin != 0 && yMax != rect.height)
                        return ReturnEmptyRects(out rect1, out rect2);
                }
                else if(xMax == rect.width)
                {
                    if (yMin != 0 && yMax != rect.height)
                        return ReturnEmptyRects(out rect1, out rect2);
                }
                else
                {
                    return ReturnEmptyRects(out rect1, out rect2);
                }

                rect1 = new(rect.x + xMin, rect.y + yMin, xMax - xMin, yMax - yMin);
                rect2 = new(rect);

                return true;
            }
            
        }
        
        #endregion
    }
}
