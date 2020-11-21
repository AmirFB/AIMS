using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AIMS3.BackEnd.Cryptography;

using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.Cryptography.AES;
using static AIMS3.BackEnd.Site.TelemetricSite;

namespace AIMS3.BackEnd.Site
{
	public interface ITelemetricConnection
	{
		ITelemetricSite Owner { get; }
		string HostName { get; set; }

		TcpClient Client { get; }
		NetworkStream Stream { get; }
		System.Timers.Timer Timer { get; }

		string TelemetricUsername { get; }

		bool Connected { get; }
		bool IsSync { get; set; }

		IPAddress LocalIP { get; }
		string HostAddress { get; }

		void Stop();
		void Start();
		void ConnectThread();
		bool Connect();
		string Authenticate();
		bool BeginRead();
		bool EndRead(IAsyncResult result, NetworkStream stream);
		Task<bool> AuthenticateReceived(string data);
		Task<string> GetReceived(string data);
		bool Send(string data);
		bool AsyncSend(string data);
		bool AsyncSendPure(string data);
	}

	public abstract class TelemetricConnection : ITelemetricConnection
	{
		public ITelemetricSite Owner { get; protected set; }
		public string HostName { get; set; }

		private TcpClient client;
		public TcpClient Client
		{
			get => client;
			set
			{
				if (value == null)
				{
					try
					{ client.Close(); }
					catch (Exception ex) { }
				}

				else
					client = value;
			}
		}
		public NetworkStream Stream { get; protected set; }
		public System.Timers.Timer Timer { get; protected set; }

		public string TelemetricUsername { get; protected set; }

		public IPAddress LocalIP { get; private set; }
		public string HostAddress => Owner.HostAddress;

		protected int Port => Owner.Port;

		private int ipIndex = 0;
		public virtual bool Connected => Client == null ? false : Client.Connected;
		public bool IsSync { get; set; }

		public int Timeout => Owner.Timeout;
		protected string ReceivedData { get; set; }

		private Thread connectThread;

		private object tranceiveLockObject = new object();
		private IAsyncResult readAsyncResult = null;
		private byte[] LengthBytes = new byte[4];

		private List<AuthenticationItem> AuthenticationItems { get; } = new List<AuthenticationItem>();

		protected bool Close { get; set; } = false;

		public TelemetricConnection(ITelemetricSite owner) => Owner = owner;

		public void Stop()
		{
			Close = true;

			try
			{ Client.Close(); }
			catch (Exception ex) { }
		}

		public void Start()
		{
			if (connectThread?.IsAlive == true)
				return;

			connectThread = new Thread(ConnectThread) { IsBackground = true, Name = string.Format("{0}({1}):{2}", nameof(TelemetricConnection), nameof(ConnectThread), Owner.Name) };
			connectThread.SetApartmentState(ApartmentState.STA);
			connectThread.Start();
		}

		public virtual void ConnectThread() { }
		public virtual bool Connect() => false;

		public virtual string Authenticate() => "";

		protected void UpdateIP()
		{
			IPAddress[] ipAddresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
			List<IPAddress> interIPAddresses = new List<IPAddress>();

			for (int i = 0; i < ipAddresses.Length; i++)
				if (ipAddresses[i].AddressFamily == AddressFamily.InterNetwork)
					interIPAddresses.Add(ipAddresses[i]);

			if (ipIndex >= interIPAddresses.Count)
				ipIndex = 0;

			if (interIPAddresses.Count > 0)
				LocalIP = interIPAddresses[ipIndex];

			else
				LocalIP = IPAddress.Parse("192.192.192.192");
		}

		public static void SetKeepAlive(TcpClient client, int time, int interval)
		{
			uint dummy = 0;
			byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];

			BitConverter.GetBytes(time).CopyTo(inOptionValues, 0);
			BitConverter.GetBytes(time).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
			BitConverter.GetBytes(interval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);

			client.Client.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
		}

		public bool BeginRead()
		{
			try
			{
				readAsyncResult = Stream.BeginRead(LengthBytes, 0, 4, new AsyncCallback(AsyncReceive), Stream);
				return true;
			}
			catch (Exception ex) { WriteToDebug(typeof(TelemetricConnection), Owner.Name, nameof(BeginRead), ex); }
			Client = null;
			return false;
		}

		public bool EndRead(IAsyncResult result, NetworkStream stream)
		{
			try
			{
				//Stream.EndRead(result);
				//Stream.EndRead(readAsyncResult);
				stream.EndRead(result);
				return true;
			}
			catch (Exception ex) { WriteToDebug(typeof(TelemetricConnection), Owner.Name, nameof(EndRead), ex); }
			Client = null;
			return false;
		}

		public string Receive()
		{
			int count = 0;

			while (count++ <= Timeout / 5)
			{
				if (!Connected || ReceivedData == Commands.Failed)
					return "";

				if (!string.IsNullOrEmpty(ReceivedData))
					return ReceivedData;

				Thread.Sleep(5);
			}

			return "";
		}

		public async Task<bool> AuthenticateReceived(string data)
		{
			int count = 0;
			data = Commands.Success + data;
			var item = new AuthenticationItem() { Message = data };
			AuthenticationItems.Add(item);

			try
			{
				while (count++ <= Timeout / 5)
				{
					if (!Connected || item.Authenticated == false)
						return false;

					if (item.Authenticated == true)
						return true;

					await Task.Delay(5).ConfigureAwait(false);
				}
			}
			finally { AuthenticationItems.Remove(item); }
			return false;
		}

		public async Task<string> GetReceived(string data)
		{
			int count = 0;
			data = Commands.Success + data;
			var item = new AuthenticationItem() { Message = data };
			AuthenticationItems.Add(item);

			try
			{
				while (count++ <= Timeout / 5)
				{
					if (!Connected || item.Authenticated == false)
						return "";

					if (item.Authenticated == true)
						return item.Message;

					await Task.Delay(5).ConfigureAwait(false);
				}
			}
			finally { AuthenticationItems.Remove(item); }
			return "";
		}

		protected void AsyncReceive(IAsyncResult result)
		{
			string content;
			byte[] data;
			int length;

			lock (tranceiveLockObject)
			{
				try
				{
					if (!Connected)
						return;

					var stream = (NetworkStream)result.AsyncState;
					EndRead(result, stream);

					length = BitConverter.ToInt32(LengthBytes, 0);

					if (length > 1000000 || length <= 4)
					{
						IsSync = false;
						return;
					}

					data = new byte[length];

					Stream.Read(data, 0, length);

					if (length > 4)
					{
						var wrong = true;

						for (int i = 0; i < length; i++)
						{
							if (data[i] != 0)
							{
								wrong = false;
								break;
							}
						}

						if (wrong)
						{
							Client = null;
							return;
						}
					}

					content = AIES.Decrypt(data.ToArray(), AESType.AES128);

					var pars = content.Split(new string[] { Spacers.Client }, StringSplitOptions.None);
					HostName = pars[0];
					pars = pars[1].Split(new string[] { Spacers.User }, StringSplitOptions.None);
					TelemetricUsername = pars[0];
					ReceivedData = pars[1];

					if (Timer != null)
					{
						Timer.Interval = Timeout * 3;
						Timer.Stop();
						Timer.Start();
					}

					var item = AuthenticationItems.Find(auth => ReceivedData.Contains(auth.Message));

					if (item != null)
					{
						item.Message = ReceivedData;
						item.Authenticated = true;
						return;
					}

					else
						Owner.ExecuteReceived(content, ReceivedData, this, TelemetricUsername);
				}
				catch (Exception ex) { WriteToDebug(typeof(RemoteSite), Owner.Name, nameof(AsyncReceive), ex); }
				finally { if (!(this is LocalConnection)) BeginRead(); }
			}
		}

		public bool Send(string data)
		{
			IsSync = true;
			return AsyncSend(data);
		}

		public bool AsyncSend(string data)
		{
			byte[] toSend = AIES.Encrypt(Owner.Name + Spacers.Client + CurrentUser?.Username + Spacers.User + data, AESType.AES128);
			byte[] length = new byte[4];

			if (!Connected)
				return false;

			try
			{
				//lock (tranceiveLockObject)
				{
					length = BitConverter.GetBytes(toSend.Length);
					toSend = length.Concat(toSend).ToArray();
					Stream.Write(toSend, 0, toSend.Length);
				}

				return true;
			}
			catch (Exception ex) { WriteToDebug(typeof(RemoteSite), Owner.Name, nameof(AsyncSend), ex); }
			Client = null;
			return false;
		}

		public bool AsyncSendPure(string data)
		{
			byte[] toSend = AIES.Encrypt(data, AESType.AES128);
			byte[] length = new byte[4];

			if (!Connected)
				return false;

			try
			{
				//lock (tranceiveLockObject)
				{
					length = BitConverter.GetBytes(toSend.Length);
					toSend = length.Concat(toSend).ToArray();
					Stream.Write(toSend, 0, toSend.Length);
				}

				return true;
			}
			catch (Exception ex) { WriteToDebug(typeof(RemoteSite), Owner.Name, nameof(AsyncSend), ex); }
			Client = null;
			return false;
		}
	}

	public class AuthenticationItem
	{
		public string Message { get; set; }
		public bool? Authenticated { get; set; } = null;
	}
}