using NAudio.Midi;

namespace MIDIGimp
{
    class Program
    {
        static bool messageWait = false;
        static void Main()
        {
            using var gimpClient = new GimpClient();
            var inDevice = new MidiIn(0);
            inDevice.MessageReceived += (s, e) =>
            {
                lock (gimpClient)
                {
                    if (e.MidiEvent is ControlChangeEvent cc && !messageWait)
                    {
                        messageWait = true;
                        byte cnum = ((byte)cc.Controller);
                        Console.WriteLine($"CC:{cnum} Value:{cc.ControllerValue}");

                        if (cnum == 6)
                        {
                            float brushSize = (float)((cc.ControllerValue + 1) * 2);
                            gimpClient.SendCommand($"(gimp-context-set-brush-size {brushSize})\n");
                        }

                        if (cnum == 7)
                        {
                            float bairitsu = MIDIFunc.MapValue(cc.ControllerValue);
                            string script = $@"
                (let* (
                      (img (car (gimp-image-list)))
                      (layer (car (gimp-image-get-active-layer img)))
                      (width (car (gimp-drawable-width layer)))
                      (height (car (gimp-drawable-height layer)))
                    )
                    gimp-message (number->string (* height 2))
                )
                ";
                            //(new-width(*1 width))
                            //(new-height(*1 height))
                            //(gimp-message number->string new-width)
                            //(gimp-layer-scale layer new-width new-height INTERPOLATION-CUBIC)
                            gimpClient.SendCommand(script.Replace("\r\n", "\n") + "\n");
                        }

                        messageWait = false;
                    }
                }
            };
            inDevice.Start();
            Console.ReadLine();
        }
    }
}