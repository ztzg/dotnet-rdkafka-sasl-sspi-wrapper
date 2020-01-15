using System;
using System.Runtime.InteropServices;

namespace RdkafkaSaslSspiWrapper
{
    internal class Native
    {
        const string NATIVE_DLL = "rdkafka_sasl_sspi";

#pragma warning disable IDE1006

        [DllImport(NATIVE_DLL)]
        internal static extern IntPtr
            rd_kafka_sasl_wrapper_new(string mechanisms,
                                      string service_name,
                                      IntPtr log_cb,
                                      IntPtr errstr,
                                      UIntPtr errstr_size);

        [DllImport(NATIVE_DLL)]
        internal static extern int
            rd_kafka_sasl_client_new(IntPtr rktrans,
                                     string hostname,
                                     IntPtr clientout,
                                     ref UIntPtr clientout_size,
                                     IntPtr errstr,
                                     UIntPtr errstr_size);

        [DllImport(NATIVE_DLL)]
        internal static extern int
            rd_kafka_sasl_step(IntPtr rktrans,
                               IntPtr serverin,
                               UIntPtr serverin_size,
                               IntPtr clientout,
                               ref UIntPtr clientout_size,
                               IntPtr errstr,
                               UIntPtr errstr_size);

        [DllImport(NATIVE_DLL)]
        internal static extern void
            rd_kafka_sasl_close(IntPtr rktrans);

        [DllImport(NATIVE_DLL)]
        internal static extern void
            rd_kafka_sasl_wrapper_free(IntPtr rktrans);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void rd_kafka_log_cb_t(IntPtr rk,
                                                 int level,
                                                 string fac,
                                                 string buf);

#pragma warning restore IDE1006
    }
}
