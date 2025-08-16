using NAudio.Midi;
using System;
using System.Net.Sockets;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
    static void Main()
    {
        var inDevice = new MidiIn(0); // デバイス番号は環境に合わせる
        inDevice.MessageReceived += (s, e) => {
            if (e.MidiEvent is ControlChangeEvent cc)
            {
                byte cnum = ((byte)cc.Controller);
                Console.WriteLine(string.Format("CC:{0} Value:{1}", cnum, cc.ControllerValue));
                if (cnum == 6)
                {
                    // MIDI値(1~127)をブラシサイズに変換
                    float brushSize = (float)((cc.ControllerValue + 1) * 2);
                    SendToGimp(string.Format("(gimp-context-set-brush-size {0})\n", brushSize));
                }
                if (cnum == 7)
                {
                    // レイヤーの拡大縮小
                    float bairitsu = MapValue(cc.ControllerValue);
                    string script = $@"
                    (let* (
                        (img (car (gimp-image-list))) ; 最初の画像を取得
                        (layer (car (gimp-image-get-active-layer img)))
                        (width (gimp-drawable-width layer))
                        (height (gimp-drawable-height layer))
                        (new-width (inexact->exact (* {bairitsu} width)))
                        (new-height (inexact->exact (* {bairitsu} height)))
                     )
                     (gimp-message (string ""Active Layer ID: "" layer))
                     (gimp-message (string ""Width: "" width "", Height: "" height))
                     (gimp-layer-scale layer new-width new-height INTERPOLATION-CUBIC)
                     (gimp-message (string ""Layer ID: "" layer "" Width: "" width "" Height: "" height))
                    )";
                    script = "(gimp-message string \"Active Layer ID: \")";
                    SendToGimp(script.Replace("\r\n","\n") + "\n");
                }
            }
        };
        inDevice.Start();
        Console.ReadLine();
    }

    static string SendToGimp(string command)
    {
        string host = "127.0.0.1"; // GIMPが動作しているホスト
        int port = 10008;           // Script-Fuサーバーのポート
        string ret = string.Empty;
        try
        {
            using (TcpClient client = new TcpClient(host, port))
            using (NetworkStream stream = client.GetStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            using (BinaryReader reader = new BinaryReader(stream))
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
                string? rline = reader.ReadString();
                if (rline != null)
                {
                    Console.WriteLine(rline);
                    ret = rline;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("エラー: " + ex.Message);
        }
        return ret;
    }

    static float MapValue(int x)
    {
        float xMin = 0;
        float xMax = 127;
        float yMin = 0.5f;
        float yMax = 2.0f;

        float y = yMin + ((x - xMin) * (yMax - yMin)) / (xMax - xMin);
        return y;
    }
}