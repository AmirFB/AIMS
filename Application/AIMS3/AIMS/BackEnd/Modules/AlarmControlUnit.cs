using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Modules.ACU;

using static AIMS3.BackEnd.Common;

namespace AIMS3.BackEnd.Modules
{
	public class AlarmControlUnit : Module
    {
        public override List<IModule> Collection => Plant.ACU;
		public override ModuleType Type => ModuleType.ACU;

		public override int RelayCount => 8;
		public override int ZoneCount => 0;
		public override int StatusCount => 3;
		public override int WarningCount => 0;

		public override Command CommandCheck => new Command() { Request = "AT+" + PreName, Respond = "OK", Timeout = 100, Delay = 0 };
		public override Command CommandReset => new Command() { Request = "AT+" + PreName + "RST", Respond = "OKR", Timeout = 150, Delay = 400 };
		public override Command CommandSetOutput => new Command() { Request = "AT+" + PreName + "SOUT", Respond = "OKSO", Timeout = 150, Delay = 10 };
		public override Command CommandResetOutput => new Command() { Request = "AT+" + PreName + "ROUT", Respond = "OKRO", Timeout = 150, Delay = 10 };
		public override Command CommandStatus => new Command() { Request = "AT+" + PreName + "FSERR", Respond = "OK", Timeout = 50, Delay = 5 };

		public override void InitializeView() => Application.Current.Dispatcher.Invoke(new Action(() => { View = new ACUView(this); }));

		public AlarmControlUnit() : base()
		{
			PreName = "ACU";

			Relays.Clear();
			Faults.Clear();
		}

		public AlarmControlUnit(Plant plant) : base()
		{
			Plant = plant;
			PreName = "ACU";

			Relays.Clear();
			Faults.Clear();

			for (byte i = 0; i < RelayCount; i++)
				Relays.Add(new Relay(this, i) { Enabled = true });

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

		public override bool[] ReadStatus()
		{
			string[] strs;
			string data;
			bool[] fault = new bool[FaultCount];

			try
			{
				Status = new object[1];

				Write(CommandError.Request);
				SetTimeout(CommandError.Timeout);
				Delay = CommandError.Delay;

				data = Read();
				strs = data.Replace(",[" + Address + "]", "").Replace(CommandError.Respond + ":", "").Split(',');
				Status = new object[strs.Length];

				for (int i = 0; i < strs.Length; i++)
					Status[i] = strs[i] == "1";

				if (Status.Length == StatusCount)
				{
					SOS.TamperRaised = (bool)Status[2] && SOS.Tamper;

					fault = new bool[]
					{
					SOS.TamperRaised
					};
				}

				else
				{					
					SOS.TamperRaised = false;

					fault = new bool[]
					{
					false
					};
				}
			}
			catch (Exception ex) { }
			finally { Thread.Sleep(Delay); }

			return fault;
		}

		public static AlarmControlUnit Parse(Plant plant, string data)
		{
			try
			{
				AlarmControlUnit acu = new AlarmControlUnit() { Plant = plant };
				string[] faults = acu.Parse(data).Split(';');

				acu.Faults.Clear();
				acu.Faults.Add(SOSFault.Parse(faults[0], acu, 0));

				return acu;
			}
			catch (Exception ex) { }
			return null;
		}
	}
}