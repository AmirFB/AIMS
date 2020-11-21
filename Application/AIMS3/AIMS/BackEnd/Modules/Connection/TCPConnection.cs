using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

namespace AIMS3.BackEnd.Modules
{
	public class TCPConnection : Connection
    {
        private IPEndPoint ipEndPoint;
		private TcpClient client;
        private NetworkStream stream;
        private bool connected;

		public int TimeoutBias { get; set; } = 100;
		private int timeout;
		public int Timeout
		{
			get => timeout;
			set
			{
				try
				{
                    if (stream != null)
					    stream.ReadTimeout = value + TimeoutBias;

					timeout = value;
				}
				catch (Exception ex) { }
			}
		}

        public TCPConnection(IPEndPoint endPoint)
        {
            ipEndPoint = endPoint;
        }

        public TCPConnection(string ip, int port)
        {
            ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public IPEndPoint IPEndPoint { get => ipEndPoint; set => ipEndPoint = value; }
        public string IP { get => ipEndPoint.Address.ToString(); set => ipEndPoint.Address = IPAddress.Parse(value); }
        public int Port { get => ipEndPoint.Port; set => ipEndPoint.Port = value; }
        public bool Connected { get => connected; set => connected = value; }

        public bool Connect()
        {
            try
			{
				IAsyncResult result = null;

				client = new TcpClient(AddressFamily.InterNetwork);

				result = client.BeginConnect(IP, Port, null, null);

				if (result.AsyncWaitHandle.WaitOne(100))
					client.EndConnect(result);

				else
					return Connected = false;

				stream = client.GetStream();
				stream.ReadTimeout = Timeout + TimeoutBias;
				stream.WriteTimeout = Timeout + TimeoutBias;
				stream.Flush();
			}
			catch (Exception ex) { return Connected = false; }

            return Connected = true;
        }

		public void Close()
		{
			if (client != null)
				client.Close();

			client = null;
		}

		public override bool Write(byte[] data)
        {
            if (!connected && !Connect())
                return false;

            try
			{
				if (!connected || client == null || !client.Connected)
					if (!Connect())
						return false;

				stream.Flush();
                stream.Write(data, 0, data.Length);

                return true;
            }

            catch (Exception ex) { }

            connected = false;
            return false;
        }

        public override bool Write(string data)
        {
			return Write(Encoding.ASCII.GetBytes(data + "\r\n"));
        }

        public override string Read()
        {
            string str = "";

			if (!connected || client == null || !client.Connected)
				if (!Connect())
					return "";

            try
            {
                List<byte> data = new List<byte>();

                do
                {
                    data.Add((byte)stream.ReadByte());

                    if (data.Last() == 255)
                    {
                        client.Close();
                        connected = false;

                        break;
                    }
                } while (data.Last() != '\n' && data.Count <= 50);

                data.Remove(10);
                data.Remove(13);
                str = Encoding.ASCII.GetString(data.ToArray());
            }

            catch (Exception ex)
            {
                if (!ex.ToString().Contains("did not properly respond after a period of time"))
                    connected = false;

                Thread.Sleep(10);
            }

            finally
            {
				stream.Flush();
            }

            return str;
        }
	}
}