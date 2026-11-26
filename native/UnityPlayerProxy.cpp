// Native loader for Thief Simulator 2 (Unity 2020.3 Mono, x64).
//
// This DLL replaces UnityPlayer.dll. The original engine DLL is renamed to
// UnityPlayer_Orig.dll. UnityPlayer.dll exports exactly one symbol: UnityMain.
// We forward it to the original engine, and from a worker thread we attach to
// Mono, wait for the game assemblies, then load and invoke our managed mod.
//
// No BepInEx / doorstop / external loader is used.

#include <windows.h>
#include <stdio.h>

typedef int(__cdecl *UnityMainFn)(HINSTANCE, HINSTANCE, PWSTR, int);

struct MonoDomain;
struct MonoAssembly;
struct MonoImage;
struct MonoClass;
struct MonoMethod;
struct MonoObject;

typedef MonoDomain *(*fn_root_domain)();
typedef MonoDomain *(*fn_domain_get)();
typedef void *(*fn_thread_attach)(MonoDomain *);
typedef MonoAssembly *(*fn_assembly_open)(const char *, int *);
typedef MonoAssembly *(*fn_domain_assembly_open)(MonoDomain *, const char *);
typedef MonoImage *(*fn_assembly_get_image)(MonoAssembly *);
typedef MonoClass *(*fn_class_from_name)(MonoImage *, const char *, const char *);
typedef MonoMethod *(*fn_get_method)(MonoClass *, const char *, int);
typedef MonoObject *(*fn_runtime_invoke)(MonoMethod *, void *, void **, MonoObject **);

static HMODULE g_engine = NULL;
static UnityMainFn g_realUnityMain = NULL;
static volatile LONG g_started = 0;
static wchar_t g_logPath[MAX_PATH];

static void Log(const char *fmt, ...)
{
    char line[1024];
    va_list args;
    va_start(args, fmt);
    vsnprintf_s(line, sizeof(line), fmt, args);
    va_end(args);

    FILE *f = NULL;
    _wfopen_s(&f, g_logPath, L"a");
    if (f)
    {
        fputs(line, f);
        fputc('\n', f);
        fclose(f);
    }
}

static void InitLogPath()
{
    wchar_t exe[MAX_PATH];
    GetModuleFileNameW(NULL, exe, MAX_PATH);
    wchar_t *slash = wcsrchr(exe, L'\\');
    if (slash) *(slash + 1) = L'\0';

    wchar_t logs[MAX_PATH];
    swprintf_s(logs, MAX_PATH, L"%sMod\\logs", exe);
    CreateDirectoryW(logs, NULL);
    swprintf_s(g_logPath, MAX_PATH, L"%s\\native-loader.log", logs);
}

static FARPROC GetMonoProc(HMODULE mono, const char *name)
{
    FARPROC p = GetProcAddress(mono, name);
    if (!p) Log("  [!] missing Mono export: %s", name);
    return p;
}

static DWORD WINAPI InjectThread(LPVOID)
{
    Log("Inject worker thread started.");

    HMODULE mono = NULL;
    for (int i = 0; i < 900; ++i)
    {
        mono = GetModuleHandleW(L"mono-2.0-bdwgc.dll");
        if (mono) break;
        Sleep(100);
    }
    if (!mono)
    {
        Log("[ERROR] Mono runtime module was never loaded.");
        return 1;
    }
    Log("Mono runtime found.");

    fn_root_domain rootDomain = (fn_root_domain)GetMonoProc(mono, "mono_get_root_domain");
    fn_domain_get domainGet = (fn_domain_get)GetMonoProc(mono, "mono_domain_get");
    fn_thread_attach attach = (fn_thread_attach)GetMonoProc(mono, "mono_thread_attach");
    fn_domain_assembly_open domainAsmOpen = (fn_domain_assembly_open)GetMonoProc(mono, "mono_domain_assembly_open");
    fn_assembly_get_image getImage = (fn_assembly_get_image)GetMonoProc(mono, "mono_assembly_get_image");
    fn_class_from_name classFromName = (fn_class_from_name)GetMonoProc(mono, "mono_class_from_name");
    fn_assembly_open asmOpen = (fn_assembly_open)GetMonoProc(mono, "mono_assembly_open");
    fn_get_method getMethod = (fn_get_method)GetMonoProc(mono, "mono_class_get_method_from_name");
    fn_runtime_invoke invoke = (fn_runtime_invoke)GetMonoProc(mono, "mono_runtime_invoke");

    if (!rootDomain || !attach || !domainAsmOpen || !getImage || !classFromName ||
        !asmOpen || !getMethod || !invoke)
    {
        Log("[ERROR] Required Mono API functions are unavailable.");
        return 1;
    }

    MonoDomain *domain = rootDomain();
    if (!domain && domainGet) domain = domainGet();
    if (!domain)
    {
        Log("[ERROR] No Mono domain.");
        return 1;
    }

    // Give Unity a moment to finish creating the scripting domain.
    Sleep(1500);
    attach(domain);
    Log("Mono domain attached.");

    char exeA[MAX_PATH];
    GetModuleFileNameA(NULL, exeA, MAX_PATH);
    char *slash = strrchr(exeA, '\\');
    if (slash) *(slash + 1) = '\0';
    char managedDir[MAX_PATH];
    snprintf(managedDir, MAX_PATH, "%sThief Simulator 2_Data\\Managed\\", exeA);

    // Pass the game root to managed code so logging works even when
    // Assembly.Location / AppDomain.BaseDirectory are empty in Unity Mono.
    SetEnvironmentVariableA("THIEF_ENHANCED_ROOT", exeA);

    // Explicitly preload the engine assemblies our mod references. Mono resolves
    // a reference by simple name from the already-loaded set, so this guarantees
    // our patches target the very same Assembly-CSharp the game uses.
    // Harmony / MonoMod are merged into Thief2Enhanced.dll and are deliberately
    // not loaded separately.
    static const char *deps[] = {
        "netstandard.dll",
        "UnityEngine.dll",
        "UnityEngine.CoreModule.dll",
        "UnityEngine.IMGUIModule.dll",
        "UnityEngine.InputLegacyModule.dll",
        "UnityEngine.UI.dll",
        "UnityEngine.UIModule.dll",
        "UnityEngine.PhysicsModule.dll",
        "UnityEngine.AudioModule.dll",
        "UnityEngine.TextRenderingModule.dll",
        "Unity.TextMeshPro.dll"
        // Assembly-CSharp is intentionally NOT preloaded: Unity loads it itself.
        // Loading a second copy would make our patches target the wrong assembly.
    };
    for (int i = 0; i < (int)(sizeof(deps) / sizeof(deps[0])); ++i)
    {
        char path[MAX_PATH];
        snprintf(path, MAX_PATH, "%s%s", managedDir, deps[i]);
        if (!asmOpen(path, NULL))
            Log("  [!] dependency could not be preloaded: %s", deps[i]);
    }
    Log("Engine assemblies preloaded.");

    char modPath[MAX_PATH];
    snprintf(modPath, MAX_PATH, "%sThief2Enhanced.dll", managedDir);

    Log("Loading managed mod: %s", modPath);
    MonoAssembly *mod = asmOpen(modPath, NULL);
    if (!mod)
    {
        Log("[ERROR] mono_assembly_open failed for the mod DLL.");
        return 1;
    }

    MonoImage *modImage = getImage(mod);
    if (!modImage)
    {
        Log("[ERROR] No image in mod assembly.");
        return 1;
    }

    MonoClass *entry = classFromName(modImage, "ThiefSimulator2Mod", "NativeEntry");
    if (!entry)
    {
        Log("[ERROR] NativeEntry class not found.");
        return 1;
    }

    MonoMethod *initialize = getMethod(entry, "Initialize", 0);
    if (!initialize)
    {
        Log("[ERROR] NativeEntry.Initialize() not found.");
        return 1;
    }

    MonoObject *exc = NULL;
    MonoObject *result = invoke(initialize, NULL, NULL, &exc);
    Log("Invoke returned: result=%p exception=%p", result, exc);
    if (exc)
    {
        Log("[ERROR] NativeEntry.Initialize() threw a managed exception (%p).", exc);
        return 1;
    }

    Log("SUCCESS: managed mod initialized. Menu key is F1 by default.");
    return 0;
}

extern "C" __declspec(dllexport) int UnityMain(HINSTANCE hInstance, HINSTANCE hPrev, PWSTR cmdLine, int nCmdShow)
{
    if (!g_engine)
    {
        wchar_t path[MAX_PATH];
        GetModuleFileNameW(NULL, path, MAX_PATH);
        wchar_t *slash = wcsrchr(path, L'\\');
        if (slash) *(slash + 1) = L'\0';
        wcscat_s(path, MAX_PATH, L"UnityPlayer_Orig.dll");

        g_engine = LoadLibraryW(path);
        if (!g_engine)
        {
            InitLogPath();
            Log("[ERROR] Could not load UnityPlayer_Orig.dll (error %lu).", GetLastError());
            return 1;
        }
        g_realUnityMain = (UnityMainFn)GetProcAddress(g_engine, "UnityMain");
        if (!g_realUnityMain)
        {
            InitLogPath();
            Log("[ERROR] UnityMain export missing from original engine DLL.");
            return 1;
        }
        InitLogPath();
        Log("Native UnityPlayer proxy loaded. Original engine loaded.");
    }

    if (InterlockedExchange(&g_started, 1) == 0)
    {
        HANDLE thread = CreateThread(NULL, 0, InjectThread, NULL, 0, NULL);
        if (thread) CloseHandle(thread);
        else Log("[ERROR] CreateThread failed.");
    }

    return g_realUnityMain(hInstance, hPrev, cmdLine, nCmdShow);
}
