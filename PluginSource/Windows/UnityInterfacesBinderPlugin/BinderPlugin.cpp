#include "../../IUnityInterface.h"
#include <cstdint>

static IUnityInterfaces* s_UnityInterfaces = nullptr;

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces * unityInterfaces)
{
	s_UnityInterfaces = unityInterfaces;
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
	s_UnityInterfaces = nullptr;
}

extern "C" uint64_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetUnityInterfacePtr()
{
	return reinterpret_cast<uint64_t>(s_UnityInterfaces);
}