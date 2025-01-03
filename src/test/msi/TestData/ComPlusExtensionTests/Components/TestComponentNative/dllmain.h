// dllmain.h : Declaration of module class.

class CTestComponentNativeModule : public ATL::CAtlDllModuleT< CTestComponentNativeModule >
{
public :
	DECLARE_LIBID(LIBID_TestComponentNativeLib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_TESTCOMPONENTNATIVE, "{8aaadab2-ac31-4618-ad2b-6b71d2a318eb}")
};

extern class CTestComponentNativeModule _AtlModule;
