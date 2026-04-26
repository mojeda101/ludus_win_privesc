using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
public class LsaRights {
    [DllImport("advapi32")] static extern uint LsaOpenPolicy(ref LSA_US sn, ref LSA_OA oa, uint acc, out IntPtr ph);
    [DllImport("advapi32")] static extern uint LsaEnumerateAccountRights(IntPtr ph, IntPtr sid, out IntPtr ur, out ulong cnt);
    [DllImport("advapi32")] static extern uint LsaClose(IntPtr h);
    [DllImport("advapi32")] static extern uint LsaFreeMemory(IntPtr b);
    [StructLayout(LayoutKind.Sequential)] public struct LSA_US { public ushort L,M; public IntPtr B; }
    [StructLayout(LayoutKind.Sequential)] public struct LSA_OA { public int Len; public IntPtr RD; public LSA_US ON; public uint A; public IntPtr SD,SQ; }
    public static List<string> Get(System.Security.Principal.SecurityIdentifier sid) {
        var r = new List<string>(); byte[] b = new byte[sid.BinaryLength]; sid.GetBinaryForm(b,0);
        IntPtr sp = Marshal.AllocHGlobal(b.Length); Marshal.Copy(b,0,sp,b.Length);
        var oa = new LSA_OA{Len=Marshal.SizeOf(typeof(LSA_OA))}; var sn = new LSA_US();
        IntPtr ph; LsaOpenPolicy(ref sn, ref oa, 0x800, out ph);
        IntPtr ur; ulong cnt;
        if(LsaEnumerateAccountRights(ph,sp,out ur,out cnt)==0){
            IntPtr p=ur;
            for(ulong i=0;i<cnt;i++){var s=(LSA_US)Marshal.PtrToStructure(p,typeof(LSA_US));r.Add(Marshal.PtrToStringUni(s.B,s.L/2));p=IntPtr.Add(p,Marshal.SizeOf(typeof(LSA_US)));}
            LsaFreeMemory(ur);}
        LsaClose(ph); Marshal.FreeHGlobal(sp); return r;
    }
}
