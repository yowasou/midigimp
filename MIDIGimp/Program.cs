using NAudio.Midi;
using WindowsInput;
using WindowsInput.Native;

namespace MIDIGimp
{
    class Program
    {
        static bool messageWait = false;
        static void Main()
        {
            using var gimpClient = new GimpClient();
            var inDevice = new MidiIn(0);
            var sim = new InputSimulator();
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
                            sim.Keyboard.KeyPress(VirtualKeyCode.VK_P);
                            float brushSize = (float)((cc.ControllerValue + 1) * 2);
                            gimpClient.SendCommand($"(gimp-context-set-brush-size {brushSize})\n");
                        }

                        if (cnum == 7)
                        {
                            int size = cc.ControllerValue * 4 + 1;
                            string script = $@"
                (let* (
                      (img (car (gimp-image-list)))
                      (layer (car (gimp-image-get-active-layer img)))
                    )
                    (gimp-layer-scale layer {size} {size} INTERPOLATION-CUBIC)
                )
                ";

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