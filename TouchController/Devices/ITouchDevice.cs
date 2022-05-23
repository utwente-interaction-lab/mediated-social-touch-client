using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TouchController.Devices
{
    public interface ITouchDevice
    {
        bool IsConnected();
        void TryToConnect();
        void UpdateTouchPoints(TouchPoints? touchPoints);
    }
}
