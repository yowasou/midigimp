using System;
using System.Net.Sockets;
using System.Text;
using NAudio.Midi;

class Program
{
    static void Main()
    {
        var inDevice = new MidiIn(0); // デバイス番号は環境に合わせる
        inDevice.MessageReceived += (s, e) => {
            if (e.MidiEvent is ControlChangeEvent cc)
            {
                // MIDI値(1~127)をブラシサイズに変換
                Console.WriteLine(string.Format("CC:{0} Value:{1}", 
                    ((byte)cc.Controller), cc.ControllerValue));
                float brushSize = (float)((cc.ControllerValue + 1) * 2);
                //sendtest();
                SendToGimp(string.Format("(gimp-context-set-brush-size {0})\n", brushSize));
            }
        };
        inDevice.Start();
        Console.ReadLine();
    }

    static void SendToGimp(string command)
    {
        string host = "127.0.0.1"; // GIMPが動作しているホスト
        int port = 10008;           // Script-Fuサーバーのポート

        try
        {
            using (TcpClient client = new TcpClient(host, port))
            using (NetworkStream stream = client.GetStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Schemeコード
                string schemeCode = command;
                byte[] schemeBytes = Encoding.ASCII.GetBytes(schemeCode);
                int length = schemeBytes.Length;

                // ヘッダーの送信（Magic byte + 長さ）
                writer.Write((byte)0x47); // Magic byte 'G'
                writer.Write((byte)(length / 256)); // 長さの高位バイト
                writer.Write((byte)(length % 256)); // 長さの低位バイト

                // Schemeコードの送信
                writer.Write(schemeBytes);
                writer.Flush();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("エラー: " + ex.Message);
        }
    }
}