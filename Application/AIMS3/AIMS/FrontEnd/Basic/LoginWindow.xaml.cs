using System;
using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using AIMS3.BackEnd;
using AIMS3.BackEnd.Modules;
using AIMS3.BackEnd.Site;

using static AIMS3.BackEnd.User;
using static AIMS3.BackEnd.Common;

namespace AIMS3.FrontEnd.Basic
{
	public partial class LoginWindow : ThemedWindow
	{
		public LoginWindow()
		{
			InitializeComponent();
			this.Loaded += new RoutedEventHandler(LoginWindow_Loaded);
		}

		void LoginWindow_Loaded(object sender, RoutedEventArgs e)
		{
			//DXSplashScreen.Close();
			this.Activate();
		}

		private void Clear()
		{
			textEditUsername.Text = "";
			passwordBoxEditPassword.Password = "";
		}

		private void Login_Click(object sender, RoutedEventArgs e)
		{
			if (Authenticate(textEditUsername.Text, passwordBoxEditPassword.Password))
				Login();

			else
				DXMessageBox.Show((string)TryFindResource("UserPassWrong"));
		}

		private void Login()
		{
			ResourceDictionary dict = new ResourceDictionary();
			CurrentUser = GetUser(textEditUsername.Text);

			switch (CurrentUser.Authority)
			{
				case UserAuthority.Expert:
					dict.Source = new Uri("..\\Resources\\Authority\\Expert.xaml", UriKind.Relative);
					break;

				case UserAuthority.Admin:
					dict.Source = new Uri("..\\Resources\\Authority\\Admin.xaml", UriKind.Relative);
					break;

				case UserAuthority.Operator:
					dict.Source = new Uri("..\\Resources\\Authority\\Operator.xaml", UriKind.Relative);
					break;
			}

			Application.Current.Resources.MergedDictionaries.Add(dict);
			DialogResult = true;
			Close();
			LoggedIn = true;
		}

		private void Enter_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				Login_Click(sender, null);
				e.Handled = true;
			}
		}
	}
}