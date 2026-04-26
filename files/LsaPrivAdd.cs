using System;
using System.Runtime.InteropServices;
public class LsaPrivAdd {
    [DllImport("advapi32")] static extern uint LsaOpenPolicy(ref LSA_US s, ref LSA_OA a, uint acc, out IntPtr ph);
    [DllImport("advapi32")] static extern uint LsaAddAccountRights(IntPtr ph, IntPtr sid, LSA_US[] r, uint cnt);
    [DllImport("advapi32")] static extern uint LsaClose(IntPtr h);
    [StructLayout(LayoutKind.Sequential)] public struct LSA_US { public ushort L,M; public IntPtr B; }
    [StructLayout(LayoutKind.Sequential)] public struct LSA_OA { public int Len; public IntPtr RD; public LSA_US ON; public uint A; public IntPtr SD,SQ; }
    public static uint Add(System.Security.Principal.SecurityIdentifier sid, string right) {
        byte[] b = new byte[sid.BinaryLength]; sid.GetBinaryForm(b,0);
        IntPtr sp = Marshal.AllocHGlobal(b.Length); Marshal.Copy(b,0,sp,b.Length);
        var oa = new LSA_OA{Len=Marshal.SizeOf(typeof(LSA_OA))}; var sn = new LSA_US();
        IntPtr ph; LsaOpenPolicy(ref sn, ref oa, 0x800|0x400, out ph);
        var rs = System.Text.Encoding.Unicode.GetBytes(right);
        IntPtr rp = Marshal.AllocHGlobal(rs.Length); Marshal.Copy(rs,0,rp,rs.Length);
        var r = new LSA_US[]{new LSA_US{L=(ushort)rs.Length,M=(ushort)(rs.Length+2),B=rp}};
        uint ret = LsaAddAccountRights(ph, sp, r, 1);
        LsaClose(ph); Marshal.FreeHGlobal(sp); Marshal.FreeHGlobal(rp);
        return ret;
    }
}
