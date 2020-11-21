using System;
using System.Windows;
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Modules.Common;
using AIMS3.FrontEnd.Site.Map;
using onvif10_device;

namespace AIMS3.BackEnd.Modules
{
	public class Relay : IIcon, IActuator
	{
		public int Index { get; private set; }
		public string Name { get; private set; }
		public string Text => string.Format("{0}({1})", Name, Module.Name);
		public string IconText => Text;
		public bool Activated { get; set; }

		private bool setNeeded;
		public bool SetNeeded
		{
			get => setNeeded;
			set
			{
				setNeeded = value;

				if (setNeeded)
					ResetNeeded = false;
			}
		}

		private bool resetNeeded;
		public bool ResetNeeded
		{
			get => resetNeeded;
			set
			{
				resetNeeded = value;

				if (resetNeeded)
					SetNeeded = false;
			}
		}

		public RelayView View { get; set; }

		public Spot Icon { get; set; }
		public string PreName { get; set; }

		public bool Raised { get => State == RelayState.Set; }
		public bool RaiseNeeded { get; set; }

		public enum RelayType { NO, NC }
		public RelayType Type { get; set; } = RelayType.NO;

		public enum RelayState { Set, Reset }
		public RelayState State { get; set; } = RelayState.Reset;

		public bool IsNC
		{
			get => Type == RelayType.NC;
			set
			{
				if (value)
					Type = RelayType.NC;

				else
					Type = RelayType.NO;
			}
		}

		private bool enabled;
		public bool Enabled 
		{ 
			get => enabled;
			set
			{
				if (enabled == true && value == false)
					ResetNeeded = true;

				enabled = value;
			}
		}

		public IModule Module { get; }
		public Plant Plant => Module.Plant;
		public ITelemetricSite Site => Module.Site;

		public override string ToString() => Text;
		public string GetString() => string.Format("{0},{1},{2}", Enabled, Type, Icon.GetString());

		public Relay(Module owner, int index)
		{
			Module = owner;
			PreName = "Out";
			Index = index;
			Name = PreName + (Index + 1);

			Application.Current.Dispatcher.Invoke(new Action(() => {
				View = new RelayView(this);
				Icon = new Spot(this); }));
		}

		public bool Set()
		{
			if (!Enabled)
				return false;

			if (Type == RelayType.NO)
				SetNeeded = !Module.SetRelay(Index);

			else
				SetNeeded = !Module.ResetRelay(Index);

			State = !SetNeeded ? RelayState.Set : State;
			return !SetNeeded;
		}

		public bool Reset()
		{
			if (!Enabled)
				return Module.ResetRelay(Index);

			if (Type == RelayType.NO)
				ResetNeeded = !Module.ResetRelay(Index);

			else
				ResetNeeded = !Module.SetRelay(Index);

			State = !ResetNeeded ? RelayState.Reset : State;
			return !ResetNeeded;
		}

		public void Upload(string str)
		{
			string[] data = str.Split(',');
			int index = 0;

			Enabled = bool.Parse(data[index++]);
			Type = (RelayType)Enum.Parse(typeof(RelayType), data[index++]);
			Icon.Upload(data[index++]);
		}

		public static Relay Parse(string str, Module module, int indx)
		{
			Relay relay;
			string[] data = str.Split(',');
			int index = 0;

			relay = new Relay(module, indx)
			{
				Enabled = bool.Parse(data[index++]),
				Type = (RelayType)Enum.Parse(typeof(RelayType), data[index++])
			};
			relay.Icon.Parse(data[index++]);

			return relay;
		}

		public string GetStringStatistics() => $"{State}";

		public void UploadStatistics(string data)
		{
			var pars = data.Split(',');
			var index = 0;

			State = (RelayState)Enum.Parse(typeof(RelayState), pars[index++]);
		}

		public void Save()
		{
			View.Save();
		}

		public void Load()
		{
			View.Load();
		}

		public void PopUpStatus() { }
		public void PopUpSettings() { }

		public string GetImageSuffix(int index)
		{
			switch (index)
			{
				case 0: return "Off";
				case 1: return "Idle";
				case 2: return "Raised";
			}

			return "";
		}
	}
}