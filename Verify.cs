using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace frm_avast_uninstaller
{
    internal static class Verify
    {
        internal static string Sha256(string path)
        {
            try
            {
                using (var sha = SHA256.Create())
                using (var fs = System.IO.File.OpenRead(path))
                    return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "");
            }
            catch { return "(hash failed)"; }
        }

        internal static string Sha256OfBytes(byte[] data)
        {
            try
            {
                using (var sha = SHA256.Create())
                    return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "");
            }
            catch { return "(hash failed)"; }
        }

        internal static bool TryGetSigner(string path, out string signer)
        {
            signer = "(none)";
            try
            {
                using (var cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(path)))
                {
                    signer = cert.Subject;
                    return true;
                }
            }
            catch { return false; }
        }

        internal static bool IsAvastSigner(string signer)
        {
            if (signer == null) return false;
            return signer.IndexOf("Avast", StringComparison.OrdinalIgnoreCase) >= 0
                || signer.IndexOf("NortonLifeLock", StringComparison.OrdinalIgnoreCase) >= 0
                || signer.IndexOf("Gen Digital", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static uint WinVerifyCode(string path)
        {
            Guid action = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

            var fileInfo = new WINTRUST_FILE_INFO
            {
                cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)),
                pcwszFilePath = Marshal.StringToCoTaskMemUni(path),
                hFile = IntPtr.Zero,
                pgKnownSubject = IntPtr.Zero,
            };

            IntPtr pFile = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)));
            IntPtr pData = IntPtr.Zero;
            try
            {
                Marshal.StructureToPtr(fileInfo, pFile, false);

                var data = new WINTRUST_DATA
                {
                    cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_DATA)),
                    dwUIChoice = WTD_UI_NONE,
                    fdwRevocationChecks = WTD_REVOKE_NONE,
                    dwUnionChoice = WTD_CHOICE_FILE,
                    pFile = pFile,
                    dwStateAction = WTD_STATEACTION_VERIFY,
                    dwProvFlags = WTD_SAFER_FLAG,
                };

                pData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINTRUST_DATA)));
                Marshal.StructureToPtr(data, pData, false);

                uint result = WinVerifyTrust(IntPtr.Zero, ref action, pData);

                data.dwStateAction = WTD_STATEACTION_CLOSE;
                Marshal.StructureToPtr(data, pData, false);
                WinVerifyTrust(IntPtr.Zero, ref action, pData);

                return result;
            }
            catch { return 0x80004005; }
            finally
            {
                if (fileInfo.pcwszFilePath != IntPtr.Zero) Marshal.FreeCoTaskMem(fileInfo.pcwszFilePath);
                if (pFile != IntPtr.Zero) Marshal.FreeHGlobal(pFile);
                if (pData != IntPtr.Zero) Marshal.FreeHGlobal(pData);
            }
        }

        private const uint WTD_UI_NONE = 2;
        private const uint WTD_REVOKE_NONE = 0;
        private const uint WTD_CHOICE_FILE = 1;
        private const uint WTD_STATEACTION_VERIFY = 1;
        private const uint WTD_STATEACTION_CLOSE = 2;
        private const uint WTD_SAFER_FLAG = 0x100;

        [StructLayout(LayoutKind.Sequential)]
        private struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;
            public IntPtr pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINTRUST_DATA
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public IntPtr pFile;
            public uint dwStateAction;
            public IntPtr hWVTStateData;
            public IntPtr pwszURLReference;
            public uint dwProvFlags;
            public uint dwUIContext;
            public IntPtr pSignatureSettings;
        }

        [DllImport("wintrust.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern uint WinVerifyTrust(IntPtr hwnd, ref Guid pgActionID, IntPtr pWVTData);
    }
}
