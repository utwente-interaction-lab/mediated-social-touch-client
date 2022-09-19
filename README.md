# Social Mediated Touch Client

This C# application controls a haptic sleeve based on input commands sent by the [web application](https://github.com/utwente-interaction-lab/mediated-social-touch-web-application). It listens for (multi) touch commands from the server, which are translated to vibration patterns for a haptic sleeve device. There is support for the [bHaptics](https://github.com/utwente-interaction-lab/interaction-lab/wiki/Getting-started-with-the-bHaptics-Tactosy-for-Arms) sleeve, as well as a custom-built MIDI vibration sleeve available at HMI.

# Configuring and running the project
The project seems to work best with Visual Studio 2022. Open the `TouchController.sln` to load the project. From the `Build` menu, select `Build Solution` and make sure it can resolve all dependencies and builds without errors. If not, you will need to fix the errors before you can continue.

The client must connect to a [running server](https://github.com/utwente-interaction-lab/mediated-social-touch-web-application). You should configure the URI of the server on line 140 in file `MainWindow.xaml.cs`: 
* By default it connects to a server running on a UT machine: `client = new SocketIO("http://dog.ewi.utwente.nl:3000");`

You can configure which haptic sleeve is controlled by (un)commenting line 60/61:
* `activeTouchDevice = new MidiTouchDevice(new int[] { 48, 50, 49, 51, 52, 54, 53 });`
* `activeTouchDevice = new bHapticsTouchDevice();`

Run the project by pressing F5. A small semi-transparent window will pop up at the top left, that displays the incoming touch points. Stop the program with Shift+F5.