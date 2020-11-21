using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Ports;

using static AIMS3.BackEnd.Common;

namespace AIMS3.BackEnd.Modules
{
	public class SerialConnection : Connection
    {
        private static SerialPort serialPort;
		private int index = 0;

		public int BaudRate { get; set; } = 9600;

		public static int SetTimeout(int timeout) => serialPort.ReadTimeout = timeout + 1;

        public static bool IsOpen => serialPort.IsOpen;

        public SerialConnection()
        {
			Initialize();
			Connect();
		}

        public static bool Connected { get => serialPort != null && serialPort.IsOpen; }

		public void Initialize()
		{
			try { serialPort?.Close(); }
			catch (Exception ex) { }
			serialPort = new SerialPort { PortName = "A", BaudRate = BaudRate, NewLine = "\r\n" };
		}

		static object connectionLockObject = new object();
		public bool Connect()
		{
			lock (connectionLockObject)
			{
				try
				{
					List<string> ports = SerialPort.GetPortNames().ToList();
					//ports.Remove("COM1");

					if (index >= ports.Count)
						index = -1;

					if (++index >= ports.Count)
					{
						if (ports.Count > 0)
							index = 0;

						else
							return false;
					}

					if (serialPort == null)
						Initialize();

					serialPort.PortName = ports[index];
					serialPort.BaudRate = BaudRate;
					serialPort.Open();

					return true;
				}
				catch (Exception ex) { WriteToDebug(typeof(SerialConnection), serialPort.PortName, nameof(Connect), ex); }
				finally { WriteToDebug(typeof(SerialConnection), serialPort.PortName, nameof(Connect), ""); }
				return false;
			}
		}

		public static void Disconnect()
		{
			lock (connectionLockObject)
			{
				try
				{
					if (serialPort.IsOpen)
						serialPort.Close();
				}
				catch (Exception ex) { WriteToDebug(typeof(SerialConnection), "Static", nameof(Disconnect), ex); }
			}
		}

		public override bool Write(string data)
		{
			try
			{
				if (serialPort == null)
					Initialize();

				if (!serialPort.IsOpen && !Connect())
					return false;

				serialPort.DiscardInBuffer();
				serialPort.WriteLine(data);

				return true;
			}

			catch (Exception ex)
			{
				Disconnect();
				return false;
			}
		}

        public override bool Write(byte[] data) => Write(Encoding.ASCII.GetString(data));

        public override string Read()
        {
            string str = "";

			try
			{
				if (!Connected)
					return str;

				str = serialPort.ReadLine();

				for (int i = 0; i < str.Length; i++)
					if (str[i] == '\0')
						str = str.Remove(i--, 1);
			}
			catch (Exception ex) { }
            return str;
        }
    }
}