using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Basic;
using AIMS3.FrontEnd.Site;
using AIMS3.FrontEnd.Site.Map;
using DevExpress.Xpf.Core;

using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.DataBase;
using static AIMS3.BackEnd.Modules.Fault;
using static AIMS3.BackEnd.Site.TelemetricSite;

namespace AIMS3.BackEnd.Site
{
	public interface ITelemetricSite
	{
		string Name { get; set; }
		string Password { get; set; }

		IPAddress LocalIP { get; set; }
		string HostAddress { get; set; }
		int Port { get; set; }

		SiteSettingsWindow SettingsWindow { get; set; }

		int Timeout { get; set; }
		int SerialTimoutBias { get; set; }
		int SerialDelayBias { get; set; }
		int TCPTimoutBias { get; set; }
		int TCPDelayBias { get; set; }

		bool Connected { get; }
		Plant Plant { get; }

		SiteType Type { get; set; }
		SiteAccess Access { get; set; }
		bool IsLocal { get; }
		bool IsRemote { get; }

		SiteView View { get; }
		MapView Map { get; }

		bool IsNew { get; set; }

		string MainDirectory { get; }
		string LogDirectory { get; }

		void Start();
		void Send(string data);
		void Send(string data, ITelemetricConnection except);

		void UploadImage(string path);
		void ShowSettings();

		void ExecuteReceived(string content, string data, ITelemetricConnection connection, string user);

		string GetString();
		void Upload(string name, string[] data);
		void UploadSettings(string data);
		void DeleteSite();
		bool SaveSettings(string oldName);

		Task<bool?> AuthenticationResult(string message);
		Task<(bool?, string)> GetResult(string message);

		Task<bool?> AuthenticateSettings(string data);
		Task<bool?> AuthenticateModifyModule(IModule module);
		Task<bool?> AuthenticateAddModule(IModule module);
		Task<bool?> AuthenticateDeleteModule(IModule module);
		Task<bool?> AuthenticateCheckModule(IModule module);

		Task<bool?> AuthenticateAcknowledgeFault(IFault fault);
		Task<bool?> AuthenticateResetFault(IFault fault);
		Task<bool?> AuthenticateRaiseFault(IFault fault);

		Task<bool?> AuthenticateSetRelay(Relay relay);
		Task<bool?> AuthenticateResetRelay(Relay relay);

		Task<bool?> AuthenticateRemoveAllIcons();

		Task<bool?> AuthenticateAcknowledgeAllFaults();
		Task<bool?> AuthenticateResetAllFaults();

		Task<bool?> AuthenticateInitializeAllModules();
		Task<bool?> AuthenticateInitializeAllCams();

		Task<bool?> AuthenticateSetAlarms();
		Task<bool?> AuthenticateResetAlarms();

		Task<(bool? Success, string Data)> GetReport(string from, string to);

		void TransmitModuleStatistics(IModule module);
		void TransmitModuleInitialization(IModule module);
		void TransmitFaults();
	}

	public abstract class TelemetricSite : ITelemetricSite
	{
		public Plant Plant { get; protected set; }
		public enum SiteType { Local, Remote };
		public enum SiteAccess { Internal, Internet };

		private string name;
		public string Name
		{
			get => name;
			set
			{
				name = value;
				Plant.Name = name;
			}
		}

		public string Password { get; set; }

		public IPAddress LocalIP { get; set; }
		public string HostAddress { get; set; }
		public int Port { get; set; }

		public int Timeout { get; set; } = 2000;
		public int SerialTimoutBias { get => Plant.SerialTimoutBias; set => Plant.SerialTimoutBias = value; }
		public int SerialDelayBias { get => Plant.SerialDelayBias; set => Plant.SerialDelayBias = value; }
		public int TCPTimoutBias { get => Plant.TCPTimoutBias; set => Plant.TCPTimoutBias = value; }
		public int TCPDelayBias { get => Plant.TCPDelayBias; set => Plant.TCPDelayBias = value; }

		public virtual bool Connected => false;
		public ITelemetricConnection Connection { get; set; }

		public SiteSettingsWindow SettingsWindow { get; set; }

		public SiteType Type { get; set; } = SiteType.Local;
		public SiteAccess Access { get; set; } = SiteAccess.Internal;
		public bool IsLocal => Type == SiteType.Local;
		public bool IsRemote => Type == SiteType.Remote;

		public SiteView View { get; set; }
		public MapView Map => View.Map;

		public bool IsNew { get; set; } = false;

		object settingsLock = new object();

		public string MainFolder=> Directories.Sites + Name;
		public string MainDirectory => Directories.Sites + Name + @"\";
		public string SettingsDirectory => MainDirectory + "ss" + Extentions.SiteSettings;
		public string LogDirectory => MainDirectory + @"Log\";

		public static class Commands
		{
			public const string
				Acknowledge = "Ack",
				AcknowledgeAll = "Aka",
				Add = "Add",
				Alarm = "Alr",
				All = "All",
				AskAll = "Asa",
				AskStatistics = "Ass",
				Authenticate = "Ath",
				Check = "Chk",
				Delete = "Rmv",
				Failed = "Fid",
				Fault = "Flt",
				Initialization = "Int",
				InitializeAllAnalyzers = "Iaa",
				InitializeAllCameras = "Iac",
				Modify = "Mdf",
				Module = "Mdl",
				Raise = "Ris",
				Relay = "Rly",
				RemoveIcons = "Ric",
				Report = "Rpt",
				Reset = "Rst",
				ResetAlarms = "Ral",
				ResetAll = "Rsa",
				Set = "Set",
				SetAlarms = "Sal",
				Settings = "Stg",
				Statistics = "Sts",
				Success = "Scs",
				Update = "Upd",
				Utility = "Utl";
		}

		public static class Spacers
		{
			public const string
				All = "*(*(v (*",
				Client = "||)uy))|",
				Date = "8:j+_2",
				Fault = "/?y/?/?/",
				FaultFault = "g^6 +}s",
				Main = "%^h^%",
				Module = "~~!2&!~",
				ModuleFault = "4%(6H,&",
				ModuleType = "+=n===+",
				Relay = "8&838=6+8",
				Report = "9L3K9d3K7",
				Type = "!@!r !@!@!",
				User = "*#* f*#*#*";
		}

		public string GetStringAll() => Commands.All + Spacers.Main + Plant.GetStringModules() + Spacers.All + Plant.GetStringSettings();

		public TelemetricSite()
		{
			Plant = new Plant(this);
			View = new SiteView(this);
		}

		public void Start() => Connection.Start();
		public abstract void Send(string data);
		public virtual void Send(string data, ITelemetricConnection except) { }

		private void RequestAll() => Connection.AsyncSend(Commands.AskAll);

		public void UploadImage(string path) => View.UploadImage(path);

		public void ShowSettings()
		{
			SettingsWindow = new SiteSettingsWindow(this);
			SettingsWindow.ShowDialog();
		}

		protected bool Apply(string data, ITelemetricConnection connection, string user)
		{
			string[] pars = data.Split(new string[] { Spacers.Type }, StringSplitOptions.None);

			switch (pars[0])
			{
				case Commands.Settings:
					return ApplySettings(pars[1], connection, user);

				case Commands.Module:
					return ApplyModule(pars[1], connection, user);

				case Commands.Fault:
					return ApplyFault(pars[1], connection, user);

				case Commands.Relay:
					return ApplyRelay(pars[1], connection, user);

				case Commands.Utility:
					return ApplyUtility(pars[1], connection, user);
			}
			return false;
		}

		protected bool ApplySettings(string data, ITelemetricConnection connection, string user)
		{
			string temp = Plant.GetStringSettings();

			try
			{
				Plant.UploadSettings(data);
				ShowNotification(GetResourceString("SettingsChanged"), connection, user);
				return true;
			}
			catch (Exception ex) { WriteToDebug(typeof(TelemetricSite), Name, nameof(ApplySettings), ex); }
			Plant.UploadSettings(temp);
			return false;
		}

		protected bool ApplyModule(string data, ITelemetricConnection connection, string user)
		{
			string[] pars = data.Split(new string[] { Spacers.Module }, StringSplitOptions.None);
			string temp = Plant.GetStringModules();

			try
			{
				switch (pars[0])
				{
					case Commands.Modify:
						if (Plant.ModifyModule(pars[1]))
						{
							ShowNotification(GetResourceString("ModuleModified"), connection, user);
							return true;
						}

						Plant.EvaluateLoops();

						if (Type == SiteType.Remote)
							RequestAll();

						break;

					case Commands.Add:
						if (Plant.AddModule(pars[1]))
						{
							ShowNotification(GetResourceString("ModuleAdded"), connection, user);
							return true;
						}

						Plant.EvaluateLoops();

						if (Type == SiteType.Remote)
							RequestAll();

						break;

					case Commands.Delete:
						if (Plant.DeleteModule(pars[1]))
						{
							ShowNotification(GetResourceString("ModuleDeleted"), connection, user);
							return true;
						}

						Plant.EvaluateLoops();

						if (Type == SiteType.Remote)
							RequestAll();

						break;

					case Commands.Check:
						if (Plant.CheckModule(pars[1]))
						{
							ShowNotification(GetResourceString("ModuleChecked"), connection, user);
							return true;
						}

						return false;
				}
			}
			catch (Exception ex) { WriteToDebug(typeof(TelemetricSite), Name, nameof(ApplyModule), ex); }
			Plant.UploadModules(temp);
			return false;
		}

		protected bool ApplyFault(string data, ITelemetricConnection connection, string user)
		{
			string[] pars = data.Split(new string[] { Spacers.Module }, StringSplitOptions.None);
			string temp = Plant.GetStringModules();

			try
			{
				switch (pars[0])
				{
					case Commands.Acknowledge:
						if (Plant.AcknowledgeFault(pars[1]))
						{
							ShowNotification(GetResourceString("FaultAcknowledged"), connection, user);
							return true;
						}

						break;

					case Commands.Reset:
						if (Plant.ResetFault(pars[1]))
						{
							ShowNotification(GetResourceString("FaultReseted"), connection, user);
							return true;
						}

						break;

					case Commands.Raise:
						if (Plant.RaiseFault(pars[1]))
						{
							ShowNotification(GetResourceString("FaultRaised"), connection, user);
							return true;
						}

						break;
				}
			}
			catch (Exception ex) { WriteToDebug(typeof(TelemetricSite), Name, nameof(ApplyFault), ex); }
			return false;
		}

		protected bool ApplyRelay(string data, ITelemetricConnection connection, string user)
		{
			string[] pars = data.Split(new string[] { Spacers.Module }, StringSplitOptions.None);

			try
			{
				switch (pars[0])
				{
					case Commands.Set:
						if (Plant.SetRelay(pars[1]))
						{
							ShowNotification(GetResourceString("RelaySet"), connection, user);
							return true;
						}

						break;

					case Commands.Reset:
						if (Plant.ResetRelay(pars[1]))
						{
							ShowNotification(GetResourceString("RelayReset"), connection, user);
							return true;
						}

						break;
				}
			}
			catch (Exception ex) { WriteToDebug(typeof(TelemetricSite), Name, nameof(ApplyRelay), ex); }
			return false;
		}

		protected bool ApplyUtility(string data, ITelemetricConnection connection, string user)
		{
			string[] pars = data.Split(new string[] { Spacers.Type }, StringSplitOptions.None);

			try
			{
				switch (pars[0])
				{
					case Commands.RemoveIcons:
						Plant.RemoveAllIcons();
						ShowNotification(GetResourceString("AllIconsRemoved"), connection, user);
						return true;

					case Commands.AcknowledgeAll:
						Plant.AcknowledgeAllFaults(ResetStatus.Remote);
						ShowNotification(GetResourceString("AllFaultsAcknowledged"), connection, user);
						return true;

					case Commands.ResetAll:
						Plant.AcknowledgeAllFaults(ResetStatus.Remote);
						ShowNotification(GetResourceString("AllFaultsReseted"), connection, user);
						return true;

					case Commands.InitializeAllAnalyzers:
						Plant.Analyzers.ForEach(analyzer => analyzer.InitializeNeeded = true);
						ShowNotification(GetResourceString("AllAnalyzersInitilaized"), connection, user);
						return true;

					case Commands.InitializeAllCameras:
						Plant.Cam.ForEach(analyzer => analyzer.InitializeNeeded = true);
						ShowNotification(GetResourceString("AllCamerasInitilaized"), connection, user);
						return true;

					case Commands.SetAlarms:
						Plant.Relays.ForEach(relay => relay.SetNeeded = true);
						ShowNotification(GetResourceString("AllAlarmsSet"), connection, user);
						return true;

					case Commands.ResetAlarms:
						Plant.Relays.ForEach(relay => relay.ResetNeeded = true);
						ShowNotification(GetResourceString("AllAlarmsReset"), connection, user);
						return true;
				}
			}
			catch (Exception ex) { WriteToDebug(typeof(TelemetricSite), Name, nameof(ApplyRelay), ex); }
			return false;
		}

		public static string GetPath(string name) => Directories.Sites + name + "\\ss" + Extentions.SiteSettings;

		public static void LoadSites()
		{
			try
			{
				string[] directories = Directory.GetDirectories(Directories.Sites);
				string[] names = System.Array.ConvertAll(directories, site => site.Split('\\').Last());
				string temp;
				string[] pars;
				ITelemetricSite site;
				int index = 0;
				string path;

				Sites.Clear();

				for (int i = 0; i < directories.Length; i++)
				{
					try
					{
						path = GetPath(names[i]);

						if ((temp = ReadFromFile(path)) == null)
							continue;

						pars = temp.Split(',');
						index = 0;

						if (pars[index++] == SiteType.Remote.ToString())
							site = new RemoteSite();

						else
							site = new LocalSite();

						site.Upload(names[i], pars);

						if (site != null)
						{
							Sites.Add(site);
							site.Start();
						}
					}
					catch (Exception ex) { }
				}
			}
			catch (Exception ex) { throw new Exception("Sites load failed.\r\n" + ex); }
		}

		public string GetString() => string.Format("{0},{1},{2},{3},{4},{5}", Type, Password, HostAddress, Port, Timeout, Access);

		public void Upload(string name, string[] data)
		{
			int index = 1;
			Name = name;
			Password = data[index++];
			HostAddress = data[index++];
			Port = int.Parse(data[index++]);
			Timeout = int.Parse(data[index++]);
			Access = (SiteAccess)Enum.Parse(typeof(SiteAccess), data[index++]);
			Plant.Load();
		}

		public void UploadSettings(string data)
		{
			try
			{
				string[] pars = data.Split(',');
				int index = 0;

				SerialTimoutBias = int.Parse(pars[index++]);
				SerialDelayBias = int.Parse(pars[index++]);
				TCPTimoutBias = int.Parse(pars[index++]);
				TCPDelayBias = int.Parse(pars[index++]);

			}
			catch (Exception ex) { WriteToDebug(typeof(Plant), Name, nameof(UploadSettings), ex); }
		}

		public bool SaveSettings(string oldName)
		{
			try
			{
				lock (settingsLock)
				{
					if (IsNew)
					{
						IsNew = false;

						if (!Directory.Exists(Directories.Sites))
							Directory.CreateDirectory(Directories.Sites);

						if (!Directory.Exists(MainDirectory))
							Directory.CreateDirectory(MainDirectory);

						Start();
						Sites.Add(this);
						MainWindow.AddSite(this);
					}

					if (oldName != null && oldName != Name)
						Directory.Move(Directories.Sites + oldName, Directories.Sites + Name);

					WriteToFile(SettingsDirectory, GetString());
					return true;
				}
			}
			catch (Exception ex) { WriteToDebug(typeof(Plant), Name, nameof(SaveSettings), ex); }
			return false;
		}

		public void DeleteSite()
		{
			Connection.Stop();

			if (Directory.Exists(MainFolder))
				Directory.Delete(MainFolder, true);

			Plant.Stop();
			View = null;
			Sites.Remove(this);
		}

		public bool Update(string data, ITelemetricConnection connection)
		{
			try
			{
				var pars = data.Split(new string[] { Spacers.Type }, StringSplitOptions.None);
				bool? result = false;

				switch (pars[0])
				{
					case Commands.Statistics:
						result = Plant.UpdateModuleStatistics(pars[1]);
						break;

					case Commands.Initialization:
						result = Plant.UpdateModuleInitialization(pars[1]);
						break;

					case Commands.Fault:
						result = Plant.UpdateFaults(pars[1]);
						//result = true;
						break;
				}

				if (result == true)
					return true;

				RequestAll();
			}
			catch (Exception ex) { WriteToDebug(typeof(TelemetricSite), Name, nameof(Update), ex); }
			return false;
		}

		public void ExecuteReceived(string content, string data, ITelemetricConnection connection, string user)
		{
			try
			{
				string[] pars = data.Split(new string[] { Spacers.Main }, StringSplitOptions.None);
				bool result;

				switch (pars[0])
				{
					case Commands.AskAll:
						connection.AsyncSend(GetStringAll());
						break;

					case Commands.AskStatistics:
						Plant.Modules.ForEach(module => TransmitModuleStatistics(module));
						break;

					case Commands.All:
						string[] temp = pars[1].Split(new string[] { Spacers.All }, StringSplitOptions.None);
						var dummy = Application.Current.Dispatcher.Invoke(new Func<bool>(() => { Map.RemoveAll(); return true; }), null);

						while (Plant.RaisedFaults.Count > 0)
							Plant.RaisedFaults[0].Reset(ResetStatus.Local, false);

						Plant.UploadModules(temp[0]);

						Plant.EvaluateLoops();
						dummy = Application.Current.Dispatcher.Invoke(new Func<bool>(() => { Map.AddAll(); return true; }), null);
						Plant.UploadSettings(temp[1]);
						ShowNotification(GetResourceString("AllReceived"), connection, user);
						break;

					case Commands.Authenticate:
						result = Apply(pars[1], connection, user);

						if (Type == SiteType.Remote)
							return;

						if (result)
						{
							connection.AsyncSend(Commands.Success + data);
							Send(content, connection);
						}

						else
							connection.AsyncSend(Commands.Failed);

						break;

					case Commands.Update:
						result = Update(pars[1], connection);

						if (result != true)
							RequestAll();

						break;

					case Commands.Report:
						connection.AsyncSend(Commands.Success + data + Plant.Log.GetRawData(pars[1]));
						break;
				}
			}
			catch (Exception ex) { WriteToDebug(typeof(TelemetricSite), Name, nameof(ExecuteReceived), ex); }
		}

		public static void ShowMessageBoxAuthentication(bool? result)
		{
			if (result == true)
				return;// ShowMessageBoxSuccess();

			else if (result == false)
				ShowMessageBoxNotAuthorized();

			else
				ShowMessageBoxNotConnected();
		}

		private static void ShowMessageBoxSuccess() => DXMessageBox.Show((string)Application.Current.FindResource("LocalSiteSuccess"), (string)Application.Current.FindResource("Success"), MessageBoxButton.OK, MessageBoxImage.Information);
		private static void ShowMessageBoxNotConnected() => DXMessageBox.Show((string)Application.Current.FindResource("LocalSiteNotConnected"), (string)Application.Current.FindResource("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
		private static void ShowMessageBoxNotAuthorized() => DXMessageBox.Show((string)Application.Current.FindResource("LocalSiteNotAuthorized"), (string)Application.Current.FindResource("Error"), MessageBoxButton.OK, MessageBoxImage.Error);

		public static string GetStringSettings(string data) => Commands.Authenticate + Spacers.Main + Commands.Settings + Spacers.Type + data;
		public static string GetStringModifyModule(IModule module) => Commands.Authenticate + Spacers.Main + Commands.Module + Spacers.Type + Commands.Modify + Spacers.Module + module.GetString();
		public static string GetStringAddModule(IModule module) => Commands.Authenticate + Spacers.Main + Commands.Module + Spacers.Type + Commands.Add + Spacers.Module + module.Type + Spacers.ModuleType + module.GetString();
		public static string GetStringDeleteModule(IModule module) => Commands.Authenticate + Spacers.Main + Commands.Module + Spacers.Type + Commands.Delete + Spacers.Module + module.Hash;
		public static string GetStringCheckModule(IModule module) => Commands.Authenticate + Spacers.Main + Commands.Module + Spacers.Type + Commands.Check + Spacers.Module + module.Hash;

		public static string GetStringAcknowledgeFault(IFault fault) => Commands.Authenticate + Spacers.Main + Commands.Fault + Spacers.Type + Commands.Acknowledge + Spacers.Module + fault.Module.Hash + Spacers.Fault + fault.Index;
		public static string GetStringResetFault(IFault fault) => Commands.Authenticate + Spacers.Main + Commands.Fault + Spacers.Type + Commands.Reset + Spacers.Module + fault.Module.Hash + Spacers.Fault + fault.Index;
		public static string GetStringRaiseFault(IFault fault) => Commands.Authenticate + Spacers.Main + Commands.Fault + Spacers.Type + Commands.Raise + Spacers.Module + fault.Module.Hash + Spacers.Fault + fault.Index;

		public static string GetStringSetRelay(Relay relay) => Commands.Authenticate + Spacers.Main + Commands.Relay + Spacers.Type + Commands.Set + Spacers.Module + relay.Module.Hash + Spacers.Fault + relay.Index;
		public static string GetStringResetRelay(Relay relay) => Commands.Authenticate + Spacers.Main + Commands.Relay + Spacers.Type + Commands.Reset + Spacers.Module + relay.Module.Hash + Spacers.Fault + relay.Index;

		public static string GetStringRemoveAllIcons() => Commands.Authenticate + Spacers.Main + Commands.Utility + Spacers.Type + Commands.RemoveIcons;		
		public static string GetStringAcknowledgeAllFaults() => Commands.Authenticate + Spacers.Main + Commands.Utility + Spacers.Type + Commands.AcknowledgeAll;
		public static string GetStringResetAllFaults() => Commands.Authenticate + Spacers.Main + Commands.Utility + Spacers.Type + Commands.ResetAll;
		public static string GetStringInitializeAllModules() => Commands.Authenticate + Spacers.Main + Commands.Utility + Spacers.Type + Commands.InitializeAllAnalyzers;
		public static string GetStringInitializeAllCams() => Commands.Authenticate + Spacers.Main + Commands.Utility + Spacers.Type + Commands.InitializeAllCameras;
		public static string GetStringAuthenticateSetAlarms() => Commands.Authenticate + Spacers.Main + Commands.Utility + Spacers.Type + Commands.SetAlarms;
		public static string GetStringAuthenticateResetAlarms() => Commands.Authenticate + Spacers.Main + Commands.Utility + Spacers.Type + Commands.ResetAlarms;
		public static string GetStringGetReport(string from, string to) => Commands.Authenticate + Spacers.Main + Commands.Utility + Spacers.Type + Commands.ResetAlarms;

		public abstract Task<bool?> AuthenticationResult(string message);
		public virtual Task<(bool?, string)> GetResult(string message) => TaskEx.FromResult(((bool?)false, ""));

		public async Task<bool?> AuthenticateSettings(string data) => await AuthenticationResult(GetStringSettings(data)).ConfigureAwait(false);
		public async Task<bool?> AuthenticateModifyModule(IModule module) => await AuthenticationResult(GetStringModifyModule(module)).ConfigureAwait(false);
		public async Task<bool?> AuthenticateAddModule(IModule module) => await AuthenticationResult(GetStringAddModule(module)).ConfigureAwait(false);
		public async Task<bool?> AuthenticateDeleteModule(IModule module) => await AuthenticationResult(GetStringDeleteModule(module)).ConfigureAwait(false);

		public abstract Task<bool?> AuthenticateCheckModule(IModule module);

		public abstract Task<bool?> AuthenticateAcknowledgeFault(IFault fault);
		public abstract Task<bool?> AuthenticateResetFault(IFault fault);
		public abstract Task<bool?> AuthenticateRaiseFault(IFault fault);

		public abstract Task<bool?> AuthenticateSetRelay(Relay relay);
		public abstract Task<bool?> AuthenticateResetRelay(Relay relay);

		public abstract Task<bool?> AuthenticateRemoveAllIcons();

		public abstract Task<bool?> AuthenticateAcknowledgeAllFaults();
		public abstract Task<bool?> AuthenticateResetAllFaults();

		public abstract Task<bool?> AuthenticateInitializeAllModules();
		public abstract Task<bool?> AuthenticateInitializeAllCams();

		public abstract Task<bool?> AuthenticateSetAlarms();
		public abstract Task<bool?> AuthenticateResetAlarms();

		public virtual Task<(bool?, string)> GetReport(string from, string to) => TaskEx.FromResult(((bool?)false, ""));

		public void TransmitModuleStatistics(IModule module) => Send(Commands.Update + Spacers.Main + Commands.Statistics + Spacers.Type + module.Hash + Spacers.ModuleType + module.GetStringStatistics());
		public void TransmitModuleInitialization(IModule module) => Send(Commands.Update + Spacers.Main + Commands.Initialization + Spacers.Type + module.Hash + Spacers.ModuleType + (module.Initialized ? Commands.Success : Commands.Failed));

		public void TransmitFaults()
		{
			string temp = "";

			foreach (var fault in Plant.RaisedFaults)
				temp += fault.Module.Hash + Spacers.ModuleFault + fault.Index + Spacers.FaultFault;

			Send(Commands.Update + Spacers.Main + Commands.Fault + Spacers.Type + temp);
		}
	}
}