using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;

namespace twomindseye.Commando.Engine.Load
{
    static class GAC
    {
        static readonly ConcurrentDictionary<string, bool> s_assemblyStatus;
        static readonly string[] s_procStringAppend;
        static readonly IAssemblyCache s_assemblyCache;

        [DllImport("fusion.dll")]
        internal static extern int CreateAssemblyCache(
            out IAssemblyCache ppAsmCache,
            int reserved);

        [StructLayout(LayoutKind.Sequential)]
        internal struct AssemblyInfo
        {
            public int cbAssemblyInfo;
            public int assemblyFlags;
            public long assemblySizeInKB;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String currentAssemblyPath;
            public int cchBuf;
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
        internal interface IAssemblyCache
        {
            [PreserveSig]
            int UninstallAssembly(
                uint dwFlags,
                [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName,
                [MarshalAs(UnmanagedType.LPArray)] byte[] pRefData,
                out uint pulDisposition);

            [PreserveSig]
            int QueryAssemblyInfo(int flags,
                [MarshalAs(UnmanagedType.LPWStr)] String assemblyName,
                ref AssemblyInfo assemblyInfo);

            [PreserveSig]
            int CreateAssemblyCacheItem(
                uint dwFlags,
                IntPtr pvReserved,
                out object ppAsmItem,
                [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName);

            [PreserveSig]
            int CreateAssemblyScavenger(
                [MarshalAs(UnmanagedType.IUnknown)] out object ppAsmScavenger);

            [PreserveSig]
            int InstallAssembly(
                uint dwFlags,
                [MarshalAs(UnmanagedType.LPWStr)] string pszManifestFilePath,
                [MarshalAs(UnmanagedType.LPArray)] byte[] pRefData);
        }

        static GAC()
        {
            s_assemblyStatus = new ConcurrentDictionary<string, bool>();
            s_procStringAppend = new[]
                                 {
                                     (Environment.Is64BitProcess ? "AMD64" : "X86"),
                                     "MSIL"
                                 };
            CreateAssemblyCache(out s_assemblyCache, 0);
        }

        public static bool IsInGAC(AssemblyName name)
        {
            bool status;

            if (s_assemblyStatus.TryGetValue(name.ToString(), out status))
            {
                return status;
            }

            foreach (var append in s_procStringAppend)
            {
                var str = name + ", processorArchitecture=" + append;

                var aInfo = new AssemblyInfo();
                aInfo.cbAssemblyInfo = Marshal.SizeOf(aInfo);
                aInfo.cchBuf = 2000;
                aInfo.currentAssemblyPath = new string('\0', aInfo.cchBuf);
                status = s_assemblyCache.QueryAssemblyInfo(0, str, ref aInfo) >= 0;

                if (status)
                {
                    break;
                }
            }

            s_assemblyStatus[name.ToString()] = status;

            return status;
        }
    }
}