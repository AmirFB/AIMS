using System;
using System.Configuration;
using System.Globalization;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AIMS3.BackEnd.Cryptography;
using AIMS3.BackEnd.Modules;

using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.Log;
using static AIMS3.BackEnd.Modules.Fault;
using static AIMS3.BackEnd.Site.TelemetricSite;

namespace AIMS3.BackEnd.Site
{
	public class RemoteSite : TelemetricSite
	{
		public override bool Connected => Connection.Connected;
		public override void Send(string data) => Connection.Send(data);

		public RemoteSite() : base()
		{
			Type = SiteType.Remote;
			Connection = new RemoteConnection(this);
		}

		public async override Task<bool?> AuthenticationResult(string message)
		{
			if (!Connected)
				return null;

			try
			{
				Send(message);
				return await Connection.AuthenticateReceived(message).ConfigureAwait(false);
			}
			catch (Exception ex) { WriteToDebug(typeof(RemoteSite), Name, nameof(AuthenticationResult), ex); }
			return false;
		}

		public async override Task<(bool?, string)> GetResult(string message)
		{
			if (!Connected)
				return (null, "");

			try
			{
				Send(message);
				var data = await Connection.GetReceived(message).ConfigureAwait(false);
				var result = !string.IsNullOrEmpty(data);
				return (result, data);
			}
			catch (Exception ex) { WriteToDebug(typeof(RemoteSite), Name, nameof(GetResult), ex); }
			return (false, "");
		}

		public async override Task<bool?> AuthenticateCheckModule(IModule module)
		{
			try
			{
				var bias = 3000;
				Connection.Client.ReceiveTimeout = Timeout + bias;
				return await AuthenticationResult(GetStringCheckModule(module)).ConfigureAwait(false);
			}
			catch (Exception ex) { WriteToDebug(typeof(RemoteSite), Name, nameof(AuthenticateCheckModule), ex); }
			finally { Connection.Client.ReceiveTimeout = Timeout; }
			return false;
		}

		public async override Task<bool?> AuthenticateAcknowledgeFault(IFault fault)
		{
			try
			{
				var result = await AuthenticationResult(GetStringAcknowledgeFault(fault)).ConfigureAwait(false);

				if (result == true)
				{
					fault.Acknowledge(ResetStatus.Remote);
					return true;
				}
			}
			catch (Exception ex) { WriteToDebug(typeof(RemoteSite), Name, nameof(AuthenticateAcknowledgeFault), ex); }
			return false;
		}

		public async override Task<bool?> AuthenticateResetFault(IFault fault)
		{
			try
			{
				var result = await AuthenticationResult(GetStringResetFault(fault)).ConfigureAwait(false);

				if (result == true)
				{
					fault.Reset(ResetStatus.Remote, true);
					return true;
				}
			}
			catch (Exception ex) { WriteToDebug(typeof(RemoteSite), Name, nameof(AuthenticateResetFault), ex); }
			return false;
		}

		public async override Task<bool?> AuthenticateRaiseFault(IFault fault)
		{
			try
			{
				var result = await AuthenticationResult(GetStringRaiseFault(fault)).ConfigureAwait(false);

				if (result == true)
				{
					fault.RaiseNeeded = true;
					fault.Raise(ResetStatus.Remote);
					return true;
				}
			}
			catch (Exception ex) { WriteToDebug(typeof(RemoteSite), Name, nameof(AuthenticateRaiseFault), ex); }
			return false;
		}
		
		public async override Task<bool?> AuthenticateSetRelay(Relay relay) => await AuthenticationResult(GetStringSetRelay(relay)).ConfigureAwait(false);
		public async override Task<bool?> AuthenticateResetRelay(Relay relay) => await AuthenticationResult(GetStringResetRelay(relay)).ConfigureAwait(false);
		public async override Task<bool?> AuthenticateRemoveAllIcons() => await AuthenticationResult(GetStringRemoveAllIcons()).ConfigureAwait(false);
		public async override Task<bool?> AuthenticateAcknowledgeAllFaults() => await AuthenticationResult(GetStringAcknowledgeAllFaults()).ConfigureAwait(false);
		public async override Task<bool?> AuthenticateResetAllFaults() => await AuthenticationResult(GetStringResetAllFaults()).ConfigureAwait(false);
		public async override Task<bool?> AuthenticateInitializeAllModules() => await AuthenticationResult(GetStringInitializeAllModules()).ConfigureAwait(false);
		public async override Task<bool?> AuthenticateInitializeAllCams() => await AuthenticationResult(GetStringInitializeAllCams()).ConfigureAwait(false);
		public async override Task<bool?> AuthenticateSetAlarms() => await AuthenticationResult(GetStringAuthenticateSetAlarms()).ConfigureAwait(false);
		public async override Task<bool?> AuthenticateResetAlarms() => await AuthenticationResult(GetStringAuthenticateResetAlarms()).ConfigureAwait(false);

		public async override Task<(bool?, string)> GetReport(string from, string to)
		{
			try
			{
				DateTime start = DateTime.ParseExact(from, DateFormat, CultureInfo.InvariantCulture);
				DateTime end = DateTime.ParseExact(to, DateFormat, CultureInfo.InvariantCulture);

				var count = Math.Max((end - start).Days + 1, 0);
				var bias = Math.Min(10000, count * 1000);

				Connection.Client.ReceiveTimeout = Timeout + bias;
				return await GetResult(GetStringGetReport(from, to)).ConfigureAwait(false);
			}
			finally { Connection.Client.ReceiveTimeout = Timeout; }
		}
	}

	public class RemoteConnection : TelemetricConnection
	{

		public RemoteConnection(ITelemetricSite owner) : base(owner) { }
		private void Timer_Elapsed(object sender, ElapsedEventArgs e) => Client = null;

		public override async void ConnectThread()
		{
			Timer = new System.Timers.Timer(Timeout * 3);
			Timer.Elapsed += Timer_Elapsed;

			while (true)
			{
				try
				{
					if (Close)
					{
						WriteToDebug(typeof(RemoteSite), Owner.Name, nameof(ConnectThread), "Closed");
						return;
					}

					if (Client == null || !Client.Connected)
					{
						Timer.Stop();

						if (Client != null)
						{
							WriteToDebug(typeof(RemoteSite), Owner.Name, nameof(ConnectThread), "Disconnected");
							ShowNotification(GetResourceString("DisconnectedFromLocal"), this, TelemetricUsername);
						}

						if (Close)
							return;

						while (!Connect())
							Thread.Sleep(100);
					}
				}
				catch (Exception ex) { WriteToDebug(typeof(RemoteSite), Owner.Name, nameof(ConnectThread), ex); }
				Thread.Sleep(100);
			}
		}

		public override bool Connect()
		{
			IAsyncResult result;
			bool b = false;

			try
			{
				Client?.Close();
				Client = new TcpClient();
				result = Client.BeginConnect(HostAddress, Port, null, null);
				b = result.AsyncWaitHandle.WaitOne(Timeout);

				if (!b)
					return false;

				Client.EndConnect(result);
				Client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

				SetKeepAlive(Client, 100, 100);
				Stream = Client.GetStream();
				Stream.ReadTimeout = Timeout;
				Stream.WriteTimeout = Timeout;

				if (!BeginRead())
					return false;

				Send(Owner.Name + Spacers.Main + SHA.Hash(Owner.Password));
				Thread.Sleep(10);

				if (AuthenticateReceived(Commands.Authenticate).Result != true)
					return false;

				AsyncSend(Commands.AskAll);
				WriteToDebug(typeof(RemoteSite), Owner.Name, nameof(Connect), "Connected");
				ShowNotification(GetResourceString("ConnectedToLocal"), this, TelemetricUsername);
				return true;
			}
			catch (Exception ex) { WriteToDebug(typeof(RemoteSite), Owner.Name, nameof(Connect), ex); }
			return false;
		}
	}
}