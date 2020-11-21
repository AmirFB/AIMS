using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using AIMS3.BackEnd.Cryptography;
using AIMS3.BackEnd.Modules;
using DevExpress.Data.TreeList;
using DevExpress.Mvvm.Native;
using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.Modules.Fault;
using static AIMS3.BackEnd.Site.TelemetricSite;

namespace AIMS3.BackEnd.Site
{
	class LocalSite : TelemetricSite
	{
		new LocalConnection Connection => base.Connection as LocalConnection;

		public LocalSite() : base()
		{
			Type = SiteType.Local;
			base.Connection = new LocalConnection(this);
		}

		public override void Send(string data) => Connection.SendToAll(data);
		public override void Send(string data, ITelemetricConnection except) => Connection.SendToAll(data, except);

		public string AuthenticateRemote(string data)
		{
			try
			{
				var pars = data.Split(new string[] { Spacers.Main }, StringSplitOptions.None);

				if (pars[1] == SHA.Hash(Password))
					return pars[0];
			}
			catch (Exception ex) { WriteToDebug(typeof(LocalSite), Name, nameof(AuthenticateRemote), ex); }
			return "";
		}

		public override Task<bool?> AuthenticationResult(string message)
		{
			try { Send(message); }
			catch (Exception ex) { WriteToDebug(typeof(LocalSite), Name, nameof(AuthenticationResult), ex); }
			return TaskEx.FromResult<bool?>(true);
		}

		public override Task<bool?> AuthenticateCheckModule(IModule module) => TaskEx.FromResult<bool?>(module.Check());

		public override Task<bool?> AuthenticateAcknowledgeFault(IFault fault)
		{
			fault.Acknowledge(ResetStatus.Local);
			return TaskEx.FromResult<bool?>(true);
		}

		public override Task<bool?> AuthenticateResetFault(IFault fault)
		{
			fault.Reset(ResetStatus.Local, true);
			return TaskEx.FromResult<bool?>(true);
		}

		public override Task<bool?> AuthenticateRaiseFault(IFault fault)
		{
			fault.RaiseNeeded = true;
			return TaskEx.FromResult<bool?>(true);
		}

		public override Task<bool?> AuthenticateSetRelay(Relay relay)
		{
			relay.SetNeeded = true;
			return TaskEx.FromResult<bool?>(true);
		}

		public override Task<bool?> AuthenticateResetRelay(Relay relay)
		{
			relay.ResetNeeded = true;
			return TaskEx.FromResult<bool?>(true);
		}

		public override Task<bool?> AuthenticateRemoveAllIcons() => AuthenticationResult(GetStringRemoveAllIcons());

		public override Task<bool?> AuthenticateAcknowledgeAllFaults()
		{
			try { Plant.AcknowledgeAllFaults(ResetStatus.Local); }
			catch (Exception ex) { WriteToDebug(typeof(LocalSite), Name, nameof(AuthenticateAcknowledgeAllFaults), ex); }
			return TaskEx.FromResult<bool?>(true);
		}

		public override Task<bool?> AuthenticateResetAllFaults()
		{
			try { Plant.ResetAllFaults(ResetStatus.Local); }
			catch (Exception ex) { WriteToDebug(typeof(LocalSite), Name, nameof(AuthenticateResetAllFaults), ex); }
			return TaskEx.FromResult<bool?>(true);
		}

		public override Task<bool?> AuthenticateSetAlarms()
		{
			try { Plant.Relays.ForEach(relay => relay.SetNeeded = true); }
			catch (Exception ex) { WriteToDebug(typeof(LocalSite), Name, nameof(AuthenticateSetAlarms), ex); }
			return TaskEx.FromResult<bool?>(true);
		}

		public override Task<bool?> AuthenticateResetAlarms()
		{
			try { Plant.Relays.ForEach(relay => relay.ResetNeeded = true); }
			catch (Exception ex) { WriteToDebug(typeof(LocalSite), Name, nameof(AuthenticateResetAlarms), ex); }
			return TaskEx.FromResult<bool?>(true);
		}

		public override Task<bool?> AuthenticateInitializeAllModules()
		{
			try { Plant.Analyzers.ForEach(analyzer => analyzer.InitializeNeeded = true); }
			catch (Exception ex) { WriteToDebug(typeof(LocalSite), Name, nameof(AuthenticateInitializeAllModules), ex); }
			return TaskEx.FromResult<bool?>(true);
		}

		public override Task<bool?> AuthenticateInitializeAllCams()
		{
			try { Plant.Cam.ForEach(cam => cam.InitializeNeeded = true); }
			catch (Exception ex) { WriteToDebug(typeof(LocalSite), Name, nameof(AuthenticateInitializeAllCams), ex); }
			return TaskEx.FromResult<bool?>(true);
		}
	}

	public class LocalConnection : TelemetricConnection
	{
		private TcpListener listener { get; set; }
		private List<Node> Nodes { get; } = new List<Node>();
		new LocalSite Owner => base.Owner as LocalSite;

		public override bool Connected => true;

		public LocalConnection(ITelemetricSite owner) : base(owner) { }

		public override void ConnectThread()
		{
			IAsyncResult result;

			while (true)
			{
				try
				{
					if (Close)
						return;

					UpdateIP();

					listener?.Stop();
					listener = new TcpListener(IPAddress.Any, Port);
					listener.Start(10);
					result = listener.BeginAcceptTcpClient(null, null);

					if (result.AsyncWaitHandle.WaitOne(Timeout))
					{
						Client = listener.EndAcceptTcpClient(result);

						Stream = Client.GetStream();
						Stream.ReadTimeout = Timeout;
						Stream.WriteTimeout = Timeout;
						
						BeginRead();
						HostName = Authenticate();

						if (HostName.Length == 0)
							continue;

						System.Threading.Thread.Sleep(10);
						AsyncSend(Commands.Success + Commands.Authenticate);

						var nodes = Nodes.FindAll(node => node.HostName == HostName);

						nodes.ForEach(node => node?.Stop());

						Nodes.Add(new Node(Owner, Client, Stream, HostName));
						WriteToDebug(typeof(LocalSite), Owner.Name, nameof(ConnectThread), "Connected to " + HostName);
						ShowNotification(GetResourceString("ConnectedToRemote"), this, TelemetricUsername);
					}

					for (int i = 0; i < Nodes.Count; i++)
					{
						if (Nodes[i] == null)
							Nodes.Remove(Nodes[i]);

						if (!Nodes[i].Connected)
						{
							Nodes[i].Stop();
							ShowNotification(GetResourceString("DisconnectedFromRemote"), Nodes[i], Nodes[i].TelemetricUsername);
							Nodes.RemoveAt(i--);
						}

						else
							Nodes[i].Send("dummy");
					}
				}
				catch (Exception ex) { WriteToDebug(typeof(LocalSite), Owner.Name, nameof(ConnectThread), ex); }
			}
		}

		public void SendToAll(string data) => Parallel.ForEach(Nodes, node => { node.AsyncSend(data); });
		public void SendToAll(string data, ITelemetricConnection except) => Parallel.ForEach(Nodes, node => { if (node != except) node.AsyncSendPure(data); });

		public override string Authenticate()
		{
			string data;

			try
			{
				IsSync = true;
				data = Receive();
				return Owner.AuthenticateRemote(data);
			}
			catch (Exception ex) { WriteToDebug(typeof(LocalConnection), Owner.Name, nameof(Authenticate), ex); }
			return "";
		}

		private class Node : TelemetricConnection
		{
			public new LocalSite Owner => base.Owner as LocalSite;
			public override bool Connected => Client == null ? false : Client.Connected;

			public Node(ITelemetricSite owner, TcpClient client, NetworkStream stream, string name) : base(owner)
			{
				Client = client;
				Stream = stream;
				HostName = name;
				BeginRead();
			}
		}
	}
}