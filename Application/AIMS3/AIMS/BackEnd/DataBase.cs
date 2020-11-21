using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using System.Reflection;
using AIMS3.BackEnd.Cryptography;
using DevExpress.Xpf.Core;

using static System.Windows.Application;
using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.User;
using static AIMS3.BackEnd.Cryptography.AES;

namespace AIMS3.BackEnd
{
	public static class DataBase
	{
		public static string MinorSpacer => "\"Seperate minor Ones\"";
		public static string MajorSpacer => "\"Seperate Major Ones\"";
		private static byte[] ConfigCryptoKey => AES2.DefaultKey(13, AESType.AES256);
		private static byte[] ConfigCryptoIV => AES2.DefaultIV(14);
		private static byte[] AppConfigAESKey => AES2.DefaultKey(1, AESType.AES256);
		private static byte[] AppConfigAESIV => AES2.DefaultIV(193);
		private static byte[] UsersAESKey => AES2.DefaultKey(207, AESType.AES256);
		private static byte[] UsersAESIV => AES2.DefaultIV(111);

		internal static class Directories
		{
			internal static string Base => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\EHP\";
			internal static string Main => Base + @"AIMS 3\";
			public static string Sites => Main + @"Sites\";
			public static string Sounds => Main + @"Sounds\";
			public static string Log => Main + @"Log\";
			public static string Config => Main;
			public static string Users => Main;
			public static string Crypto => Main;
			public static string Brand => Main;
		}

		internal static class Extentions
		{
			public static string PlantModules => ".apm";
			public static string PlantSettings => ".aps";
			public static string SiteSettings => ".ass";
			public static string PlantDatabase => ".apd";
			public static string PlantConfig => ".apc";
			public static string Sound => ".wav";
			public static string Log => ".alg";
			public static string Config => ".adb";
			public static string Crypto => ".adb";
			public static string Users => ".adb";
			public static string Brand => ".acb";
			public static string FaultListImage => ".svg";
		}

		internal static class Files
		{
			public static DirectoryInfo Config { get; } = new DirectoryInfo(Directories.Config + "data1" + Extentions.Config);
			public static DirectoryInfo Crypto { get; } = new DirectoryInfo(Directories.Crypto + "data2" + Extentions.Crypto);
			public static DirectoryInfo Users { get; } = new DirectoryInfo(Directories.Users + "data3" + Extentions.Users);
			public static DirectoryInfo Brands { get; } = new DirectoryInfo(Directories.Brand + "Camera Brands" + Extentions.Brand);
		}

		public static string ReadFromFile(string path, AESType type = AESType.AES128)
		{
			try
			{
				if (!File.Exists(path))
					return null;

				return AIES.Decrypt(File.ReadAllBytes(path), type);
			}
			catch (Exception ex) { WriteToDebug(typeof(DataBase), path, nameof(ReadFromFile), ex); }
			return null;
		}

		public static bool WriteToFile(string path, string data, AESType type = AESType.AES128)
		{
			try
			{
				File.WriteAllBytes(path, AIES.Encrypt(data, type));
				return true;
			}
			catch (Exception ex) { WriteToDebug(typeof(DataBase), path, nameof(WriteToFile), ex); }
			return false;
		}

		public static bool LoadCrypto()
		{
			string[] rawData;

			try
			{
				rawData = AES2.Decrypt(File.ReadAllBytes(Files.Crypto.FullName), ConfigCryptoKey, ConfigCryptoIV).Split(';');

				string[] parameters;

				parameters = rawData[0].Split(',');

				Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
				config.AppSettings.Settings.Clear();

				UploadAppConfig("AES", parameters[0]);
				UploadAppConfig("UserKey", parameters[1]);
				UploadAppConfig("SafeKey1", parameters[2]);
				UploadAppConfig("SafeKey2", parameters[3]);
				UploadAppConfig("Bx", parameters[4]);
				UploadAppConfig("By", parameters[5]);
				UploadAppConfig("Bz", parameters[6]);
				UploadAppConfig("Bk", parameters[7]);
				UploadAppConfig("Px", parameters[8]);
				UploadAppConfig("Py", parameters[9]);
				UploadAppConfig("Pz", parameters[10]);
				UploadAppConfig("Pk", parameters[11]);

				UploadAppConfig("Requests", rawData[1]);
				UploadAppConfig("Responses", rawData[2]);

				return true;
			}
			catch (Exception ex) { }

			return false;
		}

		public static bool UploadAppConfig(string key, string value)
		{
			try
			{
				Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

				config.AppSettings.Settings.Remove(key);
				config.AppSettings.Settings.Add(key, Convert.ToBase64String(AES2.Encrypt(value, AppConfigAESKey, AppConfigAESIV)));
				config.Save(ConfigurationSaveMode.Full, true);
				ConfigurationManager.RefreshSection("appSettings");

				return true;
			}

			catch (Exception ex) { }

			return false;
		}

		public static string GetAppConfig(string key) => AES2.Decrypt(Convert.FromBase64String(ConfigurationManager.AppSettings.Get(key)), AppConfigAESKey, AppConfigAESIV);

		public static bool ClearAppConfig()
		{
			try
			{
				Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

				config.AppSettings.Settings.Clear();
				config.Save();

				return true;
			}

			catch (Exception ex) { }

			return false;
		}

		public static List<string> LoadUserStrings()
		{
			List<string> users = new List<string>();
			string data;

			try
			{
				data = AIB64.Decode(AES2.Decrypt(File.ReadAllBytes(Files.Users.FullName), UsersAESKey, UsersAESIV));
				users = data.Split(new string[] { MinorSpacer }, StringSplitOptions.RemoveEmptyEntries).ToList();
			}
			catch (Exception ex) { }

			return users;
		}

		public static List<string> LoadSecretUserStrings()
		{
			List<string> users = new List<string>();
			string data;
			byte[] temp;
			Stream stream;

			try
			{
				stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AIMS3.Resources.Files.data3.adb");
				temp = new byte[stream.Length];
				stream.Read(temp, 0, (int)stream.Length);
				data = AIB64.Decode(AES2.Decrypt(temp, UsersAESKey, UsersAESIV));
				users = data.Split(new string[] { MinorSpacer }, StringSplitOptions.RemoveEmptyEntries).ToList();
			}
			catch (Exception ex) { }

			return users;
		}

		public static ObservableCollectionCore<User> LoadUsers()
		{
			var users = new ObservableCollectionCore<User>();
			string data;
			string[] temp;

			try
			{
				data = AIB64.Decode(AES2.Decrypt(File.ReadAllBytes(Files.Users.FullName), UsersAESKey, UsersAESIV));

				foreach (string user in data.Split(new string[] { MinorSpacer }, StringSplitOptions.RemoveEmptyEntries))
				{
					temp = user.Split(new string[] { ",,,,," }, StringSplitOptions.RemoveEmptyEntries);
					users.Add(new User((UserAuthority)Enum.Parse(typeof(UserAuthority), temp[0])) { Username = temp[1] });
				}
			}
			catch (Exception ex) { }

			return users;
		}

		public static List<User> LoadSecretUsers()
		{
			List<User> users = new List<User>();
			string data;
			string[] temp;
			byte[] tempByte;
			Stream stream;

			try
			{
				stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AIMS3.Resources.Files.data3.adb");
				tempByte = new byte[stream.Length];
				stream.Read(tempByte, 0, (int)stream.Length);
				data = AIB64.Decode(AES2.Decrypt(tempByte, UsersAESKey, UsersAESIV));

				foreach (string user in data.Split(new string[] { MinorSpacer }, StringSplitOptions.RemoveEmptyEntries))
				{
					temp = user.Split(new string[] { ",,,,," }, StringSplitOptions.RemoveEmptyEntries);
					users.Add(new User((UserAuthority)Enum.Parse(typeof(UserAuthority), temp[0])) { Username = temp[1] });
				}
			}
			catch (Exception ex) { }
			return users;
		}

		public static bool Save(UserAuthority authority, string username, string password)
		{
			try
			{
				List<string> users = new List<string>();
				int index;

				if (!File.Exists(Files.Users.FullName))
					File.WriteAllText(Files.Users.FullName, "");

				byte[] data = File.ReadAllBytes(Files.Users.FullName);

				if (data.Length > 10)
					users = AIB64.Decode(AES2.Decrypt(File.ReadAllBytes(Files.Users.FullName), UsersAESKey, UsersAESIV)).Split(new string[] { MinorSpacer }, StringSplitOptions.RemoveEmptyEntries).ToList();

				index = users.FindIndex(user => user.Contains(",,,,," + username + ",,,,,"));

				if (index >= 0)
					users.RemoveAt(index);

				users.Add(string.Format("{0},,,,,{1},,,,,{2}", authority, username, SHA.Hash(username + password)));

				users.Sort();

				List<string> dummy = new List<string>();

				foreach (string str in users)
					if (str.Contains(UserAuthority.Expert + ",,,,,"))
						dummy.Add(str);

				foreach (string str in users)
					if (str.Contains((UserAuthority.Admin) + ",,,,,"))
						dummy.Add(str);

				foreach (string str in users)
					if (str.Contains((UserAuthority.Operator) + ",,,,,"))
						dummy.Add(str);

				string temp = dummy[0];

				foreach (String user in dummy.GetRange(1, dummy.Count - 1))
					temp += MinorSpacer + user;

				File.WriteAllBytes(Files.Users.FullName, AES2.Encrypt(AIB64.Encode(temp), UsersAESKey, UsersAESIV));

				return true;
			}
			catch (Exception ex) { return false; }
		}

		public static bool DeleteUser(string username)
		{
			try
			{
				List<string> users = new List<string>();
				int index;

				if (!File.Exists(Files.Users.FullName))
					File.WriteAllText(Files.Users.FullName, "");

				byte[] data = File.ReadAllBytes(Files.Users.FullName);

				if (data.Length > 10)
					users = AIB64.Decode(AES2.Decrypt(File.ReadAllBytes(Files.Users.FullName), UsersAESKey, UsersAESIV)).Split(new string[] { MinorSpacer }, StringSplitOptions.RemoveEmptyEntries).ToList();

				index = users.FindIndex(user => user.Contains(",,,,," + username + ",,,,,"));

				if (index >= 0)
					users.RemoveAt(index);

				users.Sort();

				List<string> dummy = new List<string>();

				foreach (string str in users)
					if (str.Contains(UserAuthority.Expert + ",,,,,"))
						dummy.Add(str);

				foreach (string str in users)
					if (str.Contains((UserAuthority.Admin) + ",,,,,"))
						dummy.Add(str);

				foreach (string str in users)
					if (str.Contains((UserAuthority.Operator) + ",,,,,"))
						dummy.Add(str);

				string temp = dummy[0];

				foreach (String user in dummy.GetRange(1, dummy.Count - 1))
					temp += MinorSpacer + user;

				File.WriteAllBytes(Files.Users.FullName, AES2.Encrypt(AIB64.Encode(temp), UsersAESKey, UsersAESIV));

				return true;
			}
			catch (Exception ex) { return false; }
		}
	}
}