using fts;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

[PluginAttr("Windows/x64/SpriteCutterPlugin")]
public static class SCPlugin
{
    [PluginFunctionAttr("LoadUnityInterfacePtr")]
    public static LoadUnityInterfacePtr ptrLoader = null;
    public delegate void LoadUnityInterfacePtr(ulong ptr);

    [PluginFunctionAttr("ProcessTexture2D")]
    public static ProcessTexture2D processTexture2D = null;
    public delegate int ProcessTexture2D(IntPtr textureHandle, int width, int height, float x0, float y0, float x1, float y1);
}

public class TestScript : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [DllImport("UnityInterfacesBinderPlugin")] private static extern ulong GetUnityInterfacePtr();

    void Start()
    {
        var interfacePtr = GetUnityInterfacePtr();
        SCPlugin.ptrLoader(interfacePtr);

        var texture = _spriteRenderer.sprite.texture;
        _spriteRenderer.sprite = Sprite.Create(ProcessTexture2D(texture), new Rect(0,0,texture.width, texture.height), new Vector2(.5f,.5f), _spriteRenderer.sprite.pixelsPerUnit);
    }


    public static Texture2D ProcessTexture2D(Texture2D tex2D)
    {
        var pixels = tex2D.GetPixels32(0);

        GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            SCPlugin.processTexture2D(handle.AddrOfPinnedObject(), tex2D.width, tex2D.height, 0, tex2D.height * .25f, tex2D.width, tex2D.height * .75f);
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }

        var newTex2D = new Texture2D(tex2D.width, tex2D.height, tex2D.format, false);

        newTex2D.SetPixels32(pixels, 0);
        newTex2D.Apply();
        return newTex2D;
    }
}
