using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Rug.Osc;

namespace SonyAlphaUSB
{
    class Program
    {
        static IPAddress targetAddress = IPAddress.Parse("127.0.0.1");
        static float fMin = 1f;
        static float fMax = 22f;
        static int localPort = 9002;
        static int remotePort = 9000;
        static OscSender VRCOscSender;

        static float NormalizeParameter(float value, float min, float max)
        {
            return (float)Math.Log(Math.Pow(value / min, 1 / Math.Log(max / min)));
        }

        static void OnFNumberChange(int fNumber)
        {
            float f = fNumber / 100f;
            float param = NormalizeParameter(f, fMin, fMax);
            Console.WriteLine("Aperture: {0:f} / param: {1:f}", f, param);
            VRCOscSender.Send(new OscMessage("/avatar/parameters/VirtualLens2Aperture", param));
        }

        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "wlog":
                        WIALogger.Run();
                        return;
                }
            }

            List<SonyCamera> cameras = WIA.FindCameras().ToList();
            foreach (SonyCamera camera in new List<SonyCamera>(cameras))
            {
                if (!camera.Connect())
                {
                    cameras.Remove(camera);
                }
            }

            Stopwatch stopwatch = new Stopwatch();
            int updateDelay = 41;// 24fps
            //int updateDelay = 33;// 30fps

            VRCOscSender = new OscSender(targetAddress, localPort, remotePort);
            VRCOscSender.Connect();
            int currentFNumber = 0;
            while (true)
            {
                stopwatch.Restart();

                foreach (SonyCamera camera in cameras)
                {
                    camera.Update();
                    if (currentFNumber != camera.FNumber)
                    {
                        currentFNumber = camera.FNumber;
                        OnFNumberChange(currentFNumber);
                    }
                }

                while (stopwatch.ElapsedMilliseconds < updateDelay)
                {
                    // This may result in stuttering as sleep can take longer than requested (maybe use a Timer?)
                    System.Threading.Thread.Sleep(1);
                }
            }
        }
    }
}