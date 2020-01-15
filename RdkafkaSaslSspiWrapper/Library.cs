using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace RdkafkaSaslSspiWrapper
{
    public sealed class Library
    {
        private static readonly Lazy<Library> lazy =
            new Lazy<Library>(() => new Library());

        public static Library Instance { get { return lazy.Value; } }

        private Library()
        {
        }

        public Client CreateClient(ClientParams clientParams)
        {
            const int inBufSize = 4096;
            const int outBufSize = 4096;
            const int errBufSize = 4096;
            IntPtr inBuf = Marshal.AllocHGlobal(inBufSize +
                                                outBufSize +
                                                errBufSize);
            IntPtr outBuf = IntPtr.Add(inBuf, inBufSize);
            IntPtr errBuf = IntPtr.Add(outBuf, outBufSize);
            UIntPtr errBufSizeT = new UIntPtr(errBufSize);

            Action<int, string, string> logAction = clientParams.Log;
            Delegate logDelegate = null;
            IntPtr logCb = IntPtr.Zero;

            if (logAction != null)
            {
                Native.rd_kafka_log_cb_t d = new Native.rd_kafka_log_cb_t
                    ((IntPtr rk, int level, string fac, string buf) =>
                    {
                        logAction(level, fac, buf);
                    });

                logDelegate = d;
                logCb = Marshal.GetFunctionPointerForDelegate(d);
            }

            IntPtr rktrans = Native.rd_kafka_sasl_wrapper_new
                (clientParams.Mechanisms,
                 clientParams.Service,
                 logCb, errBuf, errBufSizeT);

            if (rktrans == null)
            {
                CheckReturn(-1, "rd_kafka_sasl_wrapper_new", errBuf);
            }

            UIntPtr initialPacketSize = new UIntPtr(outBufSize);

            string sspiName = clientParams.Hostname;
            if (!string.IsNullOrEmpty(clientParams.Realm))
            {
                sspiName += "@" + clientParams.Realm;
            }

            int r = Native.rd_kafka_sasl_client_new(rktrans,
                                                    sspiName,
                                                    outBuf,
                                                    ref initialPacketSize,
                                                    errBuf, errBufSizeT);

            CheckReturn(r, "rd_kafka_sasl_client_new", errBuf);

            return new Client(rktrans,
                              inBuf, new UIntPtr(inBufSize),
                              outBuf, new UIntPtr(outBufSize),
                              initialPacketSize,
                              errBuf, errBufSizeT,
                              logDelegate);
        }

        internal static void CheckReturn(int r, string function, IntPtr errBuf)
        {
            if (r < 0)
            {
                int len = Strlen(errBuf);
                string message = Marshal.PtrToStringAnsi(errBuf, len);

                throw new Exception(string.Format("{0} error: {1} ({2})",
                                                  function, message, r));
            }
        }

        private static int Strlen(IntPtr ptr)
        {
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0)
            {
                len++;
            }
            return len;
        }
    }
}
