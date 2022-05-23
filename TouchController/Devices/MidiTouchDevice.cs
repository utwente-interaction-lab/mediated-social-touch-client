using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TouchController.Devices
{
    internal class MidiTouchDevice : ITouchDevice
    {
        private readonly int[] MidiMapping;
        private int MidiSize => MidiMapping.Length;
        private bool[] MidiStatus;
        private IMidiOutputDevice? outputDevice;

        public MidiTouchDevice(int[] midiMapping)
        {
            MidiMapping = midiMapping;
            MidiStatus = new bool[MidiMapping.Length];
            TryToConnect();
        }

        public void TryToConnect()
        {
            var outputDeviceInfo = MidiDeviceManager.Default.OutputDevices.Last();
            Console.WriteLine($"Opening {outputDeviceInfo.Name}");
            outputDevice = outputDeviceInfo.CreateDevice();
            outputDevice.Open();
        }

        public bool IsConnected() => outputDevice != null && outputDevice.IsOpen;

        public void UpdateTouchPoints(TouchPoints? touchPoints)
        {
            var newMidi = new bool[MidiSize];
            byte intensity = 100;
            if (touchPoints != null)
            {
                foreach (var p in touchPoints.Points)
                {
                    // NEW, remote web data
                    var activeFloat = p.X / touchPoints.Width * MidiSize;
                    var activeIndex = (int)Math.Round(activeFloat) - 1;

                    // todo: maybe add gradient? 
                    if (activeIndex >= 0 && activeIndex < MidiSize)
                        newMidi[activeIndex] = true;
                }
                intensity = touchPoints.Intensity;
            }

            UpdateMidiKeys(newMidi, intensity);
        }

        private void UpdateMidiKeys(bool[] targetMidi, byte intensity)
        {
            var line = "";
            for (int i = 0; i < targetMidi.Length; i++)
            {
                line += targetMidi[i] + ",";
            }

            // Turn off keys that are still on
            for (int i = 0; i < targetMidi.Length; i++)
            {
                if (!targetMidi[i] && MidiStatus[i])
                {
                    var message = new NoteOffMessage(RtMidi.Core.Enums.Channel.Channel1, (RtMidi.Core.Enums.Key)MidiMapping[i], 0);
                    outputDevice?.Send(message);
                }
            }

            // Turn on keys that should be on
            for (int i = 0; i < targetMidi.Length; i++)
            {
                if (targetMidi[i] && !MidiStatus[i])
                {
                    var message = new NoteOnMessage(RtMidi.Core.Enums.Channel.Channel1, (RtMidi.Core.Enums.Key)MidiMapping[i], intensity);
                    outputDevice?.Send(message);
                }
            }

            MidiStatus = targetMidi;
        }
    }
}
