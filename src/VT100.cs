/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TelnetGames
{
    class VT100
    {
        public enum ColorEnum
        {
            Black,
            Red,
            Green,
            Yellow,
            Blue,
            Magneta,
            Cyan,
            White
        }

        public enum Direction
        {
            Horizontal,
            Vertical
        }

        public class ColorClass
        {
            public bool Bright;
            public ColorEnum Color;
        }

        public enum ClearMode
        {
            CursorToEnd,
            BeginningToCursor,
            Entire
        }

        public enum FlushReturnState
        {
            Success,
            Error,
            Timeout
        }

        private Socket socket;
        private MemoryStream stream;
        private TcpClient tcpClient;
        private bool isEscapeCode = false;

        public VT100(TcpClient tcpClient)
        {
            this.socket = tcpClient.Client;
            this.tcpClient = tcpClient;
            socket.NoDelay = true;
            socket.Blocking = false;
            socket.SendTimeout = 10;
            stream = new MemoryStream();
            stream.Write(new byte[6] { 0xFF, 0xFB, 0x01, 0xFF, 0xFB, 0x03 }, 0, 6);
        }

        public void Close()
        {
            stream.Dispose();
            socket.Close();
            tcpClient.Close();
        }

        public FlushReturnState Flush()
        {
            try
            {
                stream.Seek(0, SeekOrigin.Begin);
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                socket.Send(buffer);
                stream.SetLength(0);
                return FlushReturnState.Success;
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10060)
                    return FlushReturnState.Timeout;
                else
                    return FlushReturnState.Error;
            }
        }

        public void DrawLine(int x, int y, Direction direction, int length)
        {
            if (direction == Direction.Horizontal)
            {
				SetCursor(x, y);
                for (int i = 0; i < length; i++)
                    stream.Write(new byte[1] { 32 }, 0, 1);
            }
            if (direction == Direction.Vertical)
            {
                for (int i = 0; i < length; i++)
                {
					SetCursor (x, y + i);
                    stream.Write(new byte[1] { 32 }, 0, 1);
                }
            }
        }

        public void CursorDown()
        {
            byte[] buffer = new byte[3] { 27, 91, 66 };
            stream.Write(buffer, 0, buffer.Length);
        }

        public void SetBackgroundColor(ColorClass color)
        {
            byte[] buffer;
            if (color.Bright)
                buffer = new byte[6] { 27, 91, 49, 48, (byte)(48 + color.Color), 109 };
            else
                buffer = new byte[5] { 27, 91, 52, (byte)(48 + color.Color), 109 };
            stream.Write(buffer, 0, buffer.Length);
        }

        public void SetForegroundColor(ColorClass color)
        {
            byte[] buffer;
            buffer = new byte[5] { 27, 91, 51, (byte)(48 + color.Color), 109 };
            if (color.Bright)
                buffer[2] = 57;
            stream.Write(buffer, 0, buffer.Length);
        }

        public void SetCursor(int x, int y)
        {
            byte[] xBytes = Encoding.ASCII.GetBytes((y + 1).ToString() + ";");
            byte[] yBytes = Encoding.ASCII.GetBytes((x + 1).ToString());
            byte[] buffer = new byte[3 + xBytes.Length + yBytes.Length];
            buffer[0] = 27;
            buffer[1] = 91;
            Array.Copy(xBytes, 0, buffer, 2, xBytes.Length);
            Array.Copy(yBytes, 0, buffer, 2 + xBytes.Length, yBytes.Length);
            buffer[2 + xBytes.Length + yBytes.Length] = 72;
            stream.Write(buffer, 0, buffer.Length);
        }

        public void SetCursorVisiblity(bool visiblity)
        {
            byte[] buffer = new byte[6] { 27, 91, 63, 50, 53, 104 };
            if (!visiblity)
                buffer[5] = 108;
            stream.Write(buffer, 0, buffer.Length);
        }

		public void Reset()
		{
			byte[] buffer = new byte[2] { 27, 99 };
			stream.Write (buffer, 0, buffer.Length);
		}

        public void Bell()
        {
            stream.Write(new byte[1] { 7 }, 0, 1);
        }

        public void ClearScreen()
        {
            ClearScreen(ClearMode.Entire);
        }

        public void ClearScreen(ClearMode clearMode)
        {
            int clearCode = 48 + (int)clearMode;
            byte[] buffer = new byte[4] { 27, 91, (byte)clearCode, 74 };
            stream.Write(buffer, 0, buffer.Length);
        }

        public void ClearLine()
        {
            ClearLine(ClearMode.Entire);
        }

        public void ClearLine(ClearMode clearMode)
        {
            int clearCode = 48 + (int)clearMode;
            byte[] buffer = new byte[4] { 27, 91, (byte)clearCode, 75 };
            stream.Write(buffer, 0, buffer.Length);
        }

        public void WriteText(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            stream.Write(buffer, 0, buffer.Length);
        }

        public char? ReadChar()
        {
            while (true)
            {
                if (socket.Available == 0)
                    return null;
                byte[] buffer = new byte[1];
                socket.Receive(buffer);
                if (isEscapeCode && ((buffer[0] > 64 && buffer[0] < 91) || (buffer[0] > 96 && buffer[0] < 123)))
                    isEscapeCode = false;
                else if (buffer[0] == 27)
                    isEscapeCode = true;
                else if (isEscapeCode == false)
                    return (char?)buffer[0];
            }
        }
    }
}
