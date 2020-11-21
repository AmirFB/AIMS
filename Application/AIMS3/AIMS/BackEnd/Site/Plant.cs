using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Site.Map;
using AIMS3.FrontEnd.Basic;
using AIMS3.FrontEnd.Modules;
using AIMS3.FrontEnd.Site;

using static System.Windows.Application;
using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.DataBase;
using static AIMS3.BackEnd.Modules.Connection;
using static AIMS3.BackEnd.Modules.Module;
using static AIMS3.BackEnd.Site.TelemetricSite;
using static AIMS3.BackEnd.Modules.Fault;

namespace AIMS3.BackEnd.Site
{
	public class Plant
	{
		public SiteType Type => Owner != null ? Owner.Type : SiteType.Local;

		public ITelemetricSite Owner { get; private set; }

		public int MaxConnectionError { get; set; } = 1;
		public int SerialTimoutBias { get; set; } = 0;
		public int SerialDelayBias { get; set; } = 0;
		public int TCPTimoutBias { get; set; } = 100;
		public int TCPDelayBias { get; set; } = 0;

		public MapView Map => View.Map;

		public SiteView View => Owner.View;
		public ModulesMenu ModulesWindow { get; set; }
		public ConnectionWindow ConnectionWindow { get; set; }
		public ReportWindow LogWindow { get; set; }
		public PlaceWindow PlaceWindow { get; set; }

		public string MainDirectory => Directories.Sites + Name + @"\";
		public string BackupPath => MainDirectory + @"Backup\";
		public string ModulesPath => MainDirectory + "pm" + Extentions.PlantModules;
		public string SettingsPath => MainDirectory + "ps" + Extentions.PlantSettings;

		public PlantConnectionType ConnectionType { get; set; }
		public int ConnectionIndex { get => (int)ConnectionType; set => ConnectionType = (PlantConnectionType)value; }
		public static int LanguageIndex { get => (int)Tongue; set => Tongue = (Language)value; }

		public enum PlantConnectionType { Direct, Local, Online };

		public string Name { get; set; }
		public bool IsPaused { get; set; }

		public bool MapShowStatistics { get; set; }
		public bool MapShowTemperature { get; set; }

		object settingsLock = new object();
		object modulesLock = new object();

		public Log Log { get; set; } = new Log();
		public FaultList FaultList { get; set; }
		private List<ConverterModule> TCPModules { get; set; } = new List<ConverterModule>();
		private ConverterModule SerialModule;

		public List<IModule> EF { get; } = new List<IModule>();
		public List<IModule> FG { get; } = new List<IModule>();
		public List<IModule> ACU { get; } = new List<IModule>();
		public List<IModule> Cam { get; } = new List<IModule>();
		public List<IModule> Analyzers { get; } = new List<IModule>();
		public List<IModule> Modules { get; } = new List<IModule>();
		public List<List<IModule>> ModulesTree { get; } = new List<List<IModule>>();

		public List<IFault> RaisedFaults { get; } = new List<IFault>();
		public List<IFault> Faults { get; } = new List<IFault>();
		public List<Relay> Relays { get; } = new List<Relay>();

		public string Message { get; private set; }

		public Relay GetRelay(string str) => Relays.Find(relay => relay.Text == str);
		public Camera GetCamera(string str) => Cam.Find(cam => cam.Name == str) as Camera;

		public Plant() { }

		public Plant(ITelemetricSite owner)
		{
			Owner = owner;
			FaultList = new FaultList() { Plant = this };
			Log.Plant = this;
		}

		public void Stop()
		{
			SerialModule?.Stop();
			TCPModules.ForEach(module => module?.Stop());
		}

		public void ShowModulesMenu()
		{
			if (ModulesWindow == null)
				ModulesWindow = new ModulesMenu() { Plant = this };

			var analyzers = Analyzers.GetRange(0, Analyzers.Count);
			ModulesWindow.PopUp();
			Map.Add(Analyzers.Where(a => !analyzers.Contains(a)).ToList());
		}

		public void ShowConnection()
		{
			ConnectionWindow = new ConnectionWindow(this);
			ConnectionWindow.ShowDialog();
		}

		public void ShowReport()
		{
			LogWindow = new ReportWindow(this);
			LogWindow.ShowDialog();
		}

		public void RefreshFaultList() => View?.RefreshFaultList();

		public bool? UploadModules(string data)
		{
			try
			{
				int index = 0;
				int efFailed = 0, fgFailed = 0, acuFailed = 0, camFailed = 0;

				string[] main = data.Split(new string[] { MajorSpacer }, StringSplitOptions.None);

				while (EF.Count > 0)
					DeleteModule(EF[0], false);

				while (FG.Count > 0)
					DeleteModule(FG[0], false);

				while (ACU.Count > 0)
					DeleteModule(ACU[0], false);

				while (Cam.Count > 0)
					DeleteModule(Cam[0], false);

				string[] cams = main[index++].Split(new string[] { MinorSpacer }, StringSplitOptions.RemoveEmptyEntries);

				foreach (string cam in cams)
				{
					Cam.Add(Camera.Parse(this, cam));

					if (Cam.Last() == null)
					{
						Cam.RemoveAt(Cam.Count - 1);
						camFailed++;
					}

					else
						(Cam.Last() as Camera).StartLoop();
				}

				string[] efs = main[index++].Split(new string[] { MinorSpacer }, StringSplitOptions.RemoveEmptyEntries);

				foreach (string ef in efs)
				{
					EF.Add(ElectroFence.Parse(this, ef));

					if (EF.Last() == null)
					{
						EF.RemoveAt(EF.Count - 1);
						efFailed++;
					}
				}

				string[] fgs = main[index++].Split(new string[] { MinorSpacer }, StringSplitOptions.RemoveEmptyEntries);

				foreach (string fg in fgs)
				{
					FG.Add(FlexiGuard.Parse(this, fg));

					if (FG.Last() == null)
					{
						FG.RemoveAt(FG.Count - 1);
						fgFailed++;
					}
				}

				string[] acus = main[index++].Split(new string[] { MinorSpacer }, StringSplitOptions.RemoveEmptyEntries);

				foreach (string acu in acus)
				{
					ACU.Add(AlarmControlUnit.Parse(this, acu));

					if (ACU.Last() == null)
					{
						ACU.RemoveAt(ACU.Count - 1);
						acuFailed++;
					}
				}

				Refresh();

				foreach (var module in Modules)
				{
					module.EvaluateRelays();
					module.InitializeView();
				}

				if ((camFailed + efFailed + fgFailed + acuFailed) > 0)
				{
					Message = string.Format("{0}\r\nEF: {1} {2} {3}\r\nFG: {4} {5} {6}\r\nACU: {7} {8} {9}\r\nCam: {10} {11} {12}",
						Current.TryFindResource("LoadingDevicesFailed"),
						efFailed, Current.TryFindResource("OutOf"), EF.Count + efFailed,
						fgFailed, Current.TryFindResource("OutOf"), FG.Count + fgFailed,
						acuFailed, Current.TryFindResource("OutOf"), ACU.Count + acuFailed,
						camFailed, Current.TryFindResource("OutOf"), Cam.Count + camFailed);

					return null;
				}

				return true;
			}
			catch (Exception ex) { }
			return false;
		}

		public string GetStringModules()
		{
			try
			{
				string data = "";

				foreach (Camera cam in Cam)
					data += cam.GetString() + MinorSpacer;

				data += MajorSpacer;

				EF.Sort((x, y) => x.Index.CompareTo(y.Index));

				foreach (ElectroFence ef in EF)
					data += ef.GetString() + MinorSpacer;

				data += MajorSpacer;

				FG.Sort((x, y) => x.Index.CompareTo(y.Index));

				foreach (FlexiGuard fg in FG)
					data += fg.GetString() + MinorSpacer;

				data += MajorSpacer;

				ACU.Sort((x, y) => x.Index.CompareTo(y.Index));

				foreach (AlarmControlUnit acu in ACU)
					data += acu.GetString() + MinorSpacer;

				return data;
			}
			catch (Exception ex) { WriteToDebug(typeof(Plant), Name, nameof(GetStringModules), ex); }
			return null;
		}

		public void SaveModules(bool evaluate)
		{
			try
			{
				lock (modulesLock)
				{
					if (!Directory.Exists(MainDirectory))
						Directory.CreateDirectory(MainDirectory);

					if (!Directory.Exists(BackupPath))
						Directory.CreateDirectory(BackupPath);

					if (File.Exists(ModulesPath))
						WriteToFile(string.Format("{0}{1}{2}", BackupPath, DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss"), Extentions.PlantModules), ReadFromFile(ModulesPath));

					Refresh();
					WriteToFile(ModulesPath, GetStringModules());

					if (evaluate)
						EvaluateLoops();
				}
			}
			catch (Exception ex) { WriteToDebug(typeof(Plant), Name, nameof(SaveModules), ex); }
		}

		public void LoadModules()
		{
			try
			{
				lock (modulesLock)
					UploadModules(ReadFromFile(ModulesPath));
			}
			catch (Exception ex) { WriteToDebug(typeof(Plant), Name, nameof(LoadModules), ex); }
		}

		public string GetStringSettings() => $"{MaxConnectionError},{SerialTimoutBias},{SerialDelayBias},{TCPTimoutBias},{TCPDelayBias},{MapShowStatistics},{MapShowTemperature}";

		public void UploadSettings(string data)
		{
			try
			{
				string[] pars = data.Split(',');
				int index = 0;

				MaxConnectionError = int.Parse(pars[index++]);
				SerialTimoutBias = int.Parse(pars[index++]);
				SerialDelayBias = int.Parse(pars[index++]);
				TCPTimoutBias = int.Parse(pars[index++]);
				TCPDelayBias = int.Parse(pars[index++]);
				MapShowStatistics = bool.Parse(pars[index++]);
				MapShowTemperature = bool.Parse(pars[index++]);

			}
			catch (Exception ex) { WriteToDebug(typeof(Plant), Name, nameof(UploadSettings), ex); }
		}

		public void SaveSettings()
		{
			try
			{
				lock (settingsLock)
					WriteToFile(SettingsPath, GetStringSettings());
			}
			catch (Exception ex) { WriteToDebug(typeof(Plant), Name, nameof(SaveSettings), ex); }
		}

		public void LoadSettings()
		{
			try
			{
				lock (settingsLock)
					UploadSettings(ReadFromFile(SettingsPath));
			}
			catch (Exception ex) { WriteToDebug(typeof(Plant), Name, nameof(LoadSettings), ex); }
		}

		public void Refresh()
		{
			try
			{
				Cam.Sort((x, y) => x.Index.CompareTo(y.Index));
				EF.Sort((x, y) => x.Index.CompareTo(y.Index));
				FG.Sort((x, y) => x.Index.CompareTo(y.Index));
				ACU.Sort((x, y) => x.Index.CompareTo(y.Index));

				Analyzers.Clear();
				Analyzers.AddRange(EF);
				Analyzers.AddRange(FG);
				Analyzers.AddRange(ACU);

				Modules.Clear();
				Modules.AddRange(Analyzers);
				Modules.AddRange(Cam);

				Faults.Clear();
				EF.ForEach(ef => Faults.AddRange(ef.Faults));
				FG.ForEach(fg => Faults.AddRange(fg.Faults));
				ACU.ForEach(acu => Faults.AddRange(acu.Faults));

				Relays.Clear();
				ACU.ForEach(acu => Relays.AddRange(acu.Relays));
				EF.ForEach(ef => Relays.AddRange(ef.Relays));
				FG.ForEach(fg => Relays.AddRange(fg.Relays));

				Analyzers.ForEach(module => module.RefreshIcons());

				ModulesTree.Clear();
				ModulesTree.Add(EF);
				ModulesTree.Add(FG);
				ModulesTree.Add(ACU);
				ModulesTree.Add(Cam);
			}
			catch (Exception ex) { WriteToDebug(typeof(Plant), Name, nameof(Refresh), ex); }
		}

		public bool Load()
		{
			try
			{
				Log.Plant = this;

				LoadSettings();
				LoadModules();

				EvaluateLoops();

				return true;
			}
			catch (Exception ex) { WriteToDebug(typeof(Plant), Name, nameof(Load), ex); }
			return false;
		}

		public void EvaluateLoops()
		{
			if (SerialModule == null)
				SerialModule = new ConverterModule(this, "");

			SerialModule.RefreshList();

			for (int i = 0; i < TCPModules.Count; i++)
			{
				TCPModules[i].RefreshList();

				if (TCPModules[i].Count == 0)
				{
					TCPModules[i].Stop();
					TCPModules.RemoveAt(i--);
				}
			}

			foreach (IModule module in Analyzers)
			{
				if (module.ConnectionType == ModuleConnectionType.TCP)
				{
					if (TCPModules.FindIndex(tcp => tcp.IPEndPoint == module.EndPoint) < 0)
					{
						TCPModules.Add(new ConverterModule(this, module.EndPoint));
						TCPModules.Last().RefreshList();
					}

					module.TCP = TCPModules.Find(tcp => tcp.IPEndPoint == module.EndPoint).TCPConnection;
				}
			}
		}

		public bool ModifyModule(string data)
		{
			string hash = data.Split(',')[0];
			IModule module = Analyzers.Find(match => match.Hash == hash);

			if (module == null)
				return false;

			if (module.Upload(data))
			{
				module.EvaluateRelays();
				Map.Add(module);
				return true;
			}

			return false;
		}

		public bool AddModule(string data)
		{
			var pars = data.Split(new string[] { Spacers.ModuleType }, StringSplitOptions.None);
			ModuleType type = (ModuleType)Enum.Parse(typeof(ModuleType), pars[0]);
			IModule module = null;

			try
			{
				switch (type)
				{
					case ModuleType.EF:
						module = ElectroFence.Parse(this, pars[1]);
						break;

					case ModuleType.FG:
						module = FlexiGuard.Parse(this, pars[1]);
						break;

					case ModuleType.ACU:
						module = AlarmControlUnit.Parse(this, pars[1]);
						break;

					case ModuleType.Cam:
						module = Camera.Parse(this, pars[1]);
						break;
				}

				if (module == null || module.Collection.Find(col => col.Index == module.Index || col.Address == module.Address) != null)
					return false;

				module.Collection.Add(module);
				module.InitializeView();
				Refresh();
				Map.Add(module);
				SaveModules(true);

				return true;
			}
			catch (Exception ex) { WriteToDebug(typeof(Plant), "", nameof(AddModule), ex); }
			return false;
		}

		public static void DeleteModule(IModule module, bool save) => module.ConverterModule.DeleteModule(module, save);

		public bool DeleteModule(string hash)
		{
			var module = Analyzers.Find(match => match.Hash == hash);

			if (module == null)
				return false;

			DeleteModule(module, true);
			return true;
		}

		public static bool CheckModule(IModule module, int repeat) => module.ConverterModule.CheckModule(module, repeat);

		public bool CheckModule(string hash)
		{
			var module = Modules.Find(module => module.Hash == hash);

			if (module == null)
				return false;

			return module.Check();
		}

		public bool AcknowledgeFault(string data)
		{
			var pars = data.Split(new string[] { Spacers.Fault }, StringSplitOptions.None);
			var module = Modules.Find(module => module.Hash == pars[0]);
			var index = int.Parse(pars[1]);

			if (module == null)
				return false;

			module.Faults[index].Acknowledge(ResetStatus.Remote);
			return true;
		}

		public bool ResetFault(string data)
		{
			var pars = data.Split(new string[] { Spacers.Fault }, StringSplitOptions.None);
			var module = Modules.Find(module => module.Hash == pars[0]);
			var index = int.Parse(pars[1]);

			if (module == null)
				return false;

			module.Faults[index].Reset(ResetStatus.Remote, true);
			return true;
		}

		public bool RaiseFault(string data)
		{
			var pars = data.Split(new string[] { Spacers.Fault }, StringSplitOptions.None);
			var module = Modules.Find(module => module.Hash == pars[0]);
			var index = int.Parse(pars[1]);

			if (module == null)
				return false;

			module.Faults[index].RaiseNeeded = true;
			module.Faults[index].Raise(ResetStatus.Remote);
			return true;
		}

		public bool SetRelay(string data)
		{
			var pars = data.Split(new string[] { Spacers.Relay }, StringSplitOptions.None);
			var module = Modules.Find(module => module.Hash == pars[0]);
			var index = int.Parse(pars[1]);

			if (module == null)
				return false;

			module.Relays[index].SetNeeded = true;
			return true;
		}

		public bool ResetRelay(string data)
		{
			var pars = data.Split(new string[] { Spacers.Relay }, StringSplitOptions.None);
			var module = Modules.Find(module => module.Hash == pars[0]);
			var index = int.Parse(pars[1]);

			if (module == null)
				return false;

			module.Relays[index].ResetNeeded = true;
			return true;
		}

		IModule previouModule = null;
		public bool? UpdateModuleStatistics(string data)
		{
			var pars = data.Split(new string[] { Spacers.ModuleType }, StringSplitOptions.None);
			var module = Modules.Find(module => module.Hash == pars[0]);

			if (module == null)
				return null;

			if (module.UploadStatistics(pars[1]))
			{
				module.RefreshIcons();

				if (previouModule != module)
					previouModule?.ResetIconsColor();

				previouModule = module;

				foreach (var relay in module.Relays)
					if (relay.State == Relay.RelayState.Set)
						relay.Icon.Raise();

				foreach (var fault in module.Faults)
					fault.StatusWindow?.Evaluate();

				return true;
			}

			return false;
		}

		public bool? UpdateModuleInitialization(string data)
		{
			var pars = data.Split(new string[] { Spacers.ModuleType }, StringSplitOptions.None);
			var module = Modules.Find(module => module.Hash == pars[0]);

			if (module == null)
				return null;

			switch (pars[1])
			{
				case Commands.Success:
					module.Initialized = true;
					break;

				case Commands.Failed:
					module.Initialized = false;
					break;
			}

			module.RefreshIconsInitialization();

			if (previouModule != module)
				previouModule?.ResetIconsColor();

			previouModule = module;
			return true;
		}

		public bool? UpdateFaults(string data)
		{
			var pars = data.Split(new string[] { Spacers.FaultFault }, StringSplitOptions.RemoveEmptyEntries);
			List<IFault> faults = new List<IFault>();

			foreach (var par in pars)
			{
				var temp = par.Split(new string[] { Spacers.ModuleFault }, StringSplitOptions.None);

				var module = Modules.Find(module => module.Hash == temp[0]);
				var index = int.Parse(temp[1]);

				if (module != null)
					faults.Add(module.Faults[index]);
			}

			foreach (var fault in faults)
				if (!RaisedFaults.Contains(fault))
					fault.Raise(ResetStatus.Local);

			foreach (var fault in RaisedFaults)
				if (!faults.Contains(fault))
					fault.Reset(ResetStatus.Local, false);

			return true;
		}

		public void RemoveAllIcons() => Current.Dispatcher.Invoke(new Action(() => { Map.RemoveAll(); }), null);

		public void AcknowledgeAllFaults(ResetStatus status) => RaisedFaults.ForEach(fault => { if (fault.CanAcknowledge) fault.Acknowledge(status); });
		public void ResetAllFaults(ResetStatus status) => RaisedFaults.ForEach(fault => { if (fault.CanReset) fault.Reset(status, true); });
	}
}