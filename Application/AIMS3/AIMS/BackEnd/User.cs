using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AIMS3.BackEnd.Cryptography;

namespace AIMS3.BackEnd
{
	public class User
	{
		public enum UserAuthority { Expert, Admin, Operator }
        public string Username { get; set; }

        public User (UserAuthority authority)
        {
			Authority = authority;
        }

        public class UserAuthorityPermissions : INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;

			private bool settings;
			public bool Settings
			{
				get => settings;
				set
				{
					settings = value;
					OnPropertyChanged(nameof(Settings));
				}
			}

			private bool modules;
			public bool Modules
			{
				get => modules;
				set
				{
					modules = value;
					OnPropertyChanged(nameof(Modules));
				}
			}

			private bool map;
			public bool Map
			{
				get => map;
				set
				{
					map = value;
					OnPropertyChanged(nameof(Map));
				}
			}

			private bool moduleOnMap;
			public bool ModuleOnMap
			{
				get => moduleOnMap;
				set
				{
					moduleOnMap = value;
					OnPropertyChanged(nameof(ModuleOnMap));
				}
			}

			private bool editAdmin;
			public bool EditAdmin
			{
				get => editAdmin;
				set
				{
					editAdmin = value;
					OnPropertyChanged(nameof(EditAdmin));
				}
			}

			private bool editOperator;
			public bool EditOperator
			{
				get => editOperator;
				set
				{
					editOperator = value;
					OnPropertyChanged(nameof(EditOperator));
				}
			}

			private bool editPlant;
			public bool EditPlant
			{
				get => editPlant;
				set
				{
					editPlant = value;
					OnPropertyChanged(nameof(EditPlant));
				}
			}

			private bool search;
			public bool Search
			{
				get => search;
				set
				{
					search = value;
					OnPropertyChanged(nameof(Search));
				}
			}

			protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

			public new string ToString()
			{
				string data = string.Format("{0},{1},{2},{3},{4},{5},{6},{7}", settings, modules, map, moduleOnMap, editAdmin, editOperator, editPlant, search);

				return data;
			}
		}

		public UserAuthority Authority { get; set; }

		public override string ToString() => string.Format("{0}({1})", Username, Authority);

		public static bool Authenticate(string username, string password)
        {
			try
			{
				List<string> users = DataBase.LoadUserStrings().ToList();
				int index = users.FindIndex(user => user.Contains(",,,,," + username + ",,,,," + SHA.Hash(username + password)));

				if (index >= 0)
					return true;

				users = DataBase.LoadSecretUserStrings().ToList();
				index = users.FindIndex(user => user.Contains(",,,,," + username + ",,,,," + SHA.Hash(username + password)));

				if (index >= 0)
					return true;
			}

			catch (Exception ex) { }

            return false;
		}

		public static bool IsUser(string username)
		{
			try
			{
				List<string> users = DataBase.LoadUserStrings().ToList();
				int index = users.FindIndex(user => user.Contains(",,,,," + username + ",,,,,"));

				if (index >= 0)
					return true;
			}
			catch (Exception ex) { }

			return false;
		}

		public static User GetUser(string username)
		{
			User user = new User(UserAuthority.Operator);

			try
			{
				List<User> users = DataBase.LoadUsers().ToList();
				int index = users.FindIndex(match => match.Username == username);

				if (index >= 0)
					user = users[index];

				else
				{
					users = DataBase.LoadSecretUsers().ToList();
					index = users.FindIndex(match => match.Username == username);

					if (index >= 0)
						user = users[index];
				}
			}
			catch (Exception ex) { }

			return user;
		}
	}
}