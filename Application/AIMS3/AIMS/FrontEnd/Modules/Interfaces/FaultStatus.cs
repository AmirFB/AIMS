using System.Timers;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DevExpress.Xpf.Core;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Interfaces;

using static AIMS3.BackEnd.Site.TelemetricSite;
using System;
using System.Runtime.CompilerServices;

namespace AIMS3.FrontEnd.Modules.Interfaces
{
	public interface IFaultStatus : IAIMSWindow
	{
		Visibility Visibility { get; }
		Visibility IsZone { get; }
		Visibility IsSOS { get; }

		void Show();
		void Evaluate();
	}

	public abstract class FaultStatus : AIMSWindow, IFaultStatus
	{
		public IFault Fault { get; private set; }
		private delegate void UpdateSourcesCallBack();
		private delegate void UpdateTargetsCallBack();
		public abstract BindingGroup Group { get; }
		public Timer Timer { get; } = new Timer(500);

		public abstract SimpleButton SimpleButtonReset { get; }
		public abstract SimpleButton SimpleButtonAcknowledge { get; }

		public Brush TamperColor => GetColor(Fault.TamperRaised);
		public Brush ConnectedColor => GetColor(!Fault.Module.Initialized);

		public string TamperRaised => GetHealthy(Fault.TamperRaised);
		public string Connected => Fault.Module.Initialized ? (string)TryFindResource("Connectedy") : (string)TryFindResource("Disconnected");

		public bool CanAcknowledge => Fault.CanAcknowledge;
		public bool CanReset => Fault.CanReset;

		public Visibility IsZone => Fault.IsZone ? Visibility.Visible : Visibility.Collapsed;
		public Visibility IsSOS => !Fault.IsZone ? Visibility.Visible : Visibility.Collapsed;
		public string Module => Fault.Module.Name;
		public string Name_ => Fault.Name;

		public void Timer_Elapsed(object sender, ElapsedEventArgs e) { }

		public void SetFault(IFault fault)
		{
			Fault = fault;
			Plant = Fault.Plant;
		}

		public override void PopUp()
		{
			Load();

			if (!IsVisible)
				Show();
		}

		public override void Load()
		{
			OnPropertyChanged(nameof(IsZone));
			OnPropertyChanged(nameof(IsSOS));
			OnPropertyChanged(nameof(Module));
			OnPropertyChanged(nameof(Fault));
			OnPropertyChanged(nameof(Name_));
			Evaluate();
		}

		public string GetHealthy(bool state)
		{
			if (!Fault.Module.Initialized)
				return BackEnd.Common.GetResourceString("Error");

			if (state)
				return BackEnd.Common.GetResourceString("Faulty");

			return BackEnd.Common.GetResourceString("Healthy");
		}

		public static Brush GetColor(bool state)
		{
			if (state)
				return Brushes.Red;

			return Brushes.Green;
		}

		public virtual void Evaluate()
		{
			OnPropertyChanged(nameof(Connected));
			OnPropertyChanged(nameof(ConnectedColor));
			OnPropertyChanged(nameof(CanAcknowledge));
			OnPropertyChanged(nameof(CanReset));
		}

		private async void Acknowledge()
		{
			await System.Threading.Tasks.Task.Run(async () =>
			{
				Evaluate();

				if (!Fault.CanAcknowledge)
					return;

				var result = await Site.AuthenticateAcknowledgeFault(Fault).ConfigureAwait(false);
				ShowMessageBoxAuthentication(result);
				Evaluate();
			}).ConfigureAwait(false);
		}

		private async void Reset()
		{
			await System.Threading.Tasks.Task.Run(async () =>
			{
				Evaluate();

				if (!Fault.CanReset)
					return;

				var result = await Site.AuthenticateResetFault(Fault).ConfigureAwait(false);
				ShowMessageBoxAuthentication(result);

				if (result == true)
					await Application.Current.Dispatcher.BeginInvoke(new Action(() => { Close(); }), null);

			}).ConfigureAwait(false);
		}

		public void Acknowledge_Click(object sender, RoutedEventArgs e) => Acknowledge();

		public void Reset_Click(object sender, RoutedEventArgs e) => Reset();
	}
}