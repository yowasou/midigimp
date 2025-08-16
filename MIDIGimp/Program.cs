using NAudio.Midi;
using System;
using System.Net.Sockets;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
    const string okText = "(OK):";
    static bool messageWait = false;
    static void Main()
    {
        var inDevice = new MidiIn(0); // デバイス番号は環境に合わせる
        inDevice.MessageReceived += (s, e) => {
            if (e.MidiEvent is ControlChangeEvent cc && !messageWait)
            {
                messageWait = true;
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
                        (new-width (inexact->exact (floor (* {bairitsu} width))))
                        (new-height (inexact->exact (floor (* {bairitsu} height))))
                     )
                     (gimp-message (string ""Active Layer ID: "" layer))
                     (gimp-message (string ""Width: "" width "", Height: "" height))
                     (gimp-layer-scale layer new-width new-height INTERPOLATION-CUBIC)
                     (gimp-message (string ""Layer ID: "" layer "" Width: "" width "" Height: "" height))
                    )";
                    script = $@"
                    (let* (
                        (img (car (gimp-image-list))) ; 最初の画像を取得
                        (layer (car (gimp-image-get-active-layer img)))
                        (width (gimp-drawable-width layer))
                        (height (gimp-drawable-height layer))
                        (new-width (inexact->exact (floor ({bairitsu} * width))))
                        (new-height (inexact->exact (floor ({bairitsu} * height))))
                     )
                     gimp-message number->string width

                    )";
                    //script = "gimp-message string \"Active Layer ID: \"";
                    SendToGimp(script.Replace("\r\n","\n") + "\n");
                }
                messageWait = false;
            }
        };
        inDevice.Start();
        Console.ReadLine();
    }

    static int GetWidth()
    {
        return 0;
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
            {
                Console.WriteLine("Script-Fu-Remote - Testclient");

                string cmd = command;
                // 送信データの作成: 'G' + len_hi + len_lo + cmd
                byte[] cmdBytes = Encoding.ASCII.GetBytes(cmd);
                int len = cmdBytes.Length;
                byte[] sendBytes = new byte[len + 3];
                sendBytes[0] = (byte)'G';
                sendBytes[1] = (byte)(len / 256);
                sendBytes[2] = (byte)(len % 256);
                Array.Copy(cmdBytes, 0, sendBytes, 3, len);

                // データ送信
                stream.Write(sendBytes, 0, sendBytes.Length);

                // ヘッダー受信（4バイト）
                byte[] header = new byte[4];
                int read = 0;
                while (read < 4)
                    read += stream.Read(header, read, 4 - read);

                if (header[0] != (byte)'G')
                {
                    Console.WriteLine("Invalid magic: " + Encoding.ASCII.GetString(header));
                }

                int msgLen = header[2] * 256 + header[3];
                bool isErr = header[1] != 0;

                // メッセージ本体受信
                byte[] msgBytes = new byte[msgLen];
                read = 0;
                while (read < msgLen)
                    read += stream.Read(msgBytes, read, msgLen - read);

                string msg = Encoding.ASCII.GetString(msgBytes);
                if (isErr)
                    Console.WriteLine("(ERR):" + msg);
                else
                    Console.WriteLine(okText + msg);
                
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error: " + ex.Message);
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