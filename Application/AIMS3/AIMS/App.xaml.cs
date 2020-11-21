using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using DevExpress.Xpf.Core;
using AIMS3.BackEnd;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Basic;
using AIMS3.FrontEnd.Main;

using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.DataBase;
using static AIMS3.BackEnd.Site.TelemetricSite;

namespace AIMS3
{
	public partial class App : Application
	{
		static new MainWindow MainWindow { get => Common.MainWindow; set => Common.MainWindow = value; }
		LoginWindow LoginWindow { get; set; }
		static readonly Mutex mutex = new Mutex(true, "AIMS");
		private DummyForm dummy;

		public App()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{

		}

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			//DXSplashScreen.Show<SplashScreenView>();

			if (!mutex.WaitOne(0, false))
			{
				DXMessageBox.Show(GetResourceString("AlreadyRunning"));
				Environment.Exit(0);
			}

			PopulateFiles();

			dummy = new DummyForm();
			LoadCrypto();
			Lock = new ARMLock { arm = dummy.axARMClass };

			if (!Lock.Initialize())
			{
				DXMessageBox.Show((string)TryFindResource("LockNotFound"));
				Environment.Exit(0);
				return;
			}

			LoadLanguage();
			SetLanguage();

			MainWindow = new MainWindow(Sites);
			MainWindow.Closing += MainWindow_Closing;
			PopulateSites();

			ShowLogin();
		}

		private void Show()
		{
			if (MainWindow == null)
			{
				MainWindow = new MainWindow(Sites);
				MainWindow.Closing += MainWindow_Closing;
			}

			MainWindow.LoggedOut = false;
			MainWindow.Show();
		}

		private void ShowLogin()
		{
			LoginWindow = new LoginWindow();
			LoginWindow.Closed += LoginWindow_Closed;
			bool? result = LoginWindow.ShowDialog();

			if (result == true)
				Show();
		}

		private void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			e.Cancel = true;
			MainWindow.Visibility = Visibility.Hidden;

			if (!MainWindow.LoggedOut)
				Environment.Exit(0);

			else
				ShowLogin();
		}

		private void LoginWindow_Closed(object sender, EventArgs e)
		{
			if (LoginWindow.DialogResult != true)
				Environment.Exit(0);
		}

		private void PopulateFiles()
		{
			byte[] temp;
			Stream stream;

			if (!Directory.Exists(Directories.Base))
				Directory.CreateDirectory(Directories.Base);

			if (!Directory.Exists(Directories.Main))
				Directory.CreateDirectory(Directories.Main);

			if (!Directory.Exists(Directories.Sites))
				Directory.CreateDirectory(Directories.Sites);

			if (!File.Exists(Files.Config.FullName))
			{
				stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AIMS3.Resources.Files.data1.adb");
				temp = new byte[stream.Length];
				stream.Read(temp, 0, (int)stream.Length);
				File.WriteAllBytes(Files.Config.FullName, temp);
			}

			if (!File.Exists(Files.Crypto.FullName))
			{
				stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AIMS3.Resources.Files.data2.adb");
				temp = new byte[stream.Length];
				stream.Read(temp, 0, (int)stream.Length);
				File.WriteAllBytes(Files.Crypto.FullName, temp);
			}

			if (!File.Exists(Files.Users.FullName))
				File.Create(Files.Users.FullName);
		}

		private void PopulateSites()
		{
			Camera.UploadBrands();
			LoadSites();

			foreach (var site in Sites)
			{
				MainWindow.AddSite(site);
				site.View.PopulateIcons();
			}
		}

		private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			e.Handled = true;
			DXMessageBox.Show(e.Exception.ToString());
		}
	}
}