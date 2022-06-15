using System;
using HidLibrary;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Globalization;

namespace Temper
{
    class Program
    {
        private static HidDevice Control;
        private static HidDevice Bulk;
        private static String Manufacturer = String.Empty;
        private static String Product = String.Empty;
        private static String Serial = String.Empty;
        private static Double Calibration_Offset = 0;
        private static Double Calibration_Scale = 2.6;

        public delegate void ReadHandlerDelegate(HidReport report);
        static byte[] temp = { 0x01, 0x80, 0x33, 0x01, 0x00, 0x00, 0x00, 0x00 };
        static int waiting = 0;
        static double temp1 = 0;
        static double temp2 = 0;
        static void Main(string[] args)
        {
            HidDevice[] _deviceList = HidDevices.Enumerate().ToArray();
            List<HidDevice> _TemperInterfaces = _deviceList.Where(x => x.Attributes.ProductHexId == "0x2107" & x.Attributes.VendorHexId == "0x413D").ToList();
            //Temper has two interfaces
            Control = _TemperInterfaces.Find(x => x.DevicePath.Contains("mi_00"));
            if (Control != null)
            {
                Bulk = _TemperInterfaces.Find(x => x.DevicePath.Contains("mi_01"));
                // connect event handlers
                Control.Inserted += Device_Inserted;
                Control.Removed += Device_Removed;
                Control.MonitorDeviceEvents = true;
                var outData = Bulk.CreateReport();
                outData.ReportId = 0x00;
                outData.Data = temp;
                Bulk.WriteReport(outData);
                waiting += 2;
                while (outData.ReadStatus == HidDeviceData.ReadStatus.NoDataRead) ;
                Bulk.ReadReport(ReadTemperatureHandler);
                Bulk.ReadReport(ReadTemperatureHandler);                
                while(waiting>0)
                {
                    System.Threading.Thread.Sleep(100);
                }
                CultureInfo c=CultureInfo.CreateSpecificCulture("en-GB");
                if (args.Length>0)
                {
                    if (args[0] == "1")
                    {
                        Console.WriteLine(temp1.ToString(c));
                    }
                    if (args[0] == "2")
                    {
                        Console.WriteLine(temp2.ToString(c));
                    }
                } else
                {
                    Console.WriteLine(temp1.ToString(c) + '|' + temp2.ToString(c));
                }
            }
            else
            {
                throw (new Exception("No Device found"));
            }
        }

        private static void ReadTemperatureHandler(HidReport report)
        {
            int RawReading = (report.Data[3] & 0xFF) + (report.Data[2] << 8);
//            Console.WriteLine(BitConverter.ToString(report.Data));

            double temperatureCelsius = (Calibration_Scale * (RawReading * (125.0 / 32000.0))) + Calibration_Offset;
            if (report.Data[1] == 0x01)
            {
                temp1 = temperatureCelsius;
            }
            if (report.Data[1] == 0x80)
            {
                temp2 = temperatureCelsius;
            }
            waiting--;
        }


        private static void Device_Inserted()
        {

            //claim interfaces:
            Control.OpenDevice();
            Bulk.OpenDevice();
        }

        private static void Device_Removed()
        {
            Bulk.CloseDevice();
            Control.CloseDevice();
        }

    }
}
