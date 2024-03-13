using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace WinterCrestal.SpriteCutter
{
    /// <summary>
    /// Struct to hold the split sprite's info, Rect holds info for recreating the relative position of the new sprites
    /// </summary>
    public struct SplitSprite
    {
        public SpriteRenderer SpriteRenderer;
        public Rect Rect;
        public SplitSprite(SpriteRenderer spriteRenderer, Rect rect)
        { SpriteRenderer = spriteRenderer; Rect = rect; }
    }

    /// <summary>
    /// Creates relevant information from a pair of points (in sprite local space) and the texture dimensions
    /// </summary>
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
        /// <summary>
        /// Extension to quickly set the alpha value of a Color32 variable
        /// </summary>
        /// <param name="c">The color to set the alpha</param>
        /// <param name="fade">The alpha value</param>
        /// <returns>The faded color</returns>
        public static Color32 SetFade(this Color32 c, byte fade)
        {
            c.a = fade;
            return c;
        }
    }

    public static class SpriteCutterUtils
    {

        #region PUBLIC_FUNCTIONS

        /// <summary>
        /// Converts a point in world space to sprite's local space
        /// </summary>
        /// <param name="renderer">The sprite renderer whose sprite is to be considered</param>
        /// <param name="worldPoint">A point in world coordinates</param>
        /// <returns>A point in sprite's local space</returns>
        public static Vector2Int WorldToSpriteLocal(this SpriteRenderer renderer, Vector2 worldPoint)
        {
            Vector2 localPos = renderer.transform.InverseTransformPoint(worldPoint);
            localPos /= renderer.localBounds.size;
            localPos += new Vector2(.5f, .5f);
            return new Vector2Int((int)(localPos.x * renderer.sprite.rect.width), (int)(localPos.y * renderer.sprite.rect.height));
        }

        /// <summary>
        /// Converts a point in sprite's local space to world space
        /// </summary>
        /// <param name="renderer">The sprite renderer whose sprite is to be considered</param>
        /// <param name="localPoint">A point in sprite's local space</param>
        /// <returns>A point in world space</returns>
        public static Vector2 SpriteLocalToWorld(this SpriteRenderer renderer, Vector2 localPoint)
        {
            Vector2 spriteSize = new(renderer.sprite.rect.width, renderer.sprite.rect.height);
            Vector2 p = localPoint / spriteSize;
            p -= new Vector2(.5f, .5f);
            p *= renderer.localBounds.size;
            return renderer.transform.TransformPoint(p);
        }

        /// <summary>
        /// Test if a line intersects the sprite renderer
        /// </summary>
        /// <param name="renderer">The sprite renderer whose sprite is to be considered</param>
        /// <param name="p0">Line's start point in world coordinates</param>
        /// <param name="p1">Line's end point in world coordinates</param>
        /// <param name="hitPoint0">Point of intersection in world coordinates (for 1st edge hit)</param>
        /// <param name="hitPoint1">Point of intersection in world coordinates (for 2nd edge hit)</param>
        /// <returns>number of edges the line intersects (0, 1 or 2)</returns>
        public static uint IntersectLine(this SpriteRenderer renderer, Vector2 p0, Vector2 p1, out Vector2 hitPoint0, out Vector2 hitPoint1)
        {
            hitPoint0 = Vector2.zero;
            hitPoint1 = Vector2.zero;
            var bounds = renderer.localBounds;

            Vector3[] corners = new Vector3[4];
            corners[0] = new(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y);
            corners[1] = new(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y);
            corners[2] = new(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y);
            corners[3] = new(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y);

            for (int i = 0; i < 4; i++) corners[i] = renderer.transform.TransformPoint((corners[i] - bounds.center));

            //for (int i = 0; i < 4; i++) corners[i] = bounds.center + rotation * (corners[i] - bounds.center) + offset;

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

        /// <summary>
        /// Cuts the attached sprite's texture into 2 separate sprites and creates a new GameObject for each
        /// </summary>
        /// <param name="spriteRenderer">The sprite renderer whose sprite to cut</param>
        /// <param name="point0">A point on the texture's edge in sprite local space</param>
        /// <param name="point1">A point on the texture's edge in sprite local space</param>
        /// <param name="s0">1st part of the cut sprite if successful, else null</param>
        /// <param name="s1">2nd part of the cut sprite if successful, else null</param>
        /// <returns>true if the cut was successful or valid, else false</returns>
        public static bool CutSprite(this SpriteRenderer spriteRenderer, Vector2Int point0, Vector2Int point1, out SpriteRenderer s0, out SpriteRenderer s1, bool checkEmptyTexture = true, byte alphaThreshold = 0)
        {
            s0 = null; s1 = null;

            var texture = spriteRenderer.sprite.texture;
            CutInfo cutInfo = new(point0, point1, texture.width, texture.height);

            if (texture.DivideTexture(cutInfo, out var tex0, out var tex1))
            {
                bool isTex0Empty = false, isTex1Empty = false;
                if(checkEmptyTexture)
                {
                    isTex0Empty = tex0.IsEmpty(alphaThreshold);
                    isTex1Empty = tex1.IsEmpty(alphaThreshold);
                }

                if (cutInfo.isCorner)
                {
                    int diffY = cutInfo.p1.y - cutInfo.p0.y;
                    int diffX = cutInfo.p1.x - cutInfo.p0.x;
                    float m = diffY / (float)diffX;

                    bool b = false;
                    if (cutInfo.xMin == 0 && cutInfo.yMin == 0) b = true;
                    else if (cutInfo.xMin == 0 && cutInfo.yMax == texture.height) b = m >= 1;
                    else if (cutInfo.xMax == texture.width && cutInfo.yMin == 0) b = m < 1;

                    if(!isTex0Empty)
                    {
                        Vector2Int offset = new(cutInfo.xMin, cutInfo.yMin);
                        CutInfo tempCutInfo = new(cutInfo.p0 - offset, cutInfo.p1 - offset, tex1.width, tex1.height);
                        tex0.CutTexture(tempCutInfo, !b);
                    }
                    if(!isTex1Empty)
                        tex1.CutTexture(cutInfo, b);
                }
                else
                {
                    if(!isTex0Empty)
                        tex0.CutTexture(cutInfo, false);
                    if(!isTex1Empty)
                    {
                        Vector2Int offset = new(cutInfo.xMin, cutInfo.yMin);
                        CutInfo tempCutInfo = new(cutInfo.p0 - offset, cutInfo.p1 - offset, tex1.width, tex1.height);
                        tex1.CutTexture(tempCutInfo, true);
                    }
                }

                if(!isTex0Empty) s0 = tex0.CreateSpriteRenderer(spriteRenderer.sprite.pixelsPerUnit);
                if(!isTex1Empty) s1 = tex1.CreateSpriteRenderer(spriteRenderer.sprite.pixelsPerUnit);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the texture contains no pixels whose transparency is greater than the threshold (in Color32)
        /// </summary>
        /// <param name="tex2D">The texture to check</param>
        /// <param name="alphaThreshold">Threshold to consider if the pixel is empty or not (Default: 0)</param>
        /// <returns>true if empty else false</returns>
        public static bool IsEmpty(this Texture2D tex2D, byte alphaThreshold = 0)
        {
            var pixels = tex2D.GetPixelData<Color32>(0);

            for (int y = 0; y < tex2D.height; y++)
            {
                for(int x = 0; x < tex2D.width; x++)
                {
                    int index = y * tex2D.width + x;
                    if (pixels[index].a > alphaThreshold)
                        return false;
                }
            }

            return true;
        }

        #endregion

        #region PRIVATE_FUNCTIONS

        /// <summary>
        /// Line-Line intersection test
        /// </summary>
        /// <param name="A">Start point of 1st line</param>
        /// <param name="B">End point of 1st line</param>
        /// <param name="C">Start point of 2nd line</param>
        /// <param name="D">End point of 2nd line</param>
        /// <param name="hit">Intersection point of the 2 lines</param>
        /// <returns>true if lines intersect else false</returns>
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

        /// <summary>
        /// Divides a texture2D into 2 texture2D copies
        /// </summary>
        /// <param name="tex2D">Texture to split/divide</param>
        /// <param name="cutInfo">Relevant information about the cut</param>
        /// <param name="newTex2D0">1st slice of the divided texture</param>
        /// <param name="newTex2D1">2nd slice of the divided texture</param>
        /// <returns></returns>
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
                //if(!(xMin == 0 && xMax == tex2D.width) || !(yMin == 0 && yMax == tex2D.height)) return false;

                newTex2D0 = new Texture2D(xMax, yMax, tex2D.format, false);
                newTex2D1 = new Texture2D(tex2D.width - xMin, tex2D.height - yMin, tex2D.format, false);

                Graphics.CopyTexture(tex2D, 0, 0, 0, 0, newTex2D0.width, newTex2D0.height, newTex2D0, 0, 0, 0, 0);
                Graphics.CopyTexture(tex2D, 0, 0, xMin, yMin, newTex2D1.width, newTex2D1.height, newTex2D1, 0, 0, 0, 0);
            }

            newTex2D0.filterMode = tex2D.filterMode;
            newTex2D1.filterMode = tex2D.filterMode;

            return true;
        }

        /// <summary>
        /// Clears out the color from the side of the line/cut
        /// </summary>
        /// <param name="tex2D">Texture to clear out the pixel colors</param>
        /// <param name="cutInfo">Relevant information about the cut</param>
        /// <param name="inverted">Whether to switch the side to clear the colors</param>
        /// <param name="pixelDifferenceTolerance">Difference in x and y position of the cut points before processing further (useful for horizontal and vertical cuts)</param>
        /// <param name="makeNoLongerReadable">Whether the texture should be made unreadable after finishing the cut</param>
        private static void CutTexture(this Texture2D tex2D, CutInfo cutInfo, bool inverted = false, int pixelDifferenceTolerance = 1, bool makeNoLongerReadable = true)
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
            tex2D.Apply(true, makeNoLongerReadable);
        }

        /// <summary>
        /// Creates a new GameObject and attaches all the required components for the given texture
        /// </summary>
        /// <param name="tex2D">The texture for which the sprite and GameObject is created</param>
        /// <param name="ppu">The pixels per unit for the sprite</param>
        /// <returns>The attached SpriteRenderer of the created GameObject</returns>
        private static SpriteRenderer CreateSpriteRenderer(this Texture2D tex2D, float ppu = 100f)
        {
            GameObject go = new GameObject();
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), new Vector2(.5f, .5f), ppu);
            
            return renderer;
        }

        #endregion
    }

    public static class Texture2DUtils
    {
        public enum SaveTextureFileFormat { EXR, JPG, PNG, TGA };

        /// <summary>
        /// Fast save texture to file
        /// Credits: https://forum.unity.com/threads/save-rendertexture-or-texture2d-as-image-file-utility.1325130/#post-8387577
        /// </summary>
        /// <param name="source"></param>
        /// <param name="filePath"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="fileFormat"></param>
        /// <param name="jpgQuality"></param>
        /// <param name="asynchronous"></param>
        /// <param name="done"></param>
        static public void SaveTextureToFile(Texture source,
                                         string filePath,
                                         int width,
                                         int height,
                                         SaveTextureFileFormat fileFormat = SaveTextureFileFormat.PNG,
                                         int jpgQuality = 95,
                                         bool asynchronous = true,
                                         System.Action<bool> done = null)
        {
            // check that the input we're getting is something we can handle:
            if (!(source is Texture2D || source is RenderTexture))
            {
                done?.Invoke(false);
                return;
            }

            // use the original texture size in case the input is negative:
            if (width < 0 || height < 0)
            {
                width = source.width;
                height = source.height;
            }

            // resize the original image:
            var resizeRT = RenderTexture.GetTemporary(width, height, 0);
            Graphics.Blit(source, resizeRT);

            // create a native array to receive data from the GPU:
            var narray = new NativeArray<byte>(width * height * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // request the texture data back from the GPU:
            var request = AsyncGPUReadback.RequestIntoNativeArray(ref narray, resizeRT, 0, (AsyncGPUReadbackRequest request) =>
            {
                // if the readback was successful, encode and write the results to disk
                if (!request.hasError)
                {
                    NativeArray<byte> encoded;

                    switch (fileFormat)
                    {
                        case SaveTextureFileFormat.EXR:
                            encoded = ImageConversion.EncodeNativeArrayToEXR(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                            break;
                        case SaveTextureFileFormat.JPG:
                            encoded = ImageConversion.EncodeNativeArrayToJPG(narray, resizeRT.graphicsFormat, (uint)width, (uint)height, 0, jpgQuality);
                            break;
                        case SaveTextureFileFormat.TGA:
                            encoded = ImageConversion.EncodeNativeArrayToTGA(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                            break;
                        default:
                            encoded = ImageConversion.EncodeNativeArrayToPNG(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                            break;
                    }

                    System.IO.File.WriteAllBytes(filePath, encoded.ToArray());
                    encoded.Dispose();
                }

                narray.Dispose();

                // notify the user that the operation is done, and its outcome.
                done?.Invoke(!request.hasError);
            });

            if (!asynchronous)
                request.WaitForCompletion();
        }

        public static void SaveToFIle(this Texture2D tex2D, 
                                        string filePath, 
                                        SaveTextureFileFormat fileFormat = SaveTextureFileFormat.PNG, 
                                        int jpgQuality = 95,
                                        bool asynchronous = true,
                                        System.Action<bool> done = null)
        {
            SaveTextureToFile(tex2D, filePath, tex2D.width, tex2D.height, fileFormat, jpgQuality, asynchronous, done);
        }

        public static void DebugInfo(this Texture2D tex2D)
        {
            System.Text.StringBuilder s = new("Dimensions: " + tex2D.width + " x " + tex2D.height + "\n");
            s.AppendLine("Format: " + tex2D.format.ToString());
            s.AppendLine("isDataSRGB: " + tex2D.isDataSRGB);
            s.AppendLine("FilterMode: " + tex2D.filterMode);
            s.AppendLine("mipmapCount: " + tex2D.mipmapCount);
            Debug.Log(s.ToString());
        }
    }
}
