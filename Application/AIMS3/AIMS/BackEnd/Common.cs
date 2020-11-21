using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Win32;
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Main;

using static AIMS3.BackEnd.User;
using Application = System.Windows.Application;

namespace AIMS3.BackEnd
{
	public static class Common
	{
		public static List<ITelemetricSite> Sites { get; } = new List<ITelemetricSite>();
		public static List<Plant> Plants { get; } = new List<Plant>();
		public static ARMLock Lock { get; set; } = new ARMLock();
		public static User CurrentUser { get; set; } = new User(UserAuthority.Operator);

		public static MainWindow MainWindow { get; set; }
		public static bool LoggedIn { get; set; }

		private static Language language;
		public static Language Tongue
		{
			get => language;
			set
			{
				if (language != value)
				{
					language = value;
					SetLanguage();
					SaveLanguage();
				}
			}
		}

		public static UserAuthority ExpertAuthority { get; set; }
		public static UserAuthority AdminAuthority { get; set; }
		public static UserAuthority OperatorAuthority { get; set; }

		public enum Language : int { English, Farsi };

		private static NotifyIcon notifyIcon = new NotifyIcon() { Visible = true, Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location) };

		public static string GetResourceString(string key) => (string)Application.Current.FindResource(key);
		public static bool GetResourceBool(string key) => (bool)Application.Current.FindResource(key);

		public static void ShowNotification(string text, ITelemetricConnection client, string user)
		{
			var message = string.Format("{0}\r\n{1}=>{2}", text, client?.HostName, user);
			notifyIcon.ShowBalloonTip(3000, "AIMS", message, ToolTipIcon.Info);
		}

		public static void SetLanguage()
		{
			ResourceDictionary dict = new ResourceDictionary();

			switch (language)
			{
				case Language.English:
					dict.Source = new Uri("..\\Resources\\Dictionaries\\Dictionary-en.xaml", UriKind.Relative);
					break;

				case Language.Farsi:
					dict.Source = new Uri("..\\Resources\\Dictionaries\\Dictionary-fa.xaml", UriKind.Relative);
					break;
			}

			Application.Current.Resources.MergedDictionaries.Add(dict);
		}

		public static void WriteToDebug(Type type, string name, string action, object ex)
		{
			var thread = new Thread(() =>
			{
				Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss - ") + type + " -> " + name + ", " + action + ": " + ex);
			});

			thread.Name = nameof(WriteToDebug);
			thread.Start();
		}

		public static void SaveLanguage() => Registry.CurrentUser.CreateSubKey(@"SOFTWARE\EHP\AIMS3").SetValue("Language", Tongue);
		public static void LoadLanguage()
		{
			Registry.CurrentUser.CreateSubKey(@"SOFTWARE\EHP\AIMS3");
			Tongue = (Language)Enum.Parse(typeof(Language), (string)Registry.CurrentUser.OpenSubKey(@"SOFTWARE\EHP\AIMS3").GetValue("Language", "English"));
		}
	}
}