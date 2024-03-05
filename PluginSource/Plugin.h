#pragma once

#if _MSC_VER
#define EXPORT_API __declspec(dllexport)
#else
#define EXPORT_API
#endif

#include "IUnityLog.h"
#include <cstdint>

//for holding the reference ptr until plugin unloads
static IUnityLog* unityLogPtr = nullptr;

extern "C"
{
	// Setup
	EXPORT_API void LoadUnityInterfacePtr(uint64_t unityInterfacesPtr);

	// Texture Handling
	EXPORT_API void ProcessTexture2D(void* textureHandle, int textureWidth, int textureHeight, float point1X, float point1Y, float point2X, float point2Y);
}