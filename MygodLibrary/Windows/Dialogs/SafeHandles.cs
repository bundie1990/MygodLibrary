// Copyright ?Sven Groot (Ookii.org) 2009
// BSD license; see license.txt for details.

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Mygod.Windows.Dialogs
{
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    internal class SafeModuleHandle : SafeHandle
    {
        public SafeModuleHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid { get { return handle == IntPtr.Zero; } }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            return NativeMethods.FreeLibrary(handle);
        }
    }
}