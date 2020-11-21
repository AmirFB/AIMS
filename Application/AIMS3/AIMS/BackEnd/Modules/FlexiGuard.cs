using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Modules.FG;

using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.DataBase;

namespace AIMS3.BackEnd.Modules
{
	public class FlexiGuard : Module
	{
        public override List<IModule> Collection => Plant.FG;
		public override ModuleType Type => ModuleType.FG;

		new FGFault Zone1 => Faults[0] as FGFault;
		new FGFault Zone2 => Faults[1] as FGFault;

		public override int RelayCount => 2;
		public override int ZoneCount => 2;
		public override int StatusCount => 15;
		public override int WarningCount => 0;

		public Command CommandSensitivity => new Command() { Request = "AT+" + PreName + "SENS", Respond = "OK", Timeout = 300, Delay = 200 };
		public Command CommandPulse => new Command() { Request = "AT+" + PreName + "PULS", Respond = "OK", Timeout = 100, Delay = 0 };
		public Command CommandTimeWindow => new Command() { Request = "AT+" + PreName + "TIME", Respond = "OK", Timeout = 125, Delay = 0 };
		public Command CommandPreTime => new Command() { Request = "AT+" + PreName + "PTIME", Respond = "OK", Timeout = 100, Delay = 0 };
		public override Command CommandCheck => new Command() { Request = "AT+" + PreName, Respond = "OK", Timeout = 75, Delay = 0 };
		public override Command CommandReset => new Command() { Request = "AT+" + PreName + "RST", Respond = "OKR", Timeout = 75, Delay = 300 };
		public override Command CommandStatus => new Command() { Request = "AT+" + PreName + "FSERR", Respond = "OK", Timeout = 50, Delay = 0 };

		public override void InitializeView() => Application.Current.Dispatcher.Invoke(new Action(() => { View = new FGView(this); }));

		public FlexiGuard() : base()
		{
			PreName = "FG";

			Relays.Clear();
			Faults.Clear();
		}

		public FlexiGuard(Plant plant) : base()
		{
			Plant = plant;
			PreName = "FG";

			Relays.Clear();
			Faults.Clear();

			for (byte i = 0; i < RelayCount; i++)
				Relays.Add(new Relay(this, i));

			for (byte i = 0; i < ZoneCount; i++)
				Faults.Add(new FGFault(this, i));

			Faults.Add(new SOSFault(this, ZoneCount));
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

					if (!SetPulse())
					{
						ErrorCode = 4;
						continue;
					}

					if (!SetSensitivity())
					{
						ErrorCode = 5;
						continue;
					}

					if (!SetTimeWindow())
					{
						ErrorCode = 6;
						continue;
					}

					if (!SetPreTime())
					{
						ErrorCode = 7;
						continue;
					}

					for (byte i = 0; i < RelayCount; i++)
					{
						if (!Relays[i].Reset())
						{
							done = false;
							ErrorCode = 8 + i;
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
				catch (Exception ex) { ErrorCode = 0; }
				finally { base.Initialize(); }
			}

			return false;
		}

        public bool TurnOn()
        {
			string data, param = string.Format("{0},{1}", Zone1.Enabled ? "1" : "0", Zone2.Enabled ? "1" : "0");

			try
			{
				Write(CommandTurnOn.Request, param);
				SetTimeout(CommandTurnOn.Timeout);
				Delay = CommandTurnOn.Delay;
				data = Read();

				if (data == string.Format("{0},[{1}],{2}", CommandTurnOn.Respond, Address, param))
					return true;
			}
			catch (Exception ex) { }
			finally { Thread.Sleep(Delay); }

			return false;
		}

        public bool SetPulse()
        {
			string data, param = string.Format("{0},{1},{2},{3}", Zone1.CutPulses, Zone1.ClimbPulses, Zone2.CutPulses, Zone2.ClimbPulses);

			try
			{
				Write(CommandPulse.Request, param);
				SetTimeout(CommandPulse.Timeout);
				Delay = CommandPulse.Delay;
				data = Read();

				if (data == string.Format("{0},{1},[{2}]", CommandPulse.Respond, param, Address))
					return true;
			}
			catch (Exception ex) { }
			finally { Thread.Sleep(Delay); }

			return false;
        }

        public bool SetTimeWindow()
        {
			string data, param = string.Format("{0},{1},{2},{3}", Zone1.CutTimeWindow, Zone1.ClimbTimeWindow, Zone2.CutTimeWindow, Zone2.ClimbTimeWindow);

			try
			{
				Write(CommandTimeWindow.Request, param);
				SetTimeout(CommandTimeWindow.Timeout);
				Delay = CommandTimeWindow.Delay;
				data = Read();

				if (data == string.Format("{0},{1},[{2}]", CommandPulse.Respond, param, Address))
					return true;
			}
			catch (Exception ex) { }
			finally { Thread.Sleep(Delay); }

			return false;
		}

        public bool SetSensitivity()
        {
			string data, param = string.Format("{0},{1},{2},{3}", Zone1.CutSensitivity, Zone1.ClimbSensitivity, Zone2.CutSensitivity, Zone2.ClimbSensitivity);

			try
			{
				Write(CommandSensitivity.Request, param);
				SetTimeout(CommandSensitivity.Timeout);
				Delay = CommandSensitivity.Delay;
				data = Read();

				if (data == string.Format("{0},{1},[{2}]", CommandSensitivity.Respond, param, Address))
					return true;
			}
			catch (Exception ex) { }
			finally { Thread.Sleep(Delay); }

			return false;
		}

        public bool SetPreTime()
		{
			string data, param = string.Format("{0},{1}", Zone1.PreTime, Zone2.PreTime);

			try
			{
				Write(CommandPreTime.Request, param);
				SetTimeout(CommandPreTime.Timeout);
				Delay = CommandPreTime.Delay;
				data = Read();

				if (data == string.Format("{0},{1},[{2}]", CommandPulse.Respond, param, Address))
					return true;
			}
			catch (Exception ex) { }
			finally { Thread.Sleep(Delay); }

			return false;
		}

		public string GetVersion()
        {
			string data = string.Format("{0},{1}", Zone1.PreTime, Zone2.PreTime);

			try
			{
				Write(CommandPreTime.Request, data);
				SetTimeout(CommandPreTime.Timeout);
				Delay = CommandPreTime.Delay;
				data = Read();

				return data.Split(',')[0];
			}
			catch (Exception ex) { }
			finally { Thread.Sleep(Delay); }

			return "";
        }

        public override bool[] ReadStatus()
        {
			string data;
			bool[] fault = new bool[FaultCount];

			try
			{
				Status = new object[1];

				Write(CommandFault.Request);
				SetTimeout(CommandFault.Timeout);
				Delay = CommandFault.Delay;
				data = Read();

				string[] strs = data.Replace(",[" + Address + "]", "").Replace(CommandFault.Respond + ",", "").Split(',');
				Status = new object[strs.Length];

				if (strs.Length == StatusCount)
				{
					for (int i = 0; i < 7; i++)
						Status[i] = strs[i] == "1";

					Zone1.CutPulseValue = int.Parse(strs[7]);
					Zone1.ClimbPulseValue = int.Parse(strs[8]);
					Zone1.CutTimeWindowValue = double.Parse(strs[9]) / 10;
					Zone1.ClimbTimeWindowValue = double.Parse(strs[10]) / 10;
					Zone2.CutPulseValue = int.Parse(strs[11]);
					Zone2.ClimbPulseValue = int.Parse(strs[12]);
					Zone2.CutTimeWindowValue = double.Parse(strs[13]) / 10;
					Zone2.ClimbTimeWindowValue = double.Parse(strs[14]) / 10;

					RefreshIconsText();
					Status[Status.Length - 1] = strs[strs.Length - 1] == "1";
				}

				if (Status.Length == StatusCount)
				{
					Zone1.CutRaised = (bool)Status[0] && Zone1.Enabled;
					Zone1.ClimbRaised = (bool)Status[1] && Zone1.Enabled;
					Zone1.CircuitRaised = (bool)Status[2] && Zone1.Enabled;
					Zone2.CutRaised = (bool)Status[3] && Zone2.Enabled;
					Zone2.ClimbRaised = (bool)Status[4] && Zone2.Enabled;
					Zone2.CircuitRaised = (bool)Status[5] && Zone2.Enabled;
					SOS.TamperRaised = (bool)Status[6] && SOS.Tamper;

					fault = new bool[3]
					{
					Zone1.CutRaised || Zone1.ClimbRaised || Zone1.CircuitRaised,
					Zone2.CutRaised || Zone2.ClimbRaised || Zone2.CircuitRaised,
					SOS.TamperRaised
					};
				}

				else
				{
					Zone1.CutRaised = false;
					Zone1.ClimbRaised = false;
					Zone1.CircuitRaised = false;
					Zone2.CutRaised = false;
					Zone2.ClimbRaised = false;
					Zone2.CircuitRaised = false;
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
			finally { Thread.Sleep(Delay); }
			return fault;
		}

		public static FlexiGuard Parse(Plant plant, string data)
		{
			try
			{
				FlexiGuard fg = new FlexiGuard() { Plant = plant };
				string[] faults = fg.Parse(data).Split(';');

				for (int i = 0; i < fg.ZoneCount; i++)
					fg.Faults.Add(FGFault.Parse(faults[i], fg, i));

				fg.Faults.Add(SOSFault.Parse(faults[fg.ZoneCount], fg, fg.ZoneCount));

				return fg;
			}
			catch (Exception ex) { }
			return null;
		}

		public override void CopyZones(int index) { }
	}

	public class FGFault : Fault
	{
		public bool CutRaised { get; set; }
		public bool ClimbRaised { get; set; }
		public bool CircuitRaised { get; set; }

		public int CutPulses { get; set; } = 2;
		public int ClimbPulses { get; set; } = 2;
		public int CutTimeWindow { get; set; } = 5;
		public int ClimbTimeWindow { get; set; } = 12;
		public int CutSensitivity { get; set; } = 5;
		public int ClimbSensitivity { get; set; } = 5;
		public int PreTime { get; set; } = 2;

		public int CutPulseValue { get; set; }
		public int ClimbPulseValue { get; set; }
		public double CutTimeWindowValue { get; set; }
		public double ClimbTimeWindowValue { get; set; }

		public override string FaultListImagePath
		{
			get
			{
				if (Reason == FaultReason.Cut || Reason == FaultReason.Climb)
					return faultListImagePath = baseFaultListImagePath + Reason + Extentions.FaultListImage;

				return base.FaultListImagePath;
			}
		}

		public override string IconText => Name
			+ (Plant.MapShowStatistics ? string.Format("\r\n{0} {1}\r\n{2} {3}",
				CutPulseValue.ToString().PadLeft(2, ' '),
				CutTimeWindowValue.ToString("0.0").PadLeft(4, ' '),
				ClimbPulseValue.ToString().PadLeft(2, ' '),
				ClimbTimeWindowValue.ToString("0.0").PadLeft(4, ' ')) : "")
			+ (Plant.MapShowTemperature ? string.Format("\r\n{0}°C", Module.Temperature) : "");
		public override string GetString() => string.Format("{0},{1}_{2}_{3}_{4}_{5}_{6}_{7}",
			base.GetString(), CutPulses, ClimbPulses, CutTimeWindow, ClimbTimeWindow, CutSensitivity, ClimbSensitivity, PreTime);
		public override void InitializeView() => PopUp = new FGPopUp(this);
		public override void InitializeStatus() => StatusWindow = new FGFaultStatus(this);

		public FGFault(IModule owner, int index) : base(owner, index) { }

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

			else if (CutRaised)
				return Reason = FaultReason.Cut;

			else if (ClimbRaised)
				return Reason = FaultReason.Climb;

			else if (CircuitRaised)
				return Reason = FaultReason.Circuit;

			else
				return Reason = FaultReason.Test;
		}

		public override bool Upload(string str)
		{
			int index = 0;

			try
			{
				string[] data = Parse(str).Split('_');

				CutPulses = int.Parse(data[index++]);
				ClimbPulses = int.Parse(data[index++]);
				CutTimeWindow = int.Parse(data[index++]);
				ClimbTimeWindow = int.Parse(data[index++]);
				CutSensitivity = int.Parse(data[index++]);
				ClimbSensitivity = int.Parse(data[index++]);
				PreTime = int.Parse(data[index++]);

				return true;
			}
			catch (Exception ex) { WriteToDebug(typeof(FGFault), Name, nameof(Upload), ex); }
			return false;
		}

		public static FGFault Parse(string str, IModule owner, int zoneIndex)
		{
			FGFault fault = new FGFault(owner, zoneIndex);
			fault.Upload(str);
			return fault;
		}

		public override string GetStringStatistics() => $"{base.GetStringStatistics()};" +
			$"{CutRaised},{ClimbRaised},{CircuitRaised},{CutPulseValue},{CutTimeWindowValue},{ClimbPulseValue},{ClimbTimeWindowValue}";

		public override void UploadStatistics(string data)
		{
			var pars = data.Split(';');
			var index = 0;

			base.UploadStatistics(pars[0]);

			pars = pars[1].Split(',');

			CutRaised = bool.Parse(pars[index++]);
			ClimbRaised = bool.Parse(pars[index++]);
			CircuitRaised = bool.Parse(pars[index++]);
			CutPulseValue = int.Parse(pars[index++]);
			CutTimeWindowValue = double.Parse(pars[index++]);
			ClimbPulseValue = int.Parse(pars[index++]);
			ClimbTimeWindowValue = double.Parse(pars[index++]);
		}
	}
}