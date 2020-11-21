using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Modules.EF;

using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.DataBase;

namespace AIMS3.BackEnd.Modules
{
	public class ElectroFence : Module
	{
		public override List<IModule> Collection => Plant.EF;
		public override ModuleType Type => ModuleType.EF;

		new EFFault Zone1 => Faults[0] as EFFault;
		new EFFault Zone2 => Faults[1] as EFFault;

		public override int RelayCount => 4;
		public override int ZoneCount => 2;
		public override int StatusCount => 15;
		public override int WarningCount => 10;

		private string[] Voltage = new string[] { "-", "-" };

        public static List<string> ModuleZonesTypeString { get; } = new List<string> { "Double", "Common", "Uni" };
		public static List<string> Warnings => new List<string> { "HV Supply", "Charge HV1", "Charge HV2", "Discharge HV1", "Discharge HV2", "Main Supply", "Battery Charge", "Battery Low", "Battery", "Connection" };

		public enum ModuleZonesType { TwoZones, TwoZonesCommon, OneZone }

        public int EFTypeIndex { get => (int)ZoneType; set => ZoneType = (ModuleZonesType)value; }

		public Command CommandPower => new Command() { Request = "AT+" + PreName + "POW", Respond = "OKP", Timeout = 50, Delay = 5 };
		public Command CommandThreshold => new Command() { Request = "AT+" + PreName + "ALRS", Respond = "OKA", Timeout = 50, Delay = 1 };
		public Command CommandVoltage => new Command() { Request = "AT+" + PreName + "VOL", Respond = "OK", Timeout = 50, Delay = 5 };

		public ElectroFence() : base()
		{
			PreName = "EF";
			ZoneType = ModuleZonesType.TwoZonesCommon;

			Relays.Clear();
			Faults.Clear();

			Warning.Clear();

			for (byte i = 0; i < WarningCount; i++)
			{
				Warning.Add(new Caution());
				Warning[i].Raised = false;
				Warning[i].Name = Warnings[i];

				if (i == 6)
					Warning[i].Status = Application.Current.TryFindResource("Disconnected") as string;

				else if (i == 9)
					Warning[i].Status = Application.Current.TryFindResource("Connectedy") as string;

				else
					Warning[i].Status = Application.Current.TryFindResource("Healthy") as string;
			}
		}

		public ElectroFence(Plant plant) : base()
		{
			Plant = plant;
			PreName = "EF";
			ZoneType = ModuleZonesType.TwoZonesCommon;

			GetNewAddress();

			Relays.Clear();
			Faults.Clear();

			for (byte i = 0; i < RelayCount; i++)
				Relays.Add(new Relay(this, i));

			for (byte i = 0; i < ZoneCount; i++)
				Faults.Add(new EFFault(this, i));

			Faults.Add(new SOSFault(this, ZoneCount));

			Warning.Clear();

			for (byte i = 0; i < WarningCount; i++)
			{
				Warning.Add(new Caution());
				Warning[i].Raised = false;
				Warning[i].Name = Warnings[i];

				if (i == 6)
					Warning[i].Status = Application.Current.TryFindResource("Disconnected") as string;

				else if (i == 9)
					Warning[i].Status = Application.Current.TryFindResource("Connectedy") as string;

				else
					Warning[i].Status = Application.Current.TryFindResource("Healthy") as string;
			}
		}

        public override bool Initialize(int repeat = 1)
        {
			bool done;
            Initialized = false;

			while (repeat-- > 0)
			{
				try
				{
					done = true;

					if (!Check())
					{
						ErrorCode = 1;
						continue;
					}

					if (!Reset())
					{
						ErrorCode = 2;
						continue;
					}

					if (!TurnOn())
					{
						ErrorCode = 3;
						continue;
					}

					if (!ChangePower())
					{
						ErrorCode = 4;
						continue;
					}

					if (!ChangeThreshold())
					{
						ErrorCode = 5;
						continue;
					}

					for (byte i = 0; i < RelayCount; i++)
					{
						if (!Relays[i].Reset())
						{
							done = false;
							ErrorCode = 6 + i;
							break;
						}
					}

					if (!done)
						continue;

					ErrorCode = -1;
					Initialized = true;
					InitializeNeeded = false;
					return true;
				}
				catch (Exception ex) { }
				finally { base.Initialize(); }
			}

			ErrorCode = 0;
			return false;
		}

		public bool TurnOn()
		{
			string data;

			try
			{
				if (ZoneType == ModuleZonesType.TwoZonesCommon || ZoneType == ModuleZonesType.TwoZones)
				{
					data = ((Zone1.HV && Zone1.Enabled) ? "1," : "0,") + ((Zone2.HV && Zone2.Enabled) ? "1," : "0,") + ((Zone1.LV && Zone1.Enabled) ? "1," : "0,") + ((Zone2.LV && Zone2.Enabled) ? "1" : "0");

					Write(CommandTurnOn.Request, data);
					SetTimeout(CommandTurnOn.Timeout);
					Delay = CommandTurnOn.Delay;
					data = Read();

					if (data.Contains(CommandTurnOn.Respond + ",[" + Address + "]"))
						return true;
				}
			}
			catch (Exception ex) { }
			finally { Thread.Sleep(Delay); }

			return false;
		}

		public bool ChangePower()
        {
			string data;

			try
			{
				if (ZoneType == ModuleZonesType.TwoZonesCommon || ZoneType == ModuleZonesType.TwoZones)
				{
					Write(CommandPower.Request, Zone1.Power);
					SetTimeout(CommandPower.Timeout);
					Delay = CommandPower.Delay;
					data = Read();

					if (data == CommandPower.Respond + "-" + Zone1.Power + ",[" + Address + "]")
						return true;
				}
			}
			catch (Exception ex) { }
			finally { Thread.Sleep(Delay); }

			return false;
		}

        public bool ChangeThreshold()
        {
			string data;

			try
			{
				if (ZoneType == ModuleZonesType.TwoZonesCommon || ZoneType == ModuleZonesType.TwoZones)
				{
					Write(CommandThreshold.Request, Math.Min(Zone1.Threshold, 5999), Zone1.Repeat);
					SetTimeout(CommandThreshold.Timeout);
					Delay = CommandThreshold.Delay;
					data = Read();

					if (data == CommandThreshold.Respond + ",[" + Address + "]")
						return true;
				}
			}
			catch (Exception ex) { }
			finally { Thread.Sleep(Delay); }

			return false;
		}

        public void GetStatistics()
        {
			string data;

			try
			{
				Write(CommandVoltage.Request);
				SetTimeout(CommandStatus.Timeout);
				Delay = CommandStatus.Delay;

				data = Read();

				var pars = data.Remove(data.LastIndexOf(',')).Remove(0, data.IndexOf(',') + 1).Split(',');

				Voltage[0] = pars[0];
				Voltage[1] = pars[1];
			}
			finally { Thread.Sleep(Delay); }
		}

		public override bool[] ReadStatus()
		{
			string[] strs;
			string data;
			bool[] fault = new bool[FaultCount];

			try
			{
				Status = new object[1];

				Write(CommandStatus.Request);
				SetTimeout(CommandStatus.Timeout);
				Delay = CommandStatus.Delay;

				data = Read();
				strs = data.Replace(",[" + Address + "]", "").Replace(CommandStatus.Respond + ",", "").Split(',');
				Status = new object[strs.Length];

				for (int i = 0; i < (strs.Length - 1); i++)
					Status[i] = strs[i] == "1";

				Status[Status.Length - 1] = int.Parse(strs[strs.Length - 1]);
				Thread.Sleep(Delay);

				GetStatistics();
				Zone1.Voltage = Voltage[0];
				Zone2.Voltage = Voltage[1];
				Temperature = (string)Status[Status.Length - 1];
				RefreshIconsText();

				if (Status.Length == StatusCount)
				{
					Zone1.HVRaised = (bool)Status[0] && Zone1.HV;
					Zone1.LVRaised = (bool)Status[2] && Zone1.LV;
					Zone2.HVRaised = (bool)Status[1] && Zone2.HV;
					Zone2.LVRaised = (bool)Status[3] && Zone2.LV;
					SOS.TamperRaised = (bool)Status[4] && SOS.Tamper;

					fault = new bool[3]
					{
					Zone1.HVRaised || Zone1.LVRaised,
					Zone2.HVRaised || Zone2.LVRaised,
					SOS.TamperRaised
					};
				}

				else
				{
					Zone1.HVRaised = false;
					Zone1.LVRaised = false;
					Zone2.HVRaised = false;
					Zone2.LVRaised = false;
					SOS.TamperRaised = false;

					fault = new bool[3]
					{
					false,
					false,
					false
					};
				}
			}
			catch (Exception ex) { }
			finally {  }
			return fault;
		}

		public bool EvaluateWarnings(object[] status)
		{
			bool output = false;
			string stringError = (string)Application.Current.TryFindResource("Error");
			string stringOK = (string)Application.Current.TryFindResource("Healthy");
			string stringFault = (string)Application.Current.TryFindResource("Fault");
			string stringDisconnectoin = (string)Application.Current.TryFindResource("Disonnected");
			string stringConnectoin = (string)Application.Current.TryFindResource("Connection");

			for (int i = 0; i < WarningCount - 1; i++)
			{
				if (Warning[9].Raised)
				{
					Warning[i].Status = stringError;
					//output = true;
				}

				else if ((bool)status[i + 5])
				{
					Warning[i].Raised = true;

					if (i == 6)
						Warning[i].Status = (string)Application.Current.TryFindResource("Charging");

					else
					{
						Warning[i].Status = stringFault;
						output = true;
					}
				}

				else
				{
					Warning[i].Raised = false;

					if (i == 6)
						Warning[i].Status = (string)Application.Current.TryFindResource("NotCharging");

					else
						Warning[i].Status = stringOK;
				}
			}

			Warning[9].Status = !Warning[9].Raised ? (string)Application.Current.TryFindResource("Connectedy") : (string)Application.Current.TryFindResource("Disconnected");

			return output;
		}

		public override void InitializeView() => Application.Current.Dispatcher.Invoke(new Action(() => { View = new EFView(this); }));

		public static ElectroFence Parse(Plant plant, string data)
        {
			try
			{
				ElectroFence ef = new ElectroFence() { Plant = plant };
				string[] faults = ef.Parse(data).Split(';');

				for (int i = 0; i < ef.ZoneCount; i++)
					ef.Faults.Add(EFFault.Parse(faults[i], ef, i));

				ef.Faults.Add(SOSFault.Parse(faults[ef.ZoneCount], ef, ef.ZoneCount));
				
                return ef;
            }
            catch (Exception ex) { }
            return null;
        }

		public override void CopyZones(int index)
		{
			int index_ = (index + 1) % 2;

			(Faults[index_] as EFFault).Power = (Faults[index] as EFFault).Power;
			(Faults[index_] as EFFault).Threshold = (Faults[index] as EFFault).Threshold;
			(Faults[index_] as EFFault).Repeat = (Faults[index] as EFFault).Repeat;
		}
	}

	public class EFFault : Fault
	{
		public bool HV { get; set; } = true;
		public bool LV { get; set; }
		public string Voltage { get; set; } = "-";

		public bool HVRaised { get; set; }
		public bool LVRaised { get; set; }

		public int Power { get; set; } = 70;
		public int Threshold { get; set; } = 3000;
		public int Repeat { get; set; } = 2;

		public override string FaultListImagePath
		{
			get
			{
				if (Reason == FaultReason.HV || Reason == FaultReason.LV)
					return faultListImagePath = baseFaultListImagePath + Reason + Extentions.FaultListImage;

				return base.FaultListImagePath;
			}
		}

		public override string IconText => Name
			+ (Plant.MapShowStatistics && IsZone ? string.Format("\r\n{0}V", Voltage) : "")
			+ (Plant.MapShowTemperature ? string.Format("\r\n{0}°C", Module.Temperature) : "");
		public override string GetString() => string.Format("{0},{1}_{2}_{3}_{4}_{5}", base.GetString(), HV, LV, Power, Threshold, Repeat);
		public override void InitializeView() => PopUp = new EFPopUp(this);
		public override void InitializeStatus() => StatusWindow = new EFFaultStatus(this);

		public EFFault(IModule owner, int index) : base(owner, index) { }

		public override FaultReason SetReason()
		{
			if (RaiseNeeded)
				return Reason = FaultReason.Test;

			if (!IsZone)
			{
				if (TamperRaised)
					return Reason = FaultReason.Tamper;

				return Reason = FaultReason.Connection;
			}

			if (HVRaised)
				return Reason = FaultReason.HV;

			else if (LVRaised)
				return Reason = FaultReason.LV;

			else
				return Reason = FaultReason.Test;
		}

		public override bool Upload(string str)
		{
			int index = 0;

			try
			{
				string[] data = Parse(str).Split('_');

				HV = bool.Parse(data[index++]);
				LV = bool.Parse(data[index++]);
				Power = int.Parse(data[index++]);
				Threshold = int.Parse(data[index++]);
				Repeat = int.Parse(data[index++]);

				return true;
			}
			catch (Exception ex) { WriteToDebug(typeof(EFFault), Name, nameof(Upload), ex); }
			return false;
		}

		public static EFFault Parse(string str, IModule owner, int zoneIndex)
		{
			EFFault fault = new EFFault(owner, zoneIndex);
			fault.Upload(str);
			return fault;
		}

		public override string GetStringStatistics() => $"{base.GetStringStatistics()};{HVRaised},{LVRaised},{Voltage}";

		public override void UploadStatistics(string data)
		{
			var pars = data.Split(';');
			var index = 0;

			base.UploadStatistics(pars[0]);

			pars = pars[1].Split(',');

			HVRaised = bool.Parse(pars[index++]);
			LVRaised = bool.Parse(pars[index++]);
			Voltage = pars[index++];
		}
	}
}