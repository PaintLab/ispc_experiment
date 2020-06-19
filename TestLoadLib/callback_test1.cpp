#ifdef _WIN32 
#define MY_DLL_EXPORT __declspec(dllexport)
#else 
#define MY_DLL_EXPORT
#endif

extern "C" {
	//typedef void(__cdecl* managed_callback)(int id, void* args);

	typedef void(__cdecl* managed_callback)(int data);

	managed_callback myext_mcallback;
	MY_DLL_EXPORT void set_managed_callback(managed_callback m_callback) {
		myext_mcallback = m_callback;
	}

	void appFunc(int activeLanes) { 
		//this func is call from iscp
		if (myext_mcallback) {
			myext_mcallback(activeLanes);
		}
	}
}