using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using AIMS3.BackEnd;
using AIMS3.FrontEnd.Interfaces;

using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.DataBase;
using static AIMS3.BackEnd.User;

namespace AIMS3.FrontEnd.Site.Users
{
	public partial class UsersWindow : AIMSWindow
	{
		public ObservableCollectionCore<User> Users { get; set; } = new ObservableCollectionCore<User>();

		public UsersWindow()
		{
			InitializeComponent();

			RefreshList();
			DataContext = this;
		}

		private void RefreshList()
		{
			Users.Clear();

			foreach (var user in LoadUsers())
				Users.Add(user);

			//comboBoxEditUser.GetBindingExpression(ComboBoxEdit.ItemsSourceProperty).UpdateTarget();

			if (!Users.Contains(comboBoxEditUser.SelectedItem))
				comboBoxEditUser.SelectedIndex = 0;
		}

		private void Modify_Click(object sender, RoutedEventArgs e)
		{
			if (!Users.Contains(comboBoxEditUser.EditValue))
			{
				DXMessageBox.Show((string)TryFindResource("UserNotFound"));
				return;
			}

			if (CurrentUser.Authority == UserAuthority.Admin && ((User)comboBoxEditUser.EditValue).Authority == UserAuthority.Expert)
			{
				DXMessageBox.Show((string)TryFindResource("NotAuthorized"));
				return;
			}

			(new UserModifyWindow((User)comboBoxEditUser.EditValue)).ShowDialog();
			RefreshList();
		}

		private void Delete_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentUser.Username == ((User)comboBoxEditUser.SelectedItem).Username)
			{
				DXMessageBox.Show((string)TryFindResource("NotYourself"));
				return;
			}

			if (CurrentUser.Authority == UserAuthority.Admin && ((User)comboBoxEditUser.SelectedItem).Authority == UserAuthority.Expert)
			{
				DXMessageBox.Show((string)TryFindResource("NotAuthorized"));
				return;
			}

			if (DXMessageBox.Show((string)TryFindResource("AskDelete"), (string)TryFindResource("Logout"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
				return;

			DeleteUser(((User)comboBoxEditUser.SelectedItem).Username);
			RefreshList();
		}

		private void New_Click(object sender, RoutedEventArgs e)
		{
			(new UserNewWindow()).ShowDialog();
			RefreshList();
		}
	}
}