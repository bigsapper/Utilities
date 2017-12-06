using System;
using System.Runtime.InteropServices;

namespace Framework
{
	public class Win32SDK
	{
		[DllImport("kernel32.dll", PreserveSig = true, CharSet = CharSet.Auto)]
		public static extern IntPtr CreateFile(
			string strFileName,
			int dwDesiredAccess,
			uint dwShareMode,
			IntPtr lpSecurityAttributes,
			int dwCreationDisposition,
			int dwFlagsAndAttributes,
			IntPtr hTemplateFile
		);

		[DllImport("kernel32.dll", PreserveSig = true, CharSet = CharSet.Auto)]
		public static extern int GetDiskFreeSpaceEx(
		   IntPtr lpDirectoryName,                 // directory name
		   out long lpFreeBytesAvailable,    // bytes available to caller
		   out long lpTotalNumberOfBytes,    // bytes on disk
		   out long lpTotalNumberOfFreeBytes // free bytes on disk
		);
	}
}