using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using AIMS3.BackEnd.Cryptography;
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Modules.Interfaces;
using AIMS3.FrontEnd.Site.Map;

using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.Modules.Connection;
using static AIMS3.BackEnd.Modules.ElectroFence;
using static AIMS3.BackEnd.Modules.Fault;
using static AIMS3.BackEnd.Modules.Module;

namespace AIMS3.BackEnd.Modules
{
	public interface IModule
	{
		List<IModule> Collection { get; }
		List<IModule> Modules { get; }
		IModuleView View { get; }
		ModuleType Type { get; }
		ModuleConnectionType ConnectionType { get; set; }
		ConverterModule ConverterModule { get; set; }
		TCPConnection TCP { get; set; }
		ModuleState State { get; set; }
		ModuleZonesType ZoneType { get; set; }

		int Address { get; set; }
		int Index { get; set; }
		string PreName { get; set; }
		string Name { get; }
		string _Name { get; }
		string IP { get; set; }
		int Port { get; set; }
		string EndPoint { get; }
		bool Encrypted { get; set; }
		string Hash { get; }
		
		bool Initialized { get; set; }
		bool InitializeNeeded { get; set; }
		bool Changed { get; set; }
		bool IsSelected { get; set; }

		int RelayCount { get; }
		int ZoneCount { get; }
		int FaultCount { get; }
		int StatusCount { get; }
		int WarningCount { get; }

		string Temperature { get; set; }

		Plant Plant { get; }
		ITelemetricSite Site { get; }

		List<IFault> Faults { get; }
		List<IFault> Zones { get; }
		IFault Zone1 { get; }
		IFault Zone2 { get; }
		SOSFault SOS { get; }
		List<Relay> Relays { get; }
		List<IActuator> Actuators { get; }
		List<Spot> Icons { get; }

		bool Check(int repeat = 2);
		bool Initialize(int repeat = 1);
		bool[] ReadStatus();
		bool SetRelay(int index);
		bool ResetRelay(int index);

		void Probe();
		string GetString();
		bool Upload(string str);
		string GetStringStatistics();
		bool UploadStatistics(string data);
		void Delete();

		void GetNewIndex();
		void GetNewAddress();
		void InitializeView();
		void EvaluateRelays();

		void RefreshIcons();
		void RefreshIconsText();
		void RefreshIconsInitialization();
		void RefreshIconsSuccess();
		void RefreshIconsFailed();
		void ResetIconsColor();

		void CopyZones(int index);
		void GenerateHash();
	}

	public abstract class Module : IModule, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public abstract List<IModule> Collection { get; }
		public List<IModule> Modules => Plant.Analyzers;
		public enum ModuleType { EF, FG, ACU, Cam };
		public abstract ModuleType Type { get; }
		public IModuleView View { get; protected set; }
		public ModuleState State { get; set; }

		public enum ModuleState { New, Deleted };
		public ModuleZonesType ZoneType { get; set; }

		private string StringData { get; set; }

		public int Index { get; set; }
		public int Address { get; set; }
		public string PreName { get; set; }
		public string Name { get => PreName + Index; }
		public string _Name => (Collection.Contains(this) ? "" : "*") + Name;

		public virtual string IP { get; set; }
		public virtual int Port { get; set; } = 513;
		public string EndPoint { get => IP + ":" + Port; }
		public string _EndPoint { get => ConnectionType == ModuleConnectionType.TCP ? $", {IP}:{Port}" : ""; }
		public bool Encrypted { get; set; }
		public string Hash { get; private set; }

		public string Temperature { get; set; } = "-";

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

		public int ConnectionErrorCount { get; set; } = 0;
		private int MaxConnectionError => Plant.MaxConnectionError;

		public abstract int RelayCount { get; }
		public abstract int ZoneCount { get; }
		public int FaultCount => ZoneCount + 1;
		public abstract int StatusCount { get; }
		public abstract int WarningCount { get; }

		public List<IFault> Faults { get; } = new List<IFault>();
		public List<IFault> Zones => Faults.GetRange(0, ZoneCount);
		public IFault Zone1 => ZoneCount > 0 ? Faults[0] : null;
		public IFault Zone2 => ZoneCount > 1 ? Faults[1] : null;
		public SOSFault SOS => Faults[ZoneCount] as SOSFault;
		public List<Relay> Relays { get; } = new List<Relay>();
		public List<IActuator> Actuators => Faults.Select(fault => fault as IActuator).Concat(Relays.Select(relay => relay as IActuator)).ToList();
		public virtual List<Spot> Icons => Faults.Select(fault => fault.Icon).Concat(Relays.Select(relay => relay.Icon)).ToList();

		public string Zone1String => Zone1.Name;
		public string Zone2String => Zone2.Name;

		public bool Initialized { get; set; } = false;
		public bool InitializeNeeded { get; set; } = false;
		public bool Changed { get; set; } = true;

		private bool isSelected;
		public bool IsSelected
		{
			get => isSelected;
			set
			{
				isSelected = value;
				OnPropertyChanged();
			}
		}

		public ModuleConnectionType ConnectionType { get; set; }

		public ConverterModule ConverterModule { get; set; }
		public TCPConnection TCP { get; set; }
		public static SerialConnection Serial { get; set; } = new SerialConnection();

		private int delay = 0;
		public int Delay { get { if (ConnectionType == ModuleConnectionType.Serial) return delay + Plant.SerialDelayBias; else return delay + Plant.TCPDelayBias; } set => delay = value; }

		private bool Connected { get => ConnectionType == ModuleConnectionType.Serial ? SerialConnection.Connected : TCP.Connected; }
		public int ErrorCode { get; set; }

		internal object[] Status = new object[0];
		public List<Caution> Warning { get; } = new List<Caution>();

		public virtual Command CommandCheck => new Command() { Request = "AT+" + PreName, Respond = "OK", Timeout = 50, Delay = 0 };
		public virtual Command CommandReset => new Command() { Request = "AT+" + PreName + "RST", Respond = "OKR", Timeout = 50, Delay = 100 };
		public Command CommandTurnOn => new Command() { Request = "AT+" + PreName + "ON", Respond = "OKON", Timeout = 100, Delay = 0 };
		public Command CommandTurnOff => new Command() { Request = "AT+" + PreName + "OFF", Respond = "OKOFF", Timeout = 100, Delay = 0 };
		public virtual Command CommandSetOutput => new Command() { Request = "AT+" + PreName + "SOUT", Respond = "OKSO", Timeout = 75, Delay = 0 };
		public virtual Command CommandResetOutput => new Command() { Request = "AT+" + PreName + "ROUT", Respond = "OKRO", Timeout = 75, Delay = 0 };
		public Command CommandUART => new Command() { Request = "AT+" + PreName + "UART", Respond = "Res", Timeout = 50, Delay = 0 };
		public Command CommandFault => new Command() { Request = "AT+" + PreName + "FS", Respond = "OK", Timeout = 100, Delay = 0 };
		public Command CommandError => new Command() { Request = "AT+" + PreName + "ERR", Respond = "Faults", Timeout = 75, Delay = 0 };
		public virtual Command CommandStatus => new Command() { Request = "AT+" + PreName + "FSERR", Respond = "OK", Timeout = 100, Delay = 5 };
		public Command CommandVersion => new Command() { Request = "AT+" + PreName + "GMR", Respond = "Res", Timeout = 50, Delay = 0 };

		public Module() { }

		public void OnPropertyChanged([CallerMemberName]string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		public void RefreshIcons()
		{
			foreach (var icon in Icons)
				if (icon.IsPlaced)
					Application.Current.Dispatcher.BeginInvoke(new Action(() => { icon.Refresh(Initialized); }), null);
		}

		public void RefreshIconsText()
		{
			foreach (var icon in Icons)
				if (icon.IsPlaced)
					Application.Current.Dispatcher.BeginInvoke(new Action(() => { icon.RefreshText(Initialized); }), null);
		}

		public void RefreshIconsInitialization()
		{
			if (Initialized)
				RefreshIconsSuccess();

			else
				RefreshIconsFailed();
		}

		public void RefreshIconsSuccess()
		{
			foreach (var icon in Icons)
				if (icon.IsPlaced)
					Application.Current.Dispatcher.BeginInvoke(new Action(() => { icon.InitializationRefresh(); }), null);
		}

		public void RefreshIconsFailed()
		{
			foreach (var icon in Icons)
				if (icon.IsPlaced)
					Application.Current.Dispatcher.BeginInvoke(new Action(() => { icon.FailedRefresh(); }), null);
		}

		public void ResetIconsColor()
		{
			foreach (var icon in Icons)
				if (icon.IsPlaced)
					Application.Current.Dispatcher.BeginInvoke(new Action(() => { icon.ResetColors(); }), null);
		}

		public void SetTimeout(int timeout)
		{
			if (ConnectionType == ModuleConnectionType.Serial)
				SerialConnection.SetTimeout(timeout + Plant.SerialTimoutBias);

			else
				TCP.Timeout = timeout + Plant.TCPTimoutBias;
		}

		public bool Write(string command, params object[] parameters)
		{
			command += "," + Address;

			foreach (object parameter in parameters)
				command += "," + parameter;

			if (Encrypted)
				command = AIB64.Encode(command);

			try
			{
				if (ConnectionType == ModuleConnectionType.Serial)
					return Serial.Write(command);

				if (ConnectionType == ModuleConnectionType.TCP)
					return TCP.Write(command);
			}
			catch (Exception ex) { }
			return false;
		}

		public string Read()
		{
			string data = "";

			if (!Connected)
				return "";

			if (ConnectionType == ModuleConnectionType.Serial)
				data = Serial.Read();

			else if (ConnectionType == ModuleConnectionType.TCP)
				data = TCP.Read();

			if (Encrypted)
				data = AIB64.Decode(data);

			return data;
		}

		public void GetNewIndex()
		{
			int i = 1;

			foreach (IModule module in Collection)
			{
				if (module.Index != i++)
				{
					Index = i - 1;
					return;
				}
			}

			Index = i;
		}

		public int IncIndex()
		{
			int index = Index + 1;
			int address = Index;

			while (index < Collection.Count)
			{
				if (address++ != Collection[index++].Index)
					return address - 1;
			}

			return address + ((address == Index) ? 1 : 0);
		}

		public int DecIndex()
		{
			int index = Index - 1;
			int address = Index;

			while (index >= 0)
			{
				if (address-- != Collection[index++].Index)
					return address + 1;
			}

			return Math.Max((address - ((address == Index) ? 1 : 0)), 0);
		}

		public void GetNewAddress()
		{
			if (Collection.FindIndex(module => module.Address == Index) < 0)
			{
				Address = Index;
				return;
			}

			int i = 1;

			foreach (IModule module in Collection)
			{
				if (module.Address != i++)
				{
					Address = i - 1;
					return;
				}
			}

			Address = i;
		}

		public int IncAddress()
		{
			int index = Index + 1;
			int address = Address;

			while (index < Collection.Count)
			{
				if (address++ != Collection[index++].Address)
					return address - 1;
			}

			return address + ((address == Address) ? 1 : 0);
		}

		public int DecAddress()
		{
			int index = Index - 1;
			int address = Address;

			while (index >= 0)
			{
				if (address-- != Collection[index++].Address)
					return address + 1;
			}

			return Math.Max((address - ((address == Address) ? 1 : 0)), 0);
		}

		public bool Check()
		{
			string data;

			try
			{
				if (!Write(CommandCheck.Request))
					return false;

				SetTimeout(CommandCheck.Timeout);
				Delay = CommandCheck.Delay;
				data = Read();

				if (data == CommandCheck.Respond + ",[" + Address + "]")
					return true;
			}
			catch (Exception ex) { }
			finally { Thread.Sleep(Delay); }

			return false;
		}

		public bool Reset()
		{
			string data;

			try
			{
				Write(CommandReset.Request);
				SetTimeout(CommandReset.Timeout);
				Delay = CommandReset.Delay;
				data = Read();

				if (data == CommandReset.Respond + ",[" + Address + "]")
					return true;
			}
			catch (Exception ex) { }
			finally { Thread.Sleep(Delay); }

			return false;
		}

		public bool SetRelay(int index)
		{
			string data;

			try
			{
				Write(CommandSetOutput.Request, index);
				SetTimeout(CommandSetOutput.Timeout);
				Delay = CommandResetOutput.Delay;
				data = Read();

				if (data != CommandSetOutput.Respond + "-" + (index) + ",[" + Address + "]")
					return !(Relays[index].SetNeeded = !false);

				Relays[index].Icon.Raise();
				return !(Relays[index].SetNeeded = !true);
			}
			catch (Exception ex) { return !(Relays[index].SetNeeded = !false); }
			finally { Thread.Sleep(Delay); }
		}

		public bool ResetRelay(int index)
		{
			string data;

			try
			{
				Write(CommandResetOutput.Request, index);
				SetTimeout(CommandResetOutput.Timeout);
				Delay = CommandResetOutput.Delay;
				data = Read();

				if (data != CommandResetOutput.Respond + "-" + (index) + ",[" + Address + "]")
					return !(Relays[index].ResetNeeded = !false);

				Relays[index].Icon.TurnOff();
				return !(Relays[index].ResetNeeded = !true);
			}
			catch (Exception ex) { return !(Relays[index].ResetNeeded = !false); }
			finally { Thread.Sleep(Delay); }
		}

		public bool Check(int repeat = 2) => ConverterModule?.CheckModule(this, repeat) == true;

		public virtual bool Initialize(int repeat = 1)
		{
			RefreshIconsInitialization();
			Site.TransmitModuleInitialization(this);
			return true;
		}
		public abstract bool[] ReadStatus();

		public void Probe()
		{
			bool connection;
			bool[] fault = new bool[FaultCount];

			try
			{
				if (!Initialized || InitializeNeeded)
					Initialize();

				if (Initialized)
					fault = ReadStatus();

				else
					Status = new object[1];

				if (Status.Length != StatusCount || !Initialized)
				{
					ConnectionErrorCount = Math.Min(++ConnectionErrorCount, MaxConnectionError);
					Initialized = false;
				}

				else
					ConnectionErrorCount = 0;

				connection = true;

				if (ConnectionErrorCount == MaxConnectionError)
				{
					Initialized = false;

					if (!SOS.Raised)
						SOS.Raise(ResetStatus.Local);

					if (SOS.Raised && !Plant.RaisedFaults.Contains(SOS))
						Plant.RaisedFaults.Add(SOS);
				}

				else
					connection = false;

				if (ZoneCount == 0)
					fault = new bool[1] { false };

				fault[ZoneCount] |= connection;

				for (int j = 0; j < RelayCount && !connection; j++)
				{
					if (Relays[j].SetNeeded)
						Relays[j].Set();

					else if (Relays[j].ResetNeeded)
						Relays[j].Reset();
				}

				for (int j = 0; j < FaultCount; j++)
				{
					if (connection)
					{
						Faults[j].StatusWindow?.Evaluate();

						if (Faults[j].RaiseNeeded)
							if (!Faults[j].Raised)
								Faults[j].Raise(ResetStatus.Local);

						continue;
					}

					if (Faults[j].ResetNeeded)
						Faults[j].Reset(ResetStatus.Local, true);

					if (Faults[j].Raised && !Plant.RaisedFaults.Contains(Faults[j]))
						Plant.RaisedFaults.Add(Faults[j]);

					if (!Faults[j].Enabled)
					{
						Plant.RaisedFaults.Remove(Faults[j]);
						Faults[j].StatusWindow?.Evaluate();
						continue;
					}

					if (Faults[j].RaiseNeeded || fault[j])
					{
						if (!Faults[j].Raised)
							Faults[j].Raise(ResetStatus.Local);
					}

					else if (Faults[j].Raised)
					{
						if (Faults[j].AutomaticReset)
						{
							Plant.RaisedFaults.Remove(Faults[j]);
							Faults[j].Reset(ResetStatus.Automatic, true);
						}

						else
							Faults[j].Obviate();
					}

					else if (!Faults[j].Enabled)
					{
						Plant.RaisedFaults.Remove(Faults[j]);

						if (Plant.RaisedFaults.Count == 0)
							Sound.Stop();
					}

					Faults[j].StatusWindow?.Evaluate();
				}

				for (int j = 0; j < RelayCount && !connection; j++)
				{
					if (Relays[j].SetNeeded)
						Relays[j].Set();

					else if (Relays[j].ResetNeeded)
						Relays[j].Reset();
				}
			}
			catch (Exception ex) { }
		}

		public abstract void InitializeView();

		public override string ToString() => Name;

		public virtual string GetString()
		{
			if (!string.IsNullOrEmpty(StringData) && Changed == false)
				return StringData;

			string data = string.Format("{0},{1},{2},{3},{4},{5},{6}\r\n", Hash, ConnectionType, Encrypted, Index, Address, IP, Port);

			foreach (Relay relay in Relays)
				data += relay.GetString() + ";";

			if (Relays.Count > 0)
				data = data.Remove(data.Length - 1, 1);

			data += "\r\n";

			foreach (IFault fault in Faults)
				data += fault.GetString() + ";";

			if (Faults.Count > 0)
				data = data.Remove(data.Length - 1, 1);

			Changed = false;
			//StringData = data;
			return data;
		}

		public string GetStringStatistics()
		{
			string output = $"{Temperature},{Initialized}>";

			foreach (var actuator in Actuators)
				output += actuator.GetStringStatistics() + "_";

			return output;
		}

		public bool UploadStatistics(string data)
		{
			try
			{
				var pars = data.Split('>');
				var main = pars[0].Split(',');
				var index = 0;

				Temperature = main[index++];
				Initialized = bool.Parse(main[index++]);

				pars = pars[1].Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
				var actuators = Actuators;

				for (int i = 0; i < pars.Length; i++)
					actuators[i].UploadStatistics(pars[i]);

				return true;
			}
			catch (Exception ex) { WriteToDebug(typeof(Module), Name, nameof(UploadStatistics), ex); }
			return false;
		}

		public bool Upload(string data)
		{
			try
			{
				string[] faults = Parse(data).Split(';');

				for (int i = 0; i < ZoneCount; i++)
					Faults[i].Upload(faults[i]);

				SOS?.Upload(faults[ZoneCount]);

				return true;
			}
			catch (Exception ex) { WriteToDebug(typeof(Module), Name, nameof(Upload), ex); }
			return false;
		}

		public string Parse(string data)
		{
			try
			{
				int index = 0, indexMain = 0;
				string[] strs = data.Split(new string[] { "\r\n" }, StringSplitOptions.None);
				string[] main = strs[index++].Split(',');

				Hash = main[indexMain++];
				ConnectionType = (Connection.ModuleConnectionType)Enum.Parse(typeof(Connection.ModuleConnectionType), main[indexMain++]);
				Encrypted = bool.Parse(main[indexMain++]);
				Index = int.Parse(main[indexMain++]);
				Address = int.Parse(main[indexMain++]);
				IP = main[indexMain++];
				Port = int.Parse(main[indexMain++]);

				string[] relays = strs[index++].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

				if (Relays.Count != RelayCount)
				{
					Relays.Clear();

					for (int i = 0; i < relays.Length; i++)
						Relays.Add(Relay.Parse(relays[i], this, i));
				}

				else
					for (int i = 0; i < RelayCount; i++)
						Relays[i].Upload(relays[i]);

				return strs[index++];
			}
			catch (Exception ex) { }
			return "";
		}

		public void Delete()
		{
			foreach (var icon in Icons)
				Application.Current.Dispatcher.Invoke(new Action(() => { icon?.Remove(false); }), null);

			State = ModuleState.Deleted;
		}

		public void EvaluateRelays()
		{
			foreach (IFault fault in Faults)
				fault.EvaluateRelays();
		}

		public virtual void CopyZones(int index) { }

		static Random random = new Random();
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
		public void GenerateHash()
		{
			string temp;

			do
				temp = new string(Enumerable.Repeat(chars, random.Next(5, 15)).Select(s => s[random.Next(s.Length)]).ToArray());
			while (Modules.FindIndex(module => module.Hash == temp) >= 0);

			Hash = temp;
		}
	}

	public interface IIcon
	{
		string Name { get; }
		Spot Icon { get; }
		Plant Plant { get; }
		ITelemetricSite Site { get; }
		IModule Module { get; }

		bool Enabled { get; set; }
		bool RaiseNeeded { get; set; }
		bool Raised { get; }

		string PreName { get; }
		string IconText { get; }

		void PopUpStatus();
		void PopUpSettings();

        string GetImageSuffix(int index);
    }

	public class Command
	{
		public string Request { get; set; }
		public string Respond { get; set; }
		public int Timeout { get; set; }
		public int Delay { get; set; }
	}

	public class Caution
	{
		public bool Raised { get; set; }
		public string Name { get; set; }
		public string Status { get; set; }
	}
}