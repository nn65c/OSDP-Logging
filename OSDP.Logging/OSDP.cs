﻿using System;
using System.IO;
using System.IO.Ports;

namespace OSDP.Logging
{
    class OSDP
    {
        public byte Address { get; }
        public ushort Length { get; }
        public byte Ctrl { get; }
        public byte CmdReply { get; }
        public byte[] Data { get; }
        public byte Checksum { get; }
        public byte[] Message { get; }
        public bool ValidMessage { get; } = false;
        public bool PollAck { get; }

        public OSDP(SerialPort sp)
        {
            byte[] header = new byte[5];
            sp.Read(header, 0, 5);

            Length = (ushort)(header[1] + (header[2] << 8));

            if (CheckHeader())
            {
                Message = new byte[Length];
                Message[0] = 0x53;

                Data = new byte[Length - 7];
                sp.Read(Data, 0, Length - 7);

                if (Length > 7)
                {
                    Array.Copy(Data, 0, Message, 6, Length - 7);
                }

                Array.Copy(header, 0, Message, 1, 5);
                Message[Length - 1] = (byte)sp.ReadByte();
                Address = Message[1];
                Ctrl = Message[4];
                CmdReply = Message[5];
                Checksum = Message[Length - 1];

                ValidMessage = (CheckMessage());
                PollAck = (CmdReply == 0x40 || CmdReply == 0x60);
            }

        }

        private bool CheckMessage()
        {
            return Checksum == Checksum2s(Message, true);
        }

        private bool CheckHeader()
        {
            return Length >= 7 && Length <= 1440;
        }

        private static byte Checksum2s(byte[] bytes, bool skipLast = false)
        {
            int calcSum = 0;
            int bytesLength = bytes.Length;

            if (skipLast)
            {
                bytesLength--;
            }

            for (int i = 0; i < bytesLength; i++)
            {
                calcSum += bytes[i];
            }

            calcSum = ~calcSum;
            calcSum++;

            return (byte)calcSum;
        }

        public void ConsoleLog()
        {
            string consoleMessage = Timestamp() + ";" + SerialTool.BytesToHexString(Message);

            if (!ValidMessage)
            {
                Console.WriteLine(consoleMessage);
                Console.Write("- Message not valid. Checksum ERROR");
            }
            else if (PollAck)
            {
                SerialTool.CursorSpin(5);
            }
            else
            {
                Console.WriteLine(consoleMessage);
            }
        }

        public void FileLog(string filename)
        {
            string fileMessage = Timestamp() + ";" + SerialTool.BytesToHexString(Message);

            if (!ValidMessage)
            {
                fileMessage += ";NotValid";
            }

            if (!PollAck)
            {
                try
                {
                    FileStream fileStream = File.Open(filename, FileMode.Append, FileAccess.Write);
                    StreamWriter fileWriter = new StreamWriter(fileStream);

                    fileWriter.Write(fileMessage);
                    fileWriter.WriteLine();
                    fileWriter.Flush();
                    fileWriter.Close();
                }

                catch (IOException ioe)
                {
                    Console.WriteLine(ioe);
                }
            }
        }

        #region Helpers
        private static string Timestamp()
        {
            return DateTime.Now.ToString("dd.MM.yy HH:mm:ss");
        }
        #endregion
    }
}
