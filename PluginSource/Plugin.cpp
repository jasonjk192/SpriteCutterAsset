#include "Plugin.h"
#include <string>

extern "C"
{
	EXPORT_API void LoadUnityInterfacePtr(uint64_t unityInterfacesPtr)
	{
		IUnityInterfaces* unityInterfaces = reinterpret_cast<IUnityInterfaces*>(unityInterfacesPtr);
		if (unityInterfaces != nullptr)
		{
			unityLogPtr = unityInterfaces->Get<IUnityLog>();
			//UnityPluginLoad(unityInterfaces);
		}
	}
}

extern "C"
{
	EXPORT_API void ProcessTexture2D(void* textureHandle, int textureWidth, int textureHeight, float point1X, float point1Y, float point2X, float point2Y)
	{
		unsigned char* texData = static_cast<unsigned char*>(textureHandle);

        float m = (point2Y - point1Y) / (point2X - point1X);
        float b = point1Y - m * point1X;

        for (int y = 0; y < textureHeight; ++y) 
        {
            for (int x = 0; x < textureWidth; ++x) 
            {
                int pixelIndex = (y * textureWidth + x) * 4;

                float pixelY = static_cast<float>(y);
                float expectedX = (pixelY - b) / m;

                if (x < expectedX) 
                {
                    // Above the line: Set red channel, keep green and blue as zero.
                    texData[pixelIndex + 1] = 0;
                    texData[pixelIndex + 2] = 0;
                }
                else {
                    // Below the line: Set blue channel, keep red and green as zero.
                    texData[pixelIndex] = 0; 
                    texData[pixelIndex + 1] = 0; 
                }
            }
        }
	}

}