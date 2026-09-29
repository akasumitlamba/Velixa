#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <audioclient.h>
#include <mmdeviceapi.h>
#include <audioclientactivationparams.h>
#include <wrl.h>
#include <thread>
#include <vector>
#include <atomic>
using namespace Microsoft::WRL;
using AudioCallback = void (__stdcall *)(const BYTE*, int);

// An agile completion object owns everything the asynchronous COM call touches.
// It remains valid even when activation outlives the caller's timeout.
class Activation final : public RuntimeClass<RuntimeClassFlags<ClassicCom>, FtmBase, IActivateAudioInterfaceCompletionHandler> {
public:
    HANDLE ready = CreateEvent(nullptr, TRUE, FALSE, nullptr);
    HRESULT result = E_PENDING;
    ComPtr<IAudioClient> client;
    ~Activation() { if (ready) CloseHandle(ready); }
    STDMETHOD(ActivateCompleted)(IActivateAudioInterfaceAsyncOperation* operation) override {
        ComPtr<IUnknown> unknown;
        HRESULT activated = E_FAIL;
        result = operation->GetActivateResult(&activated, &unknown);
        if (SUCCEEDED(result)) result = activated;
        if (SUCCEEDED(result)) result = unknown.As(&client);
        SetEvent(ready);
        return S_OK;
    }
};

struct Capture {
    HANDLE stop = CreateEvent(nullptr, TRUE, FALSE, nullptr);
    HANDLE ready = CreateEvent(nullptr, TRUE, FALSE, nullptr);
    std::thread worker;
    std::atomic<HRESULT> result{E_PENDING};
    ~Capture() { SetEvent(stop); if (worker.joinable()) worker.join(); CloseHandle(stop); CloseHandle(ready); }
    HRESULT Run(DWORD excluded, AudioCallback callback) {
        auto activation = Make<Activation>();
        AUDIOCLIENT_ACTIVATION_PARAMS parameters{};
        parameters.ActivationType = AUDIOCLIENT_ACTIVATION_TYPE_PROCESS_LOOPBACK;
        parameters.ProcessLoopbackParams.TargetProcessId = excluded;
        parameters.ProcessLoopbackParams.ProcessLoopbackMode = PROCESS_LOOPBACK_MODE_EXCLUDE_TARGET_PROCESS_TREE;
        PROPVARIANT property{};
        property.vt = VT_BLOB;
        property.blob.cbSize = sizeof(parameters);
        property.blob.pBlobData = reinterpret_cast<BYTE*>(&parameters);
        ComPtr<IActivateAudioInterfaceAsyncOperation> operation;
        HRESULT hr = ActivateAudioInterfaceAsync(VIRTUAL_AUDIO_DEVICE_PROCESS_LOOPBACK, __uuidof(IAudioClient), &property, activation.Get(), &operation);
        if (FAILED(hr)) return hr;
        HANDLE waiting[] = {stop, activation->ready};
        DWORD wait = WaitForMultipleObjects(2, waiting, FALSE, 10000);
        if (wait != WAIT_OBJECT_0 + 1) return HRESULT_FROM_WIN32(wait == WAIT_TIMEOUT ? ERROR_TIMEOUT : ERROR_CANCELLED);
        if (FAILED(activation->result)) return activation->result;
        auto client = activation->client;
        WAVEFORMATEX format{WAVE_FORMAT_PCM, 2, 48000, 192000, 4, 16, 0};
        hr = client->Initialize(AUDCLNT_SHAREMODE_SHARED, AUDCLNT_STREAMFLAGS_LOOPBACK | AUDCLNT_STREAMFLAGS_EVENTCALLBACK | AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM, 0, 0, &format, nullptr);
        if (FAILED(hr)) return hr;
        HANDLE audio = CreateEvent(nullptr, FALSE, FALSE, nullptr);
        if (!audio) return HRESULT_FROM_WIN32(GetLastError());
        struct Close { HANDLE handle; ~Close(){ CloseHandle(handle); } } close{audio};
        ComPtr<IAudioCaptureClient> reader;
        hr = client->GetService(IID_PPV_ARGS(&reader));
        if (FAILED(hr)) return hr;
        hr = client->SetEventHandle(audio);
        if (FAILED(hr)) return hr;
        hr = client->Start();
        if (FAILED(hr)) return hr;
        result = S_OK;
        SetEvent(ready);
        HANDLE signals[] = {stop, audio};
        std::vector<BYTE> silence;
        while (WaitForMultipleObjects(2, signals, FALSE, INFINITE) == WAIT_OBJECT_0 + 1) {
            UINT32 frames = 0;
            while (SUCCEEDED(hr = reader->GetNextPacketSize(&frames)) && frames) {
                BYTE* bytes = nullptr; DWORD flags = 0;
                hr = reader->GetBuffer(&bytes, &frames, &flags, nullptr, nullptr);
                if (FAILED(hr)) break;
                int count = static_cast<int>(frames * format.nBlockAlign);
                if (flags & AUDCLNT_BUFFERFLAGS_SILENT) { silence.assign(count, 0); bytes = silence.data(); }
                callback(bytes, count);
                hr = reader->ReleaseBuffer(frames);
                if (FAILED(hr)) break;
            }
            if (FAILED(hr)) break;
        }
        client->Stop();
        return hr;
    }
};

extern "C" __declspec(dllexport) HRESULT __stdcall AudioStart(DWORD excluded, AudioCallback callback, Capture** output) {
    if (!callback || !output) return E_INVALIDARG;
    *output = nullptr;
    try {
        auto capture = new Capture();
        capture->worker = std::thread([capture, excluded, callback] {
            HRESULT initialized = CoInitializeEx(nullptr, COINIT_MULTITHREADED);
            HRESULT hr = initialized;
            try { if (SUCCEEDED(hr)) hr = capture->Run(excluded, callback); } catch (...) { hr = E_FAIL; }
            capture->result = hr; SetEvent(capture->ready);
            if (SUCCEEDED(initialized)) CoUninitialize();
        });
        DWORD waited = WaitForSingleObject(capture->ready, 12000);
        HRESULT hr = waited == WAIT_OBJECT_0 ? capture->result.load() : HRESULT_FROM_WIN32(ERROR_TIMEOUT);
        if (FAILED(hr)) { delete capture; return hr; }
        *output = capture; return S_OK;
    } catch (...) { return E_OUTOFMEMORY; }
}
extern "C" __declspec(dllexport) HRESULT __stdcall AudioStatus(Capture* capture) { return capture ? capture->result.load() : E_POINTER; }
extern "C" __declspec(dllexport) void __stdcall AudioStop(Capture* capture) { delete capture; }
