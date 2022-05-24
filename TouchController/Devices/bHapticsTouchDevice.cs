
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bhaptics.Tact; // import define name space

namespace TouchController.Devices
{
    public class bHapticsTouchDevice : ITouchDevice
    {
        private bool _connected;
        private HapticPlayer hapticPlayer;

        public bool IsConnected() => _connected;

        public void TryToConnect()
        {
            // Create instance of HapticPlayer
            hapticPlayer = new HapticPlayer("mediatedTouch", "Mediated touch", (connected) =>
            {
                _connected = connected;
            });
        }

        public void UpdateTouchPoints(TouchPoints touchPoints)
        {
            if (touchPoints == null) return;
            if (!_connected) return;

            foreach (var p in touchPoints.Points)
            {
                var pathPoint = new PathPoint(
                    p.X / touchPoints.Width,
                    p.Y / touchPoints.Height,
                    touchPoints.Intensity);

                hapticPlayer.Submit("Point", PositionType.ForearmR, pathPoint, 80);
            }
        }
    }
}
