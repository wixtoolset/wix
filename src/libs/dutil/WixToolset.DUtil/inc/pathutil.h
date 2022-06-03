#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

typedef enum _PATH_CANONICALIZE
{
    // Always prefix fully qualified paths with the extended path prefix (\\?\).
    PATH_CANONICALIZE_APPEND_EXTENDED_PATH_PREFIX = 0x0001,
    // Always terminate the path with \.
    PATH_CANONICALIZE_BACKSLASH_TERMINATE = 0x0002,
    // Don't collapse . or .. in the \\server\share portion of a UNC path.
    PATH_CANONICALIZE_KEEP_UNC_ROOT = 0x0004,
} PATH_CANONICALIZE;

typedef enum _PATH_EXPAND
{
    PATH_EXPAND_ENVIRONMENT = 0x0001,
    PATH_EXPAND_FULLPATH    = 0x0002,
} PATH_EXPAND;

typedef enum _PATH_PREFIX
{
    // Add prefix even if the path is not longer than MAX_PATH.
    PATH_PREFIX_SHORT_PATHS    = 0x0001,
    // Error with E_INVALIDARG if the path is not fully qualified.
    PATH_PREFIX_EXPECT_FULLY_QUALIFIED = 0x0002,
} PATH_PREFIX;


/*******************************************************************
 PathFile -  returns a pointer to the file part of the path.
********************************************************************/
DAPI_(LPWSTR) PathFile(
    __in_z LPCWSTR wzPath
    );

/*******************************************************************
 PathExtension -  returns a pointer to the extension part of the path
                  (including the dot).
********************************************************************/
DAPI_(LPCWSTR) PathExtension(
    __in_z LPCWSTR wzPath
    );

/*******************************************************************
 PathGetDirectory - extracts the directory from a path including the directory separator.
    Calling the function again with the previous result returns the same result.
    Returns S_FALSE if the path only contains a file name.
    For example, C:\a\b -> C:\a\ -> C:\a\
********************************************************************/
DAPI_(HRESULT) PathGetDirectory(
    __in_z LPCWSTR wzPath,
    __out_z LPWSTR *psczDirectory
    );

/*******************************************************************
PathGetParentPath - extracts the parent directory from a path
    ignoring a trailing slash so that when called repeatedly,
    it eventually returns the root portion of the path.
    *psczDirectory is NULL if the path only contains a file name or
    the path only contains the root.
    *pcchRoot is the length of the root part of the path.
    For example, C:\a\b -> C:\a\ -> C:\ -> NULL
********************************************************************/
DAPI_(HRESULT) PathGetParentPath(
    __in_z LPCWSTR wzPath,
    __out_z LPWSTR *psczDirectory,
    __out_opt SIZE_T* pcchRoot
    );

/*******************************************************************
 PathExpand - gets the full path to a file resolving environment
              variables along the way.
********************************************************************/
DAPI_(HRESULT) PathExpand(
    __out LPWSTR *psczFullPath,
    __in_z LPCWSTR wzRelativePath,
    __in DWORD dwResolveFlags
    );

/*******************************************************************
 PathGetFullPathName - wrapper around GetFullPathNameW.
*******************************************************************/
DAPI_(HRESULT) PathGetFullPathName(
    __in_z LPCWSTR wzPath,
    __deref_out_z LPWSTR* psczFullPath,
    __inout_z_opt LPCWSTR* pwzFileName,
    __out_opt SIZE_T* pcch
    );

/*******************************************************************
 PathPrefix - prefixes a path with \\?\ or \\?\UNC if it doesn't
    already have an extended prefix, is longer than MAX_PATH,
    and is fully qualified.
********************************************************************/
DAPI_(HRESULT) PathPrefix(
    __inout_z LPWSTR *psczFullPath,
    __in SIZE_T cchFullPath,
    __in DWORD dwPrefixFlags
    );

/*******************************************************************
 PathFixedNormalizeSlashes - replaces all / with \ and
    removes redundant consecutive slashes.
********************************************************************/
DAPI_(HRESULT) PathFixedNormalizeSlashes(
    __inout_z LPWSTR wzPath
    );

/*******************************************************************
 PathFixedReplaceForwardSlashes - replaces all / with \
********************************************************************/
DAPI_(void) PathFixedReplaceForwardSlashes(
    __inout_z LPWSTR wzPath
    );

/*******************************************************************
 PathFixedBackslashTerminate - appends a \ if path does not have it
                                 already, but fails if the buffer is
                                 insufficient.
********************************************************************/
DAPI_(HRESULT) PathFixedBackslashTerminate(
    __inout_ecount_z(cchPath) LPWSTR wzPath,
    __in SIZE_T cchPath
    );

/*******************************************************************
 PathBackslashTerminate - appends a \ if path does not have it
                                 already.
********************************************************************/
DAPI_(HRESULT) PathBackslashTerminate(
    __inout_z LPWSTR* psczPath
    );

/*******************************************************************
 PathForCurrentProcess - gets the full path to the currently executing
                         process or (optionally) a module inside the process.
********************************************************************/
DAPI_(HRESULT) PathForCurrentProcess(
    __inout LPWSTR *psczFullPath,
    __in_opt HMODULE hModule
    );

/*******************************************************************
 PathRelativeToModule - gets the name of a file in the same 
    directory as the current process or (optionally) a module inside 
    the process
********************************************************************/
DAPI_(HRESULT) PathRelativeToModule(
    __inout LPWSTR *psczFullPath,
    __in_opt LPCWSTR wzFileName,
    __in_opt HMODULE hModule
    );

/*******************************************************************
 PathCreateTempFile

 Note: if wzDirectory is null, ::GetTempPath() will be used instead.
       if wzFileNameTemplate is null, GetTempFileName() will be used instead.
*******************************************************************/
DAPI_(HRESULT) PathCreateTempFile(
    __in_opt LPCWSTR wzDirectory,
    __in_opt __format_string LPCWSTR wzFileNameTemplate,
    __in DWORD dwUniqueCount,
    __in DWORD dwFileAttributes,
    __out_opt LPWSTR* psczTempFile,
    __out_opt HANDLE* phTempFile
    );

/*******************************************************************
 PathCreateTimeBasedTempFile - creates an empty temp file based on current
                           system time
********************************************************************/
DAPI_(HRESULT) PathCreateTimeBasedTempFile(
    __in_z_opt LPCWSTR wzDirectory,
    __in_z LPCWSTR wzPrefix,
    __in_z_opt LPCWSTR wzPostfix,
    __in_z LPCWSTR wzExtension,
    __deref_opt_out_z LPWSTR* psczTempFile,
    __out_opt HANDLE* phTempFile
    );

/*******************************************************************
 PathCreateTempDirectory

 Note: if wzDirectory is null, ::GetTempPath() will be used instead.
*******************************************************************/
DAPI_(HRESULT) PathCreateTempDirectory(
    __in_opt LPCWSTR wzDirectory,
    __in __format_string LPCWSTR wzDirectoryNameTemplate,
    __in DWORD dwUniqueCount,
    __out LPWSTR* psczTempDirectory
    );

/*******************************************************************
 PathGetTempPath - returns the path to the temp folder
    that is backslash terminated.
*******************************************************************/
DAPI_(HRESULT) PathGetTempPath(
    __out_z LPWSTR* psczTempPath
    );

/*******************************************************************
 PathGetSystemTempPaths - returns the paths to system temp folders
    that are backslash terminated with higher preference first.
*******************************************************************/
DAPI_(HRESULT) PathGetSystemTempPaths(
    __inout_z LPWSTR** prgsczSystemTempPaths,
    __inout DWORD* pcSystemTempPaths
    );

/*******************************************************************
 PathGetKnownFolder - returns the path to a well-known shell folder

*******************************************************************/
DAPI_(HRESULT) PathGetKnownFolder(
    __in int csidl,
    __out LPWSTR* psczKnownFolder
    );

/*******************************************************************
 PathSkipPastRoot - returns a pointer to the first character after
    the root portion of the path or NULL if the path has no root.
    For example, the pointer will point to the "a" in "after":
    C:\after, C:after, \after, \\server\share\after,
    \\?\C:\afterroot, \\?\UNC\server\share\after
*******************************************************************/
DAPI_(LPCWSTR) PathSkipPastRoot(
    __in_z LPCWSTR wzPath,
    __out_opt BOOL* pfHasExtendedPrefix,
    __out_opt BOOL* pfFullyQualified,
    __out_opt BOOL* pfUNC
    );

/*******************************************************************
 PathIsFullyQualified - returns true if the path is fully qualified; false otherwise.
    Note that some rooted paths like C:dir are not fully qualified.
    For example, these are all fully qualified: C:\dir, C:/dir, \\server\share, \\?\C:\dir.
    For example, these are not fully qualified: C:dir, C:, \dir, dir, dir\subdir.
*******************************************************************/
DAPI_(BOOL) PathIsFullyQualified(
    __in_z LPCWSTR wzPath
    );

/*******************************************************************
 PathIsRooted - returns true if the path is rooted; false otherwise.
    Note that some rooted paths like C:dir are not fully qualified.
    For example, these are all rooted: C:\dir, C:/dir, C:dir, C:, \dir, \\server\share, \\?\C:\dir.
    For example, these are not rooted: dir, dir\subdir.
*******************************************************************/
DAPI_(BOOL) PathIsRooted(
    __in_z LPCWSTR wzPath
    );

/*******************************************************************
 PathConcat - like .NET's Path.Combine, lets you build up a path
    one piece -- file or directory -- at a time.
*******************************************************************/
DAPI_(HRESULT) PathConcat(
    __in_opt LPCWSTR wzPath1,
    __in_opt LPCWSTR wzPath2,
    __deref_out_z LPWSTR* psczCombined
    );

/*******************************************************************
 PathConcatCch - like .NET's Path.Combine, lets you build up a path
    one piece -- file or directory -- at a time.
*******************************************************************/
DAPI_(HRESULT) PathConcatCch(
    __in_opt LPCWSTR wzPath1,
    __in SIZE_T cchPath1,
    __in_opt LPCWSTR wzPath2,
    __in SIZE_T cchPath2,
    __deref_out_z LPWSTR* psczCombined
    );

/*******************************************************************
 PathConcatRelativeToBase - canonicalizes a relative path before
    concatenating it to the base path to ensure the resulting path
    is inside the base path.
*******************************************************************/
DAPI_(HRESULT) PathConcatRelativeToBase(
    __in LPCWSTR wzBase,
    __in_opt LPCWSTR wzRelative,
    __deref_out_z LPWSTR* psczCombined
    );

/*******************************************************************
 PathCompareCanonicalized - canonicalizes the two paths using PathCanonicalizeForComparison
    which does not resolve relative paths into fully qualified paths.
    The strings are then compared using ::CompareStringW().
*******************************************************************/
DAPI_(HRESULT) PathCompareCanonicalized(
    __in_z LPCWSTR wzPath1,
    __in_z LPCWSTR wzPath2,
    __out BOOL* pfEqual
    );

/*******************************************************************
 PathCompress - sets the compression state on an existing file or 
                directory. A no-op on file systems that don't 
                support compression.
*******************************************************************/
DAPI_(HRESULT) PathCompress(
    __in_z LPCWSTR wzPath
    );

/*******************************************************************
 PathGetHierarchyArray - allocates an array containing,
                in order, every parent directory of the specified path,
                ending with the actual input path
                This function also works with registry subkeys
*******************************************************************/
DAPI_(HRESULT) PathGetHierarchyArray(
    __in_z LPCWSTR wzPath,
    __deref_inout_ecount_opt(*pcPathArray) LPWSTR **prgsczPathArray,
    __inout LPUINT pcPathArray
    );

/*******************************************************************
 PathCanonicalizePath - wrapper around PathCanonicalizeW.
*******************************************************************/
DAPI_(HRESULT) PathCanonicalizePath(
    __in_z LPCWSTR wzPath,
    __deref_out_z LPWSTR* psczCanonicalized
    );

/*******************************************************************
 PathCanonicalizeForComparison - canonicalizes the path based on the given flags.
    . and .. directories are collapsed.
    All / are replaced with \.
    All redundant consecutive slashes are replaced with a single \.
*******************************************************************/
DAPI_(HRESULT) PathCanonicalizeForComparison(
    __in_z LPCWSTR wzPath,
    __in DWORD dwCanonicalizeFlags,
    __deref_out_z LPWSTR* psczCanonicalized
    );

/*******************************************************************
 PathDirectoryContainsPath - checks if wzPath is located inside wzDirectory.
    wzDirectory must be a fully qualified path.
*******************************************************************/
DAPI_(HRESULT) PathDirectoryContainsPath(
    __in_z LPCWSTR wzDirectory,
    __in_z LPCWSTR wzPath
    );

#ifdef __cplusplus
}
#endif
