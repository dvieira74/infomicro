using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Infomicro.Core
{
    public enum MessageType : byte
    {
        Image = 1,
        Command = 2
    }

    public static class NetworkProtocol
    {
        public static async Task SendMessageAsync(Stream stream, MessageType type, byte[] payload)
        {
            var lengthBytes = BitConverter.GetBytes(payload.Length);
            byte[] header = new byte[5];
            header[0] = (byte)type;
            Buffer.BlockCopy(lengthBytes, 0, header, 1, 4);

            await stream.WriteAsync(header, 0, header.Length);
            if (payload.Length > 0)
            {
                await stream.WriteAsync(payload, 0, payload.Length);
            }
        }

        public static async Task<(MessageType type, byte[] payload)> ReceiveMessageAsync(Stream stream, CancellationToken token)
        {
            byte[] header = new byte[5];
            if (!await ReadExactAsync(stream, header, header.Length, token))
                return (0, null);

            MessageType type = (MessageType)header[0];
            int length = BitConverter.ToInt32(header, 1);

            byte[] payload = new byte[length];
            if (length > 0)
            {
                if (!await ReadExactAsync(stream, payload, length, token))
                    return (0, null);
            }

            return (type, payload);
        }

        private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, int length, CancellationToken token)
        {
            int offset = 0;
            while (offset < length)
            {
                int read = await stream.ReadAsync(buffer, offset, length - offset, token);
                if (read == 0) return false;
                offset += read;
            }
            return true;
        }
    }
}
