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
                        if (cnum == 26 && cc.ControllerValue == 127)
                        {
                            sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.SHIFT, VirtualKeyCode.VK_R);
                        }
                        if (cnum == 7)
                        {
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