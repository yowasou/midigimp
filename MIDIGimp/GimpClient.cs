using System;
using System.Net.Sockets;
using System.Text;
namespace MIDIGimp
{
    public class GimpClient : IDisposable
    {
        private readonly string host = "127.0.0.1";
        private readonly int port = 10008;
        private TcpClient client;
        private NetworkStream stream;

        public GimpClient()
        {
            client = new TcpClient(host, port);
            stream = client.GetStream();
        }

        public string SendCommand(string command)
        {
            string okText = "(OK):";
            string ret = string.Empty;

            try
            {
                byte[] cmdBytes = Encoding.ASCII.GetBytes(command);
                int len = cmdBytes.Length;
                byte[] sendBytes = new byte[len + 3];
                sendBytes[0] = (byte)'G';
                sendBytes[1] = (byte)(len / 256);
                sendBytes[2] = (byte)(len % 256);
                Array.Copy(cmdBytes, 0, sendBytes, 3, len);

                stream.Write(sendBytes, 0, sendBytes.Length);

                byte[] header = new byte[4];
                int read = 0;
                while (read < 4)
                    read += stream.Read(header, read, 4 - read);

                if (header[0] != (byte)'G')
                    Console.WriteLine("Invalid magic: " + Encoding.ASCII.GetString(header));

                int msgLen = header[2] * 256 + header[3];
                bool isErr = header[1] != 0;

                byte[] msgBytes = new byte[msgLen];
                read = 0;
                while (read < msgLen)
                    read += stream.Read(msgBytes, read, msgLen - read);

                string msg = Encoding.ASCII.GetString(msgBytes);
                if (isErr)
                    Console.WriteLine("(ERR):" + msg);
                else
                    Console.WriteLine(okText + msg);

                ret = msg;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
            }

            return ret;
        }

        public void Dispose()
        {
            stream?.Close();
            client?.Close();
        }
    }
}