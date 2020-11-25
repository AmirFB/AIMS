using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AIMS3.BackEnd.Modules;
using AIMS3.BackEnd.Site;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;

using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.Modules.Relay;
using static AIMS3.BackEnd.Site.TelemetricSite;

using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace AIMS3.FrontEnd.Site.Map
{
	public partial class Spot : UserControl, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		private ITelemetricSite site;
		public virtual ITelemetricSite Site
		{
			get => site;
			set
			{
				site = value;
				plant = site.Plant;
			}
		}

		private Plant plant;
		public virtual Plant Plant
		{
			get => plant;
			set
			{
				plant = value;
				site = plant.Owner;
			}
		}

		private readonly Timer timerBlink = new Timer(250);
		private readonly Timer timerUpdate = new Timer(200);
		private readonly Timer timerInitialize = new Timer(500);
		private int index;

		public Visibility TestVisibility => Owner is IFault ? Visibility.Visible : Visibility.Collapsed;
		public Visibility StatusVisibility => Owner is IFault ? Visibility.Visible : Visibility.Collapsed;
		public Visibility SettingsVisibility => Owner is IFault ? Visibility.Visible : Visibility.Collapsed;
		public Visibility SetVisibility => Owner is Relay ? Visibility.Visible : Visibility.Collapsed;
		public Visibility ResetVisibility => Owner is Relay ? Visibility.Visible : Visibility.Collapsed;
		public Visibility AcknowledgeVisibility => Owner is IFault ? Visibility.Visible : Visibility.Collapsed;

		public bool CanRemove => (Owner as IFault)?.IsZone != true && GetResourceBool("CanMapEdit");
		public bool CanSet => (Owner as Relay)?.State == RelayState.Reset;
		public bool CanReset => (Owner as Relay)?.State == RelayState.Set;
		public bool CanAcknowledge => (Owner as IFault)?.CanAcknowledge == true;
		public bool CanResetF => (Owner as IFault)?.CanReset == true;

		private Brush backgroundBrush = DefaultBackground;
		public Brush BackgroundBrush
		{
			get => backgroundBrush;
			set
			{
				backgroundBrush = value;
				OnPropertyChanged(nameof(BackgroundBrush));
			}
		}

		private Brush foregroundBrush = DefaultForeground;
		public Brush ForegroundBrush
		{
			get => foregroundBrush;
			set
			{
				foregroundBrush = value;
				OnPropertyChanged(nameof(ForegroundBrush));
			}
		}

		private static Brush DefaultBackground => Brushes.Wheat;
		private static Brush UpdateBackground => Brushes.White;
		private static Brush InitializeBackground => Brushes.ForestGreen;
		private static Brush FailedBackground => Brushes.Red;

		private static Brush DefaultForeground => Brushes.Black;
		private static Brush UpdateForeground => Brushes.Blue;
		private static Brush InitializeForeground => Brushes.White;
		private static Brush FailedForeground => Brushes.Yellow;

		public double X { get; set; }
		public double Y { get; set; }
		public bool IsPlaced { get; set; }
		public bool IsDefault { get; set; }
		public bool IsOriginal { get; set; }

		public IIcon Owner { get; private set; }
		public IModule Module => Owner.Module;
		public string PreName => Owner.PreName;

		public string Text => Owner.IconText;
		private bool Enabled => Owner.Enabled;

		public int Index
		{
			get => index;
			set
			{
				index = value;
				OnPropertyChanged(nameof(ImageSource));
			}
		}

		private bool Raised => Plant.RaisedFaults.Contains(Owner);
		public ImageSource ImageSource
		{
			get
			{
				try
				{
					return ImageSources[(int)(ImageSourceName)Enum.Parse(typeof(ImageSourceName), PreName + Owner.GetImageSuffix(Index))];
				}
				catch { }
				return null;
			}
		}

		private enum ImageSourceName { EFIdle, EFOff, EFRaised, FGIdle, FGOff, FGRaised, OutIdle, OutOff, OutRaised }

		private static List<ImageSource> ImageSources { get; } = new List<ImageSource>()
		{
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
				new Uri("pack://application:,,,/Resources/Icons/Map/EFIdle.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
			new Uri("pack://application:,,,/Resources/Icons/Map/EFOff.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
			new Uri("pack://application:,,,/Resources/Icons/Map/EFRaised.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
			new Uri("pack://application:,,,/Resources/Icons/Map/FGIdle.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
			new Uri("pack://application:,,,/Resources/Icons/Map/FGOff.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
			new Uri("pack://application:,,,/Resources/Icons/Map/FGRaised.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
			new Uri("pack://application:,,,/Resources/Icons/Map/OutIdle.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
			new Uri("pack://application:,,,/Resources/Icons/Map/OutOff.svg")), 1d, null, null, true),
			WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
			new Uri("pack://application:,,,/Resources/Icons/Map/OutRaised.svg")), 1d, null, null, true),
		};

		public Point Position { get; set; }

		public string GetString() => string.Format("{0}>{1}>{2}>{3}", IsPlaced, X, Y, IsDefault);

		private void RefreshBindings() => contextMenu.BindingGroup.BindingExpressions.ToList().ForEach(binding => binding.UpdateTarget());

		public Spot(IIcon owner)
		{
			InitializeComponent();

			DataContext = this;

			Owner = owner;
			Plant = Owner.Plant;
			timerBlink.Elapsed += TimerBlink_Elapsed;

			timerUpdate.AutoReset = false;
			timerUpdate.Elapsed += TimerUpdate_Elapsed;

			timerInitialize.AutoReset = false;
			timerInitialize.Elapsed += TimerInitialize_Elapsed;
		}

		public void Raise() => timerBlink.Start();

		public void Upload(string data)
		{
			string[] pars = data.Split('>');
			var index = 0;

			IsPlaced = (Owner as IFault)?.IsZone == true ? true : bool.Parse(pars[index]);
			X = double.Parse(pars[++index]);
			Y = double.Parse(pars[++index]);
			IsDefault = bool.Parse(pars[++index]);
		}

		public void Parse(string data)
		{
			string[] datas = data.Split('>');

			IsPlaced = bool.Parse(datas[0]);
			X = double.Parse(datas[1]);
			Y = double.Parse(datas[2]);
		}

		public void Refresh(bool refreshText)
		{
			RefreshText(refreshText);
			RefreshBindings();

			if (Raised)
				Raise();

			else if (Enabled)
				TurnOn();

			else
				TurnOff();
		}

		public void RefreshText(bool animation)
		{
			OnPropertyChanged(nameof(Text));

			if (backgroundBrush == InitializeBackground)
				return;

			if (animation)
			{
				BackgroundBrush = UpdateBackground;
				ForegroundBrush = UpdateForeground;
				timerUpdate.Start();
			}
		}

		public void InitializationRefresh()
		{
			if (!IsPlaced)
				return;

			BackgroundBrush = InitializeBackground;
			ForegroundBrush = InitializeForeground;
			timerInitialize.Start();
		}

		public void FailedRefresh()
		{
			if (!IsPlaced)
				return;

			BackgroundBrush = FailedBackground;
			ForegroundBrush = FailedForeground;
			timerInitialize.Start();
		}

		public void ResetColors()
		{
			if (!IsPlaced)
				return;

			BackgroundBrush = DefaultBackground;
			ForegroundBrush = DefaultForeground;
			timerUpdate.Stop();
			timerInitialize.Stop();
		}

		private void TimerBlink_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (!IsPlaced)
				return;

			Index = (Index) % 2 + 1;
		}

		private void TimerUpdate_Elapsed(object sender, ElapsedEventArgs e) => ResetColors();
		private void TimerInitialize_Elapsed(object sender, ElapsedEventArgs e) => ResetColors();

		public void TurnOn()
		{
			timerBlink.Stop();
			Index = 1;
		}

		public void TurnOff()
		{
			timerBlink.Stop();
			Index = 0;
		}

		public void Remove(bool save)
		{
			Visibility = Visibility.Collapsed;
			IsPlaced = false;

			if (save)
				Plant.SaveModules(false);
		}

		public void Status() => Owner.PopUpStatus();

		private async void Test()
		{
			var result = await Site.AuthenticateRaiseFault(Owner as IFault).ConfigureAwait(false);
			ShowMessageBoxAuthentication(result);
			RefreshBindings();
		}

		private void Test_Click(object sender, RoutedEventArgs e) => Test();

		private async void Remove_Click(object sender, RoutedEventArgs e)
		{
			Remove(false);

			var result = await Site.AuthenticateModifyModule(Module).ConfigureAwait(false);

			if (result == true)
				Plant.SaveModules(false);

			else
			{
				Visibility = Visibility.Visible;
				IsPlaced = true;
			}
		}

		private void Status_Click(object sender, RoutedEventArgs e) => Status();
		private void Spot_RightClick(object sender, MouseButtonEventArgs e) => RefreshBindings();

		private void Spot_DoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (!(bool)TryFindResource("CanMapPopUp"))
				return;

			Owner.PopUpSettings();
		}

		private async void SetRelay()
		{
			var result = await Site.AuthenticateSetRelay(Owner as Relay).ConfigureAwait(false);
			ShowMessageBoxAuthentication(result);
			RefreshBindings();
		}

		private async void ResetRelay()
		{
			var result = await Site.AuthenticateResetRelay(Owner as Relay).ConfigureAwait(false);
			ShowMessageBoxAuthentication(result);
			RefreshBindings();
		}

		private void Set_Click(object sender, RoutedEventArgs e) => SetRelay();
		private void ResetR_Click(object sender, RoutedEventArgs e) => ResetRelay();

		private void Settings_Click(object sender, RoutedEventArgs e) => Owner.PopUpSettings();

		private async void Acknowledge()
		{
			if (!(Owner as IFault).CanAcknowledge)
				return;

			var result = await Site.AuthenticateAcknowledgeFault(Owner as IFault).ConfigureAwait(false);
			ShowMessageBoxAuthentication(result);
			RefreshBindings();
		}

		private async void ResetFault()
		{
			if (!(Owner as IFault).CanReset)
				return;

			var result = await Site.AuthenticateResetFault(Owner as IFault).ConfigureAwait(false);
			ShowMessageBoxAuthentication(result);
			RefreshBindings();
		}

		private void Acknowledge_Click(object sender, RoutedEventArgs e) => Acknowledge();

		private void ResetF_Click(object sender, RoutedEventArgs e) => ResetFault();
	}
}