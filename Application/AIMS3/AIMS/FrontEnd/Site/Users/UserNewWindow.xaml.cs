using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AIMS3.BackEnd;
using DevExpress.Xpf.Core;
using static AIMS3.BackEnd.User;

namespace AIMS3.FrontEnd.Site.Users
{
	public partial class UserNewWindow : ThemedWindow, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		private User User { get; set; }

		public string IsMatched
		{
			get
			{
				if (string.IsNullOrEmpty(passwordBoxEditPassword.Text) && !string.IsNullOrEmpty(passwordBoxEditConfirmPassword.Text))
					return "";

				if (passwordBoxEditPassword.Text == passwordBoxEditConfirmPassword.Text)
					return (string)TryFindResource("PasswordsMatch");

				else
					return (string)TryFindResource("PasswordsNotMatch");
			}
		}

		public Brush IsMatchedBrush => passwordBoxEditPassword.Text == passwordBoxEditConfirmPassword.Text ? Brushes.Green : Brushes.Red;

		protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		public UserNewWindow()
		{
			InitializeComponent();

			User = new User(User.UserAuthority.Operator);

			DataContext = this;
		}

		private void Password_KeyUp(object sender, KeyEventArgs e)
		{
			OnPropertyChanged(nameof(IsMatched));
			OnPropertyChanged(nameof(IsMatchedBrush));
		}

		private void Save()
		{
			DataBase.Save((UserAuthority)comboBoxEditAuthority.SelectedIndex, textEditUsername.Text, passwordBoxEditPassword.Text);
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(textEditUsername.Text))
			{
				DXMessageBox.Show((string)TryFindResource("UsernameEmpty"));
				return;
			}

			if (string.IsNullOrEmpty(passwordBoxEditPassword.Text) && string.IsNullOrEmpty(passwordBoxEditConfirmPassword.Text))
			{
				DXMessageBox.Show((string)TryFindResource("PasswordsAndConfirmEmpty"));
				return;
			}

			if (passwordBoxEditPassword.Text != passwordBoxEditConfirmPassword.Text)
			{
				DXMessageBox.Show((string)TryFindResource("PasswordsNotMatch"));
				return;
			}

			if (User.IsUser(textEditUsername.Text))
			{
				DXMessageBox.Show((string)TryFindResource("UserExists"));
				return;
			}

			if (Common.CurrentUser.Authority == UserAuthority.Admin && comboBoxEditAuthority.SelectedIndex == 0)
			{
				DXMessageBox.Show((string)TryFindResource("NotAuthorized"));
				return;
			}

			Save();
			Close();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
