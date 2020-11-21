using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Linq;
using AIMS3.BackEnd.NVT;
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Modules.Cam;
using AIMS3.FrontEnd.Site.Map;
using onvif.services;

using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.DataBase;
using static AIMS3.BackEnd.Modules.Connection;

namespace AIMS3.BackEnd.Modules
{
	public class Camera : Module, IIcon, INotifyPropertyChanged
	{
		public override List<IModule> Collection => Plant.Cam;
		public override ModuleType Type => ModuleType.Cam;
		public IModule Module => this;

		public override int RelayCount => 0;
		public override int ZoneCount => 0;
		public override int StatusCount => 0;
		public override int WarningCount => 0;

		private delegate void PlayerCallBack();
		private IPCamera Cam { get; set; } = new IPCamera();
		public Spot Icon { get; private set; }

		public override string IP { get => Cam.IP; set => Cam.IP = value; }
		public override int Port { get => Cam.Port; set => Cam.Port = value; }

		public string RTSPAddress => Brand != null ? Brand.RTSP : CameraBrand.Default.RTSP;

		public static List<CameraBrand> Brands { get; } = new List<CameraBrand>();

		public bool Enabled { get; set; } = true;
		public bool Raised { get; set; } = false;
		public bool RaiseNeeded { get; set; }

		public string IconText => Name;

		private Thread LoopThread;

		public List<PTZPreset> Presets => Cam.Presets;
		public int PresetCount => Presets.Count;
		public Player Player { get; set; }
		public CamWindow Window { get; set; }

		public void SetLocation(string name) => Window.SetLocation(name);
		public enum Stream { TCP, UDP }
				
		public string Username { get => Cam.Username; set => Cam.Username = value; }
		public string Password { get => Cam.Password; set => Cam.Password = value; }
		public int RtspPort { get; set; } = 554;
		public int Cache { get; set; } = 100;
		public bool RtspIsUDP { get => RtspStream == Stream.UDP; set => RtspStream = value ? Stream.UDP : Stream.TCP; }
		public bool RtspIsTCP { get => RtspStream == Stream.TCP; set => RtspStream = value ? Stream.TCP : Stream.UDP; }
		private int PresetToGo { get; set; } = -1;
		private bool HomeNeeded { get; set; } = false;
		private bool PlayNeeded { get; set; } = false;
		private bool StopNeeded { get; set; } = false;
		private string LastPreset { get; set; } = "";

		public Stream RtspStream { get; set; } = Stream.UDP;
		public string Token => Brand.Type == CameraBrand.RTSPType.Token ? Profiles[ProfileIndex].token : ProfileIndex.ToString();

		public CameraBrand Brand { get; set; }

		public List<Profile> Profiles => Cam.Profiles;
		public Profile Profile => Cam.Profile;
		public int ProfileIndex { get; set; }

		public Camera(Plant plant) : base()
		{
			Plant = plant;
			PreName = "Cam";
			ConnectionType = ModuleConnectionType.TCP;

			GetNewAddress();
			ConverterModule = new ConverterModule(Plant, "");
		}

		public override bool[] ReadStatus() { return new bool[0]; }

		public static void UploadBrands()
		{
            if (!File.Exists(Files.Brands.FullName))
                return;

			string[] strs = File.ReadAllText(Files.Brands.FullName).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
			string[] temp;

			Brands.Clear();

			foreach (string brand in strs)
			{
				temp = brand.Split(' ');
				Brands.Add(new CameraBrand() { Name = temp[0], Type = temp[1] == "t" ? CameraBrand.RTSPType.Token : CameraBrand.RTSPType.Index, RTSP = temp[2] });
			}
		}		

		public void StartLoop()
		{
			if (LoopThread == null)
				LoopThread = new Thread(Loop) { IsBackground = true, Name = string.Format("{0}:{1}", Name, IP) };

			if (!LoopThread.IsAlive)
				LoopThread.Start();
		}

		public async Task<bool> Initialize()
		{
			try
			{
				if (!await Cam.Initialize().ConfigureAwait(false))
					return Initialized = false;

				Presets.Clear();
				Presets.AddRange(await Cam.GetPresets().ConfigureAwait(false));
				await GoToHome().ConfigureAwait(false);

				Initialized = true;
				InitializeNeeded = false;
				return true;
			}
			catch (Exception ex) { }
			finally { RefreshIconsInitialization(); }

			return Initialized = false;
		}

		public async Task<bool> InitializeImmediate()
		{
			try
			{
				if (!await Cam.InitializeImmediate().ConfigureAwait(false))
					return Initialized = false;

				Presets.Clear();
				Presets.AddRange(await Cam.GetPresets().ConfigureAwait(false));
				await GoToHome().ConfigureAwait(false);

				Initialized = true;
				InitializeNeeded = false;
				return true;
			}
			catch (Exception ex) { }
			finally { RefreshIconsInitialization(); }

			return Initialized = false;
		}

		public void SetPreset(int preset)
		{
			PresetToGo = preset;
		}

		public void SetHome()
		{
			PresetToGo = -1;
			HomeNeeded = true;
		}

		public async Task<bool> GoToPreset(int preset)
		{
			try
			{
				PresetToGo = preset;

				if (!Initialized)
					return false;

				if (await Cam.GoToPreset(preset).ConfigureAwait(false))
				{
					PresetToGo = -1;
					LastPreset = Presets[preset].name;
					return true;
				}
			}
			catch (Exception ex) { }
			return Initialized = false;
		}

		public async Task<bool> GoToHome()
		{
			try
			{
				if (!Initialized)
					return !(HomeNeeded = true);

				if (await Cam.GoToHome().ConfigureAwait(false))
				{
					LastPreset = "Home";
					return !(HomeNeeded = false);
				}
			}
			catch (Exception ex) { }
			return Initialized = !(HomeNeeded = true);
		}

		public void Popup() => PlayNeeded = true;

		public void Close() => StopNeeded = true;

		private void Play()
		{
			if (Window.Dispatcher.CheckAccess())
			{
				if (Window.Visibility != System.Windows.Visibility.Visible)
				{
					Window.Show();
					Window.Play();
					SetLocation(ToString() + "=>Preset: " + LastPreset);
					PlayNeeded = false;
				}
			}

			else
			{
				PlayerCallBack d = new PlayerCallBack(Play);
				Application.Current.Dispatcher.Invoke(d);
			}
		}

		private void Stop()
		{
			if (Window.Dispatcher.CheckAccess())
			{
				if (Window.Visibility == System.Windows.Visibility.Visible)
					Window.Close();

				StopNeeded = false;
			}

			else
			{
				PlayerCallBack d = new PlayerCallBack(Stop);
				Application.Current.Dispatcher.Invoke(d);
			}

		}

		public override void InitializeView()
		{
			Application.Current.Dispatcher.Invoke(new Action(() => {
				Window = new CamWindow();
			Player = Window.Player;
			Player.Cam = this;
            View = new CamView(this);
			}));
		}

		public override string GetString()
		{
			string output = string.Format("{0}{1}_-_{2}_-_{3}_-_{4}_-_{5}_-_{6}_-_{7}_-_",
					 base.GetString(), Username, Password, RtspPort, RtspStream, Cache, ProfileIndex, Brand?.Name);
			Presets.ForEach(preset => output += preset.name + ">>>" + preset.token + (preset != Presets.Last() ? "=+=" : ""));
			output += "_-_";
			Profiles.ForEach(profile => output += profile.name + ">>>" + profile.token + (profile != Profiles.Last() ? "=+=" : ""));
			return output;
		}

		public static Camera Parse(Plant site, string data)
		{
			try
			{
				Camera cam = new Camera(site);
				int index = 0;
				string[] pars = cam.Parse(data).Split(new string[] { "_-_" }, StringSplitOptions.None);

				cam.Username = pars[index++];
				cam.Password = pars[index++];
				cam.RtspPort = int.Parse(pars[index++]);
				cam.RtspStream = (Stream)Enum.Parse(typeof(Stream), pars[index++]);
				cam.Cache = int.Parse(pars[index++]);
				cam.ProfileIndex = int.Parse(pars[index++]);
				cam.Brand = Brands.Find(brand => brand.Name == pars[index]);

				if (pars.Length == index + 1)
					return cam;

				string[] presets = pars[++index].Split(new string[] { "=+=" }, StringSplitOptions.RemoveEmptyEntries);
				string[] temp;

				foreach (var preset in presets)
				{
					temp = preset.Split(new string[] { ">>>" }, StringSplitOptions.None);

					if (temp.Length != 2)
						continue;

					cam.Presets.Add(new PTZPreset() { name = temp[0], token = temp[1] });
				}

				if (pars.Length == index + 1)
					return cam;

				string[] profiles = pars[++index].Split(new string[] { "=+=" }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var profile in profiles)
				{
					temp = profile.Split(new string[] { ">>>" }, StringSplitOptions.None);
					cam.Profiles.Add(new Profile() { name = temp[0], token = temp[1] });
				}

				return cam;
			}
			catch (Exception ex) { }
			return null;
		}

		private async void Loop()
		{
			Random random = new Random(Index);

			while (true)
			{
				try
				{
					Thread.Sleep(100);

					if (!Plant.Cam.Contains(this))
						return;

					if (!Initialized || InitializeNeeded)
						await Initialize().ConfigureAwait(false);

					if (Initialized)
					{
						if (PresetToGo >= 0)
							await GoToPreset(PresetToGo).ConfigureAwait(false);

						if (HomeNeeded)
							await GoToHome().ConfigureAwait(false);

						if (PlayNeeded && LoggedIn)
							Play();
					}

					else
						Thread.Sleep(random.Next(100, 2000));

					if (StopNeeded)
						Stop();
				}
				catch (Exception ex) { }
			}
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

	public class CameraBrand
	{
		public string Name { get; set; }
		public string RTSP { get; set; }
		public RTSPType Type { get; set; }

		public static CameraBrand Default => new CameraBrand() { Name = "Default", RTSP = @"/ONVIF/MediaInput?profile=" };

		public enum RTSPType { Token, Index }
	}
}