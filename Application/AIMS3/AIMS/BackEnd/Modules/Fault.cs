using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Modules.ACU;
using AIMS3.FrontEnd.Modules.Common;
using AIMS3.FrontEnd.Modules.EF;
using AIMS3.FrontEnd.Modules.FG;
using AIMS3.FrontEnd.Modules.Interfaces;
using AIMS3.FrontEnd.Site.Map;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;
using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.DataBase;
using static AIMS3.BackEnd.Modules.Connection;
using static AIMS3.BackEnd.Modules.Fault;
using static AIMS3.BackEnd.Modules.Module;

namespace AIMS3.BackEnd.Modules
{
	public interface IFault : IIcon, IActuator
	{
		int ZoneIndex { get; }
		int Index { get; }
		string FaultListText { get; }
		Color FaultListBackground { get; }

		ISettingsPopUp PopUp { get; }
		IFaultStatus StatusWindow { get; }

		ModuleType OwnerType { get; }

		bool ResetNeeded { get; }

		new bool Raised { get; set; }
		bool Obviated { get; }
		bool Acknowledged { get; }
		bool Reseted { get; }
		bool TamperRaised { get; set; }

		bool CanAcknowledge { get; }
		bool CanReset { get; }

		bool IsZone { get; }

		string FaultListImagePath { get; }
		ImageSource FaultListImageSource { get; }

		List<Surveliance> Cams { get; }
		List<Relay> Relays { get; }
		List<string> RelayStrings { get; }

		FaultReason Reason { get; }

		bool AutomaticReset { get; }
		bool Tamper { get; }

		FaultReason SetReason();

		void Raise(ResetStatus status);
		void Obviate();
		void Acknowledge(ResetStatus status);
		void Reset(ResetStatus status, bool log);

		void PopupCams();
		void CloseCams();

		string GetString();
		bool Upload(string str);
		void EvaluateRelays();
		void InitializeView();
		void InitializeStatus();
	}

	public abstract class Fault : IFault
	{
		public string Name => IsZone ? "Zone" + ZoneIndex : Module.Name;
		public int ZoneIndex => Module.Index * 2 + Index - 1;
		public int Index { get; protected set; }
		public string FaultListText => IsZone ? "Zone" + ZoneIndex : "SOS";
		public Color FaultListBackground => GetFaultListBackground();

		private readonly object lockObject = new object();

		public ISettingsPopUp PopUp { get; set; }
		public IFaultStatus StatusWindow { get; set; }
		public Spot Icon { get; set; }

		public ModuleType OwnerType => Module.Type;

		public string PreName => Module.PreName;

		public bool RaiseNeeded { get; set; }
		public bool ResetNeeded { get; set; }

		public bool Raised { get; set; }
		public bool Obviated { get; set; }
		public bool Acknowledged { get; set; }
		public bool Reseted { get; set; }
		public bool TamperRaised { get; set; }

		public bool CanAcknowledge => Plant.RaisedFaults.Contains(this) && !Acknowledged;
		public bool CanReset => Plant.RaisedFaults.Contains(this) && ((!Raised && Acknowledged) || !Enabled);

		public bool IsZone => Index < Module.ZoneCount;

		protected const string baseFaultListImagePath = "pack://application:,,,/Resources/Icons/FaultList/";
		protected string faultListImagePath { get; set; } = baseFaultListImagePath + FaultReason.Other;
		public virtual string FaultListImagePath
		{
			get
			{
				if (Reason == FaultReason.Test)
					faultListImagePath = baseFaultListImagePath + Reason + Extentions.FaultListImage;

				return faultListImagePath;
			}
		}

		private enum FaultListReason { Circuit, Climb, Cut, HV, LV, Other, SerialConnected, SerialDisconnected, TamperFault, TamperOK, TCPConnected, TCPDisconnected, Test };

		private static List<ImageSource> FaultListImageSources { get; } = new List<ImageSource>() {
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
				new Uri("pack://application:,,,/Resources/Icons/FaultList/Circuit.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
				new Uri("pack://application:,,,/Resources/Icons/FaultList/Climb.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
				new Uri("pack://application:,,,/Resources/Icons/FaultList/Cut.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
				new Uri("pack://application:,,,/Resources/Icons/FaultList/HV.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
				new Uri("pack://application:,,,/Resources/Icons/FaultList/LV.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
				new Uri("pack://application:,,,/Resources/Icons/FaultList/Other.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
				new Uri("pack://application:,,,/Resources/Icons/FaultList/SerialConnected.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
				new Uri("pack://application:,,,/Resources/Icons/FaultList/SerialDisconnected.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
				new Uri("pack://application:,,,/Resources/Icons/FaultList/TamperFault.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
				new Uri("pack://application:,,,/Resources/Icons/FaultList/TamperOK.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
				new Uri("pack://application:,,,/Resources/Icons/FaultList/TCPConnected.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
				new Uri("pack://application:,,,/Resources/Icons/FaultList/TCPDisconnected.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
				new Uri("pack://application:,,,/Resources/Icons/FaultList/Test.svg")), 1d, null, null, true)
			};

		private FaultListReason FaultReasonToFaultListReason(FaultReason reason)
		{
			if (reason == FaultReason.Connection)
			{
				if (Module.ConnectionType == ModuleConnectionType.Serial)
				{
					if (Module.Initialized)
						return FaultListReason.SerialConnected;

					return FaultListReason.SerialDisconnected;
				}

				if (Module.Initialized)
					return FaultListReason.TCPConnected;

				return FaultListReason.TCPDisconnected;
			}

			if (reason == FaultReason.Tamper)
			{
				if (TamperRaised)
					return FaultListReason.TamperFault;

				return FaultListReason.TamperOK;
			}

			return (FaultListReason)Enum.Parse(typeof(FaultListReason), reason.ToString());
		}

		public ImageSource FaultListImageSource => FaultListImageSources[(int)FaultReasonToFaultListReason(Reason)];

		public IModule Module { get; set; }

		public Plant Plant => Module.Plant;
		public ITelemetricSite Site => Module.Site;

		public List<Surveliance> Cams { get; } = new List<Surveliance>();
		public List<Relay> Relays { get; } = new List<Relay>();
		public List<string> RelayStrings { get; } = new List<string>();

		public FaultReason Reason { get; set; }

		public bool Enabled { get; set; } = true;
		public bool AutomaticReset { get; set; }
		public bool Tamper { get; set; }

		public enum ResetStatus { Automatic, Local, Remote };
		public enum FaultReason { Connection, Tamper, HV, LV, Cut, Climb, Circuit, Test, Other };

		public abstract string IconText { get; }

		public abstract FaultReason SetReason();

		public abstract void InitializeView();
		public abstract void InitializeStatus();

		public Fault(IModule owner, int index)
		{
			Module = owner;
			Index = index;

			Application.Current.Dispatcher.Invoke(new Action(() => { Icon = new Spot(this) { IsDefault = IsZone }; }), null);
		}

		public void PopUpSettings()
		{
			if (PopUp == null)
				InitializeView();

			PopUp.PopUp();
		}

		public void Raise(ResetStatus status)
		{
			lock (lockObject)
			{
				try
				{
					SetReason();

					if (!Plant.RaisedFaults.Contains(this))
						Plant.RaisedFaults.Add(this);

					Plant.RefreshFaultList();

					Sound.Play(IsZone);
					Icon?.Raise();

					if (RaiseNeeded)
						Obviated = true;

					else
						Raised = true;

					Acknowledged = false;
					Reseted = false;

					foreach (Relay relay in Relays)
						relay.Set();

					foreach (Surveliance cam in Cams)
					{
						cam.SetPreset();
						cam.Popup();
					}

					if (status == ResetStatus.Local)
						Plant.Log.Save("Fault", "Raised", Name, Reason, Module.Name);

					else
						Plant.Log.Save("Fault", "Raised Remotely", Name, Reason, Module.Name);

					RaiseNeeded = false;
				}
				catch (Exception ex) { DXMessageBox.Show("Raise\r\n" + ex.ToString()); }
			}
		}

		public void Obviate()
		{
			lock (lockObject)
			{
				try
				{
					Raised = false;
					Plant.Log.Save("Fault", "Obviated", Name, null, Module.Name);
					Plant.RefreshFaultList();
				}
				catch (Exception ex) { DXMessageBox.Show("Obviate\r\n" + ex.ToString()); }
			}
		}

		public void Acknowledge(ResetStatus status)
		{
			lock (lockObject)
			{
				Sound.Stop();
				Acknowledged = true;

				if (status == ResetStatus.Local)
					Plant.Log.Save("Fault", "Acknowledged", Name, null, Module.Name);

				else
					Plant.Log.Save("Fault", "Acknowledged Remotely", Name, null, Module.Name);

				Plant.RefreshFaultList();
			}
		}

		public void Reset(ResetStatus status, bool log)
		{
			lock (lockObject)
			{
				try
				{
					if (AutomaticReset)
						status = ResetStatus.Automatic;

					Sound.Stop();

					Raised = false;
					Reseted = true;
					Obviated = false;

					if (AutomaticReset)
						foreach (Relay relay in Relays)
							relay.Set();

					else
						foreach (Relay relay in Relays)
							relay.ResetNeeded = true;

					foreach (Surveliance cam in Cams)
					{
						cam.SetHome();
						cam.Close();
					}

					ResetNeeded = false;
					Icon?.TurnOn();

					Plant.RaisedFaults.Remove(this);
					Plant.RefreshFaultList();
					ResetNeeded = false;

					if (!log)
						return;

					if (status == ResetStatus.Automatic)
						Plant.Log.Save("Fault", "Reseted Automatically", Name, Reason, Module.Name);

					else if (status == ResetStatus.Local)
						Plant.Log.Save("Fault", "Reseted", Name, Reason, Module.Name);

					else
						Plant.Log.Save("Fault", "Reseted Remotely", Name, Reason, Module.Name);
				}
				catch (Exception ex) { DXMessageBox.Show("Reset\r\n" + ex.ToString()); }
			}
		}

		public void PopUpStatus()
		{
			if (StatusWindow == null)
				InitializeStatus();

			StatusWindow.PopUp();
			StatusWindow.Evaluate();
		}

		public void PopupCams()
		{
			foreach (Surveliance cam in Cams)
				cam.Popup();
		}

		public void CloseCams()
		{
			foreach (Surveliance cam in Cams)
				cam.Close();
		}

		public void EvaluateRelays()
		{
			Relays.Clear();

			foreach (string relay in RelayStrings)
				Relays.Add(Module.Plant.GetRelay(relay));
		}

		public override string ToString() => Name;

		public virtual string GetString()
		{
			string data = string.Format("{0},{1},{2},{3},", Enabled, AutomaticReset, Tamper, Icon.GetString());

			foreach (Relay relay in Relays)
				data += relay.Text + '-';

			if (Relays.Count > 0)
				data = data.Remove(data.Length - 1, 1);

			data += ",";

			foreach (Surveliance cam in Cams)
				data += cam.Name + ">" + cam.Preset + "_";

			if (Cams.Count > 0)
				data = data.Remove(data.Length - 1, 1);

			return data;
		}

		public abstract bool Upload(string str);

		public string Parse(string str)
		{
			int index = 0;

			string[] data = str.Split(',');

			Enabled = bool.Parse(data[index++]);
			AutomaticReset = bool.Parse(data[index++]);
			Tamper = bool.Parse(data[index++]);

			if (Icon.Owner != this)
				Icon.Parse(data[index++]);

			else
				Icon.Upload(data[index++]);

			RelayStrings.Clear();
			Cams.Clear();

			RelayStrings.AddRange(data[index++].Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries));

			foreach (string cam in data[index++].Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string[] pars = cam.Split('>');
				Cams.Add(new Surveliance() { Camera = Module.Plant.GetCamera(pars[0]), Preset = int.Parse(pars[1]) });
			}

			return index < data.Length ? data[index] : "";
		}

		public virtual string GetStringStatistics() => $"{Raised},{Acknowledged},{Reseted},{Obviated},{Reason}";

		public virtual void UploadStatistics(string data)
		{
			var pars = data.Split(',');
			var index = 0;
			bool raised, acknowledged, reseted, obviated;

			raised = bool.Parse(pars[index++]);
			acknowledged = bool.Parse(pars[index++]);
			reseted = bool.Parse(pars[index++]);
			obviated = bool.Parse(pars[index++]);
			Reason = (FaultReason)Enum.Parse(typeof(FaultReason), pars[index++]);

			if (raised && !Raised)
				Raise(ResetStatus.Local);

			else if (obviated && !Obviated)
			{
				if (!Raised)
					Raise(ResetStatus.Local);

				Obviate();
			}

			else if (reseted && !Reseted)
				Reset(ResetStatus.Local, true);

			else if (acknowledged && !Acknowledged)
				Acknowledge(ResetStatus.Local);

			Raised = raised;
			Acknowledged = acknowledged;
			Reseted = reseted;
			Obviated = obviated;
		}

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

		private Color GetFaultListBackground()
		{
			if (Obviated)
			{
				if (Acknowledged)
					return Colors.Blue;

				return Colors.Green;
			}

			if (Acknowledged)
				return Colors.Yellow;

			return Colors.Red;
		}
	}

	public class SOSFault : Fault
	{
		public override string IconText => Name;

		public override string FaultListImagePath
		{
			get
			{
				if (Reason == FaultReason.Connection)
					return faultListImagePath = baseFaultListImagePath + Module.ConnectionType + (Module.Initialized ? "Connected" : "Disconnected") + Extentions.FaultListImage;

				if (Reason == FaultReason.Tamper)
					return faultListImagePath = baseFaultListImagePath + Reason + Extentions.FaultListImage;

				return base.FaultListImagePath;
			}
		}

		public override string GetString() => string.Format("{0}", base.GetString());
		public override void InitializeView() => PopUp = new SOSPopUp(this);

		public override void InitializeStatus()
		{
			switch (Module.Type)
			{
				case ModuleType.EF:
					StatusWindow = new EFFaultStatus(this);
					break;

				case ModuleType.FG:
					StatusWindow = new FGFaultStatus(this);
					break;

				case ModuleType.ACU:
					StatusWindow = new ACUFaultStatus(this);
					break;
			}
		}

		public SOSFault(IModule owner, int index) : base(owner, index) => AutomaticReset = true;

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

			else
				return Reason = FaultReason.Test;
		}

		public override bool Upload(string str)
		{
			try
			{
				//int index = 0;
				/*string[] data = */
				Parse(str);/*.Split('_');*/
				return true;
			}
			catch (Exception ex) { WriteToDebug(typeof(SOSFault), Name, nameof(Upload), ex); }
			return false;
		}

		public static SOSFault Parse(string str, IModule owner, int zoneIndex)
		{
			SOSFault fault = new SOSFault(owner, zoneIndex);
			fault.Upload(str);
			return fault;
		}

		public override string GetStringStatistics() => $"{base.GetStringStatistics()};{TamperRaised}";

		public override void UploadStatistics(string data)
		{
			var pars = data.Split(';');
			var index = 0;

			base.UploadStatistics(pars[0]);

			pars = pars[1].Split(',');

			TamperRaised = bool.Parse(pars[index++]);
		}
	}

	public class Surveliance
	{
		public Camera Camera { get; set; }
		public int Preset { get; set; }
		public string Name { get => Camera.Name; }
		public string IP { get => Camera.Name; }
		public string Username { get => Camera.Username; }
		public string Password { get => Camera.Password; }

		public void SetPreset() => Camera.SetPreset(Preset);
		public void SetHome() => Camera.SetHome();
		public void Popup() => Camera.Popup();
		public void Close() => Camera.Close();

		public override string ToString() => Name + ">" + Preset;
	}
}