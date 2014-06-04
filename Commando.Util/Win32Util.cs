using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace twomindseye.Commando.Util
{
    public static class Win32Util
    {
        public static bool GetFileInfo(string strPath, bool bSmallIcon, out Icon icon, out string displayName)
        {
            Win32.SHFILEINFO info = new Win32.SHFILEINFO();

            var cbFileInfo = Marshal.SizeOf(info);

            var flags = Win32.SHGFI.Icon | Win32.SHGFI.UseFileAttributes | Win32.SHGFI.DisplayName;

            if (bSmallIcon)
            {
                flags |= Win32.SHGFI.SmallIcon;
            }
            else
            {
                flags |= Win32.SHGFI.LargeIcon;
            }

            icon = null;
            displayName = null;

            if (Win32.SHGetFileInfo(strPath, 256, out info, (uint)cbFileInfo, flags) != 0)
            {
                if (info.hIcon != IntPtr.Zero)
                {
                    icon = (Icon) Icon.FromHandle(info.hIcon).Clone();
                    Win32.DestroyIcon(info.hIcon);
                }

                displayName = info.szDisplayName;

                return true;
            }

            return false;
        }
    }
}
