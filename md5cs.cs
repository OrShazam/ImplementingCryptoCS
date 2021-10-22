using System;
using System.IO;
using System.Text;
namespace MD5CS
{
    class MD5
    {
        private int[] s = new int[64]; private uint[] K = new uint[64];
        private uint a0, b0, c0, d0;
        public MD5() {
            a0 = 0x67452301; b0 = 0xefcdab89; c0 = 0x98badcfe; d0 = 0x10325476;
            InitK(); InitS();
        }
        public byte[] ComputeHash(string text)
        { 
            // get the text bytes and add padding
            int length = text.Length;
            byte[] textBytes = Encoding.ASCII.GetBytes(text);
            byte[] padding = AddPadding(length);
            Array.Resize(ref textBytes, textBytes.Length + padding.Length);
            padding.CopyTo(textBytes, length);
            //
            uint[] M = new uint[16];
            byte[] temp = new byte[4];
            uint A, B, C, D, tempA;
            for (int i = 0; i < textBytes.Length / 64; i += 64)
            {
                A = a0; B = b0; C = c0; D = d0;
                // set up current M
                for (int j = 0; j < 16; j++)
                {
                    Array.Copy(textBytes, i + j * 4, temp, 0, 4);
                    Array.Reverse(temp); // little endian
                    M[j] = BitConverter.ToUInt32(temp, 0);
                }
                for (int j = 0; j < 64; j++)
                {
                    A = addMod32(addMod32(addMod32(A, F(B, C, D, j)),M[g(j)]),K[j]);
                    A <<= s[j]; A = addMod32(A, B);
                    tempA = A; A = D; D = C; C = B; B = tempA;
                }
                a0 = addMod32(a0, A);  b0 = addMod32(b0, B); c0 = addMod32(c0, C); d0 = addMod32(d0, D);
            }
            byte[] result = new byte[16];
            byte[] a = BitConverter.GetBytes(a0); Array.Reverse(a); Array.Copy(a, 0, result, 0, 4);//
            byte[] b = BitConverter.GetBytes(b0); Array.Reverse(b); Array.Copy(b, 0, result, 4, 4);//
            byte[] c = BitConverter.GetBytes(c0); Array.Reverse(c); Array.Copy(c, 0, result, 8, 4);//
            byte[] d = BitConverter.GetBytes(d0); Array.Reverse(d); Array.Copy(d, 0, result, 12, 4);// little endian
            return result;
        }
        private uint F(uint B, uint C, uint D, int round)
        {
               if (round < 16)
                    return (B & C) | ((~B) & D);
               if (round < 32)
                    return (D & B) | ((~D) & C);
               if (round < 48)
                    return B ^ C ^ D;
               else 
                    return C ^ (B | (~D));
        }
        private int g(int i)
        {
            if (i < 16)
                return i;
            if (i < 32)
                return (5 * i + 1) % 16;
            if (i < 48)
                return (3 * i + 5) % 16;
            else 
                return (7 * i) % 16;
        }
        private byte[] AddPadding(int length)
        {
            int paddingInBytes = (512 - ((length * 8) % 512)) / 8;
            byte[] padding = new byte[paddingInBytes];
            byte[] lengthBytes = BitConverter.GetBytes((Int64)length);
            Array.Reverse(lengthBytes); // little endian
            if (padding.Length <= 8)
            {
                for (int i = 0; i < padding.Length; i++)
                {
                    padding[i] = lengthBytes[i];
                }
            }
            else
            {
                int i = padding.Length - 8;
                for (int j = 0; j < 8; j++) { padding[i + j] = lengthBytes[j]; }
                padding[0] = 0x01;
                for (int j = 1; j < i; j++)
                {
                    padding[j] = 0x00;
                }
            }
            return padding;
        }
        private void InitK()
        {
            long _2_32 = (long)1 << 32;
            for (int i = 0; i < 64; i++)
            {
                K[i] = (uint)Math.Floor(_2_32 * Math.Abs(Math.Sin(i + 1)));
            }
        }
        private void InitS()
        {
            int[] _0_15 = new int[] { 7, 12, 17, 22 }, _16_31 = new int[] { 5, 9, 14, 20 },
                _32_47 = new int[] { 4, 11, 16, 23 }, _48_63 = new int[] { 6, 10, 15, 21 };
            for (int i = 0; i < 64; i++)
            {
                if (i < 16) { s[i] = _0_15[i % 4]; continue; }
                if (i < 32) { s[i] = _16_31[i % 4]; continue; }
                if (i < 48) { s[i] = _32_47[i % 4]; continue; }
                else { s[i] = _48_63[i % 4]; continue; }
            }
        }
        private uint addMod32(uint a, uint b)
        {
            // there's probably a less clumsy way to do this
            if ((uint.MaxValue - a) < b)
            {
                return b - (uint.MaxValue - a);
            }
            else
                return a + b;
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: MD5CS.exe <text/filename>");
                return;
            }
            string text;
            if (File.Exists(args[0])) text = File.ReadAllText(args[0]);
            else text = args[0];
            MD5 md5 = new MD5();
            byte[] hash = md5.ComputeHash(text);
            Console.WriteLine("\n" + BitConverter.ToString(hash));
        }
    }
}