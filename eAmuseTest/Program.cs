﻿using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using eAmuseCore.Crypto;
using eAmuseCore.Compression;
using eAmuseCore.KBinXML;

namespace eAmuseTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // string url = "/L44/8?model=L44:J:E:A:2018070901&f=gametop.get_pdata";
            string compress = "lz77";
            string eamuse_info = "1-5ca39858-b503";
            // string user_agent = "EAMUSE.XRPC/1.0";
            byte[] data = new byte[] {
                0xb6, 0x5d, 0x24, 0x57, 0x42, 0x34, 0xa8, 0x8f, 0xc7, 0x6c, 0x3c, 0x05, 0xe4, 0xa1, 0xf9, 0x14,
                0x8e, 0xd1, 0xd1, 0x6a, 0xc9, 0xf7, 0x95, 0x50, 0xff, 0xe0, 0x12, 0xf0, 0xbc, 0x25, 0x21, 0x73,
                0x9a, 0x90, 0xd4, 0x5c, 0x07, 0xf6, 0x92, 0xd3, 0x72, 0x51, 0x21, 0x2a, 0xa3, 0xd1, 0x0c, 0x19,
                0x43, 0xfb, 0x9c, 0x6d, 0x7d, 0xb8, 0xcd, 0x6d, 0x20, 0x0d, 0x5d, 0xa3, 0xda, 0x91, 0x6d, 0x19,
                0x9a, 0x8d, 0x60, 0x40, 0x14, 0xda, 0x81, 0xa0, 0xcf, 0x60, 0x95, 0x57, 0xc3, 0x06, 0x86, 0x40,
                0x0f, 0xb3, 0x62, 0x4b, 0x6b, 0x71, 0x46, 0x5a, 0x38, 0x8c, 0x36, 0x08, 0x68, 0xc8, 0x60, 0xad,
                0x17, 0x0e, 0x97, 0xec, 0x69, 0xaf, 0x5e, 0x73, 0x5f, 0x55, 0xc0, 0x46, 0xbf, 0x21, 0xf1, 0x1a,
                0xbc, 0xca, 0x44, 0xa0, 0x52, 0xdf, 0x8d, 0xf4, 0x78, 0xa0, 0xe5, 0x2f, 0xa0, 0x68, 0x49, 0xd6,
                0xe0, 0xf7, 0x0c, 0x57, 0x85, 0x23, 0xb5, 0x0e, 0x3a, 0x80, 0x4b, 0x32, 0xe0, 0xaa, 0x2b, 0x50,
                0xcf, 0x19, 0xb8, 0x1b, 0x96, 0xc9, 0x81, 0xa3, 0x49, 0x50, 0x9d, 0xe4, 0x9d, 0xb8, 0x5d, 0xff,
                0xee, 0xbb, 0xf5, 0x58, 0x78, 0x68, 0x7a, 0xbc, 0x3c, 0x79, 0x82, 0x51, 0xd9, 0x26, 0x1e, 0x5d,
                0x26, 0x95, 0x5d, 0x66, 0x1c, 0x87, 0xda, 0x5c, 0x4e, 0x55, 0x9a, 0xaf, 0xa2, 0x5b, 0x4b, 0xde,
                0x38, 0x4c, 0x25, 0x3d, 0xb0, 0x65, 0xc3, 0xad, 0xa7, 0x00, 0xbc, 0x55, 0x48, 0x7b, 0x54, 0x5f,
                0xc5, 0xe9, 0x1c, 0x52, 0x25, 0xb2, 0x1d, 0xd2, 0x76, 0xa9, 0x74, 0xba, 0x94, 0xd4, 0xd6, 0xc1,
                0x20, 0xcf, 0x4c, 0xc2, 0x1d, 0xcf, 0x61, 0x45, 0xf7, 0x4f, 0x32, 0xd6, 0xd6, 0xc7, 0xf0, 0xe1,
                0x4b, 0x66, 0x07, 0xc0, 0xd0, 0xb3
            };

            compress = compress.ToLower();

            var decryptedData = RC4.ApplyEAmuseInfo(eamuse_info, data);
            var rawData = decryptedData;
            if (compress == "lz77")
                rawData = LZ77.Decompress(decryptedData);
            else if (compress != "none")
                throw new ArgumentException("Unsupported compression algorithm");

            Console.WriteLine(BytesToString(rawData));

            KBinXML kbinxml = new KBinXML(rawData);
        }

        private static string BytesToString(IEnumerable<byte> bytes)
        {
            var sb = new StringBuilder();

            foreach (byte b in bytes) {
                char c = (char)b;
                if (c == '\n')
                    sb.Append("\\n");
                else if (c == '\r')
                    sb.Append("\\r");
                else if (c == '\t')
                    sb.Append("\\t");
                else if (b < 128 && (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c)))
                    sb.Append(Convert.ToString(c));
                else
                    sb.Append("\\x" + Convert.ToString(b, 16).PadLeft(2, '0'));
            }

            return sb.ToString();
        }
    }
}
