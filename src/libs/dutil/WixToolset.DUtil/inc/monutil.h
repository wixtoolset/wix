#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

#define ReleaseMon(mh) if (mh) { MonDestroy(mh); }
#define ReleaseNullMon(mh) if (mh) { MonDestroy(mh); mh = NULL; }

typedef void* MON_HANDLE;
typedef const void* C_MON_HANDLE;

// Defined in regutil.h
enum REG_KEY_BITNESS;

extern const int MON_HANDLE_BYTES;

// Note: callbacks must be implemented in a thread-safe manner. They will be called asynchronously by a MonUtil-spawned thread.
// They must also be written to return as soon as possible - they are called from the waiter thread
typedef void (*PFN_MONGENERAL)(
    __in HRESULT hr,
    __in_opt LPVOID pvContext
    );
// This callback is not specific to any wait - it will notify client of any drive status changes, such as removable drive insertion / removal
typedef void (*PFN_MONDRIVESTATUS)(
    __in WCHAR chDrive,
    __in BOOL fArriving,
    __in_opt LPVOID pvContext
    );
// Note if these fire  with a failed result it means an error has occurred with the wait, and so the wait will stay in the list and be retried. When waits start succeeding again,
// MonUtil will notify of changes, because it may have not noticed changes during the interval for which the wait had failed. This behavior can result in false positive notifications,
// so all consumers of MonUtil should be designed with this in mind.
typedef void (*PFN_MONDIRECTORY)(
    __in HRESULT hr,
    __in_z LPCWSTR wzPath,
    __in BOOL fRecursive,
    __in_opt LPVOID pvContext,
    __in_opt LPVOID pvDirectoryContext
    );
typedef void (*PFN_MONREGKEY)(
    __in HRESULT hr,
    __in HKEY hkRoot,
    __in_z LPCWSTR wzSubKey,
    __in REG_KEY_BITNESS kbKeyBitness,
    __in BOOL fRecursive,
    __in_opt LPVOID pvContext,
    __in_opt LPVOID pvRegKeyContext
    );

// Silence period allows you to avoid lots of notifications when a lot of writes are going on in a directory
// MonUtil will wait until the directory has been "silent" for at least dwSilencePeriodInMs milliseconds
// The drawback to setting this to a value higher than zero is that even single write notifications
// are delayed by this amount
HRESULT DAPI MonCreate(
    __out_bcount(MON_HANDLE_BYTES) MON_HANDLE *pHandle,
    __in PFN_MONGENERAL vpfMonGeneral,
    __in_opt PFN_MONDRIVESTATUS vpfMonDriveStatus,
    __in_opt PFN_MONDIRECTORY vpfMonDirectory,
    __in_opt PFN_MONREGKEY vpfMonRegKey,
    __in_opt LPVOID pvContext
    );
// Don't add multiple identical waits! Not only is it wasteful and will cause multiple fires for the exact same change, it will also
// result in slightly odd behavior when you remove a duplicated wait (removing a wait may or may not remove multiple waits)
// This is due to the way coordinator thread and waiter threads handle removing, and while it is possible to solve, doing so would complicate the code.
// So instead, de-dupe your wait requests before sending them to MonUtil.
// Special notes for network waits: MonUtil can send false positive notifications (i.e. notifications when nothing had changed) if connection
// to the share is lost and reconnected, because MonUtil can't know for sure whether changes occurred while the connection was lost.
// Also, MonUtil will very every 20 minutes retry even successful network waits, because the underlying Win32 API cannot notify us if a remote server
// had its network cable unplugged or similar sudden failure. When we retry the successful network waits, we will also send a false positive notification,
// because it's impossible for MonUtil to detect if we're reconnecting to a server that had died and come back to life, or if we're reconnecting to a server that had
// been up all along. For both of the above reasons, clients of MonUtil must be written to do very, very little work in the case of false positive network waits.
HRESULT DAPI MonAddDirectory(
    __in_bcount(MON_HANDLE_BYTES) MON_HANDLE handle,
    __in_z LPCWSTR wzPath,
    __in BOOL fRecursive,
    __in DWORD dwSilencePeriodInMs,
    __in_opt LPVOID pvDirectoryContext
    );
HRESULT DAPI MonAddRegKey(
    __in_bcount(MON_HANDLE_BYTES) MON_HANDLE handle,
    __in HKEY hkRoot,
    __in_z LPCWSTR wzSubKey,
    __in REG_KEY_BITNESS kbKeyBitness,
    __in BOOL fRecursive,
    __in DWORD dwSilencePeriodInMs,
    __in_opt LPVOID pvRegKeyContext
    );
HRESULT DAPI MonRemoveDirectory(
    __in_bcount(MON_HANDLE_BYTES) MON_HANDLE handle,
    __in_z LPCWSTR wzPath,
    __in BOOL fRecursive
    );
HRESULT DAPI MonRemoveRegKey(
    __in_bcount(MON_HANDLE_BYTES) MON_HANDLE handle,
    __in HKEY hkRoot,
    __in_z LPCWSTR wzSubKey,
    __in REG_KEY_BITNESS kbKeyBitness,
    __in BOOL fRecursive
    );
void DAPI MonDestroy(
    __in_bcount(MON_HANDLE_BYTES) MON_HANDLE handle
    );

#ifdef __cplusplus
}
#endif
