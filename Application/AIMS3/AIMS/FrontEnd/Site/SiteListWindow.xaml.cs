using System.Windows;
using AIMS3.BackEnd;
using AIMS3.BackEnd.Site;
using DevExpress.Xpf.Core;

using static AIMS3.BackEnd.Common;

namespace AIMS3.FrontEnd.Site
{
    public partial class SiteListWindow : ThemedWindow
    {
        public ObservableCollectionCore<ITelemetricSite> Sites { get; } = new ObservableCollectionCore<ITelemetricSite>();

        public SiteListWindow()
        {
            InitializeComponent();
            DataContext = this;
            Refresh();
        }

        private void Refresh()
        {
            Sites.Clear();
            Common.Sites.ForEach(site => Sites.Add(site));

            if (!Sites.Contains(comboBoxEditSites.SelectedItem as ITelemetricSite))
                comboBoxEditSites.SelectedIndex = 0;
        }

        private void Modify_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            (comboBoxEditSites.SelectedItem as ITelemetricSite)?.ShowSettings();
            Refresh();
        }

        private void Delete_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DXMessageBox.Show(this, GetResourceString("AskIfDeleteSite"), GetResourceString("Delete"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                MainWindow.DeleteSite(comboBoxEditSites.SelectedItem as ITelemetricSite);
                (comboBoxEditSites.SelectedItem as ITelemetricSite).DeleteSite();
                Refresh();
            }
        }

        private void NewLocal_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            (new SiteSettingsWindow(new LocalSite() { IsNew = true })).ShowDialog();
            Refresh();
        }

        private void NewRemote_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            (new SiteSettingsWindow(new RemoteSite() { IsNew = true })).ShowDialog();
            Refresh();
        }
    }
}