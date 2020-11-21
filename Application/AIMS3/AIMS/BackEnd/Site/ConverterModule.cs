using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using AIMS3.BackEnd.Modules;

using static AIMS3.BackEnd.Modules.Connection;

namespace AIMS3.BackEnd.Site
{
	public class ConverterModule
	{
		private ITelemetricSite site;
		public virtual ITelemetricSite Site
		{
			get => site;
			protected set
			{
				site = value;
				plant = site.Plant;
			}
		}

		private Plant plant;
		public virtual Plant Plant
		{
			get => plant;
			protected set
			{
				plant = value;
				site = plant.Owner;
			}
		}

		public TCPConnection TCPConnection { get; set; }
		List<IFault> RaisedFaults => Plant.RaisedFaults;
		public string IPEndPoint { get; set; }
		bool close = false;
		public int Count => EF.Count + FG.Count + ACU.Count;
		public bool Connected => TCPConnection != null ? TCPConnection.Connected : SerialConnection.Connected;

		private List<IModule> EF = new List<IModule>();
		private List<IModule> FG = new List<IModule>();
		private List<IModule> ACU = new List<IModule>();
		private List<IModule> Analyzers = new List<IModule>();

		private object loopLockObject = new object();

		public ConverterModule(Plant site, string iPEndPoint)
		{
			Plant = site;
			IPEndPoint = iPEndPoint;
			try
			{ TCPConnection = new TCPConnection(new IPEndPoint(IPAddress.Parse(iPEndPoint.Split(':')[0]), int.Parse(iPEndPoint.Split(':')[1]))); }
			catch (Exception ex) { }
		}

		Thread loopThread;
		public void RefreshList()
		{
			//Close();

			lock (loopLockObject)
			{
				EF = Plant.EF.FindAll(ef => !string.IsNullOrEmpty(IPEndPoint) ? (ef.EndPoint == IPEndPoint && ef.ConnectionType == ModuleConnectionType.TCP) : ef.ConnectionType == ModuleConnectionType.Serial).ToList();
				FG = Plant.FG.FindAll(fg => !string.IsNullOrEmpty(IPEndPoint) ? (fg.EndPoint == IPEndPoint && fg.ConnectionType == ModuleConnectionType.TCP) : fg.ConnectionType == ModuleConnectionType.Serial).ToList();
				ACU = Plant.ACU.FindAll(acu => !string.IsNullOrEmpty(IPEndPoint) ? (acu.EndPoint == IPEndPoint && acu.ConnectionType == ModuleConnectionType.TCP) : acu.ConnectionType == ModuleConnectionType.Serial).ToList();

				Analyzers.Clear();
				Analyzers.AddRange(EF);
				Analyzers.AddRange(FG);
				Analyzers.AddRange(ACU);

				Analyzers.ForEach(analyzer => analyzer.ConverterModule = this);

				if (loopThread == null)
					Start();
			}
		}

		private object mainLockObject = new object();
		public void Start()
		{
			close = false;
			loopThread = new Thread(Loop) { IsBackground = true, Name = string.IsNullOrEmpty(IPEndPoint) ? "SerialModule" : IPEndPoint };
			loopThread.Start();
		}

		public void Stop()
		{
			close = true;
			TCPConnection?.Close();
		}

		public void ProbeModule(IModule module, IModule previous)
		{
			lock (loopLockObject)
			{
				if (!Plant.Analyzers.Contains(module))
					return;

				module.Probe();

				if (!Plant.Analyzers.Contains(previous))
					return;

				if (previous != module)
					previous?.ResetIconsColor();
				
				previous = module;

				Site.TransmitModuleStatistics(module);
				module.RefreshIcons();
			}
		}

		public bool CheckModule(IModule module, int repeat)
		{
			lock (loopLockObject)
			{
				if (!Plant.Analyzers.Contains(module))
					return false;

				return module.Initialize(repeat);
			}
		}

		public void DeleteModule(IModule module, bool save)
		{
			lock (loopLockObject)
			{
				module.Collection.Remove(module);
				Plant.Analyzers.Remove(module);
				module.Delete();
			}

			if (save)
				Plant.SaveModules(true);
		}

		public void Loop()
		{
			int count = 1;
			IModule previous = null;

			lock (mainLockObject)
			{
				while (true)
				{
					try
					{
						Thread.Sleep(1);
						//Plant.RefreshFaultList();

						if (close)
							return;

						if (Plant.Type == TelemetricSite.SiteType.Remote)
						{
							Thread.Sleep(100);
							continue;
						}

						if (!Connected)
							Thread.Sleep(100);

						if (TCPConnection == null && Analyzers.FindIndex(module => module.Initialized) < 0)
						{
							Thread.Sleep(100);
							SerialConnection.Disconnect();
						}

						if (Count == 0)
						{
							Thread.Sleep(100);
							continue;
						}

						if (RaisedFaults.Count == 0 && count != 0)
							Sound.Stop();

						count = RaisedFaults.Count;

						foreach (IModule module in Analyzers)
						{
							if (close)
								return;

							if (Plant.IsPaused)
							{
								Thread.Sleep(10);
								continue;
							}

							ProbeModule(module, previous);

							previous = module;

							Site.TransmitFaults();
						}
					}
					catch (Exception ex) { }
				}
			}
		}
	}
}