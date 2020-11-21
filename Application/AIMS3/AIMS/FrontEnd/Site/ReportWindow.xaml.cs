using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using Microsoft.Office.Interop.Excel;
using AIMS3.BackEnd;
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Interfaces;
using DevExpress.Xpf.Grid;

using static System.Array;
using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.Site.TelemetricSite;

using Application = Microsoft.Office.Interop.Excel.Application;

namespace AIMS3.FrontEnd.Site
{
	public partial class ReportWindow : AIMSWindow
	{
		public Visibility RemoteVisibility => Plant.Type == SiteType.Remote ? Visibility.Visible : Visibility.Collapsed;

		public List<Record> Logs { get; } = new List<Record>();

		public ReportWindow()
		{
			InitializeComponent();

			DataContext = this;
		}

		public ReportWindow(Plant plant)
		{
			InitializeComponent();

			Plant = plant;
			DataContext = this;
		}

		private void Fill(List<Record> records)
		{
			Logs.Clear();
			Logs.AddRange(records);
			gridControl.GetBindingExpression(GridControl.ItemsSourceProperty).UpdateTarget();

			tableView.BestFitColumns();
			gridControl.RefreshData();
		}

		private List<Record> Parse(string data)
		{
			var days = data.Split(new string[] { Spacers.Report }, StringSplitOptions.RemoveEmptyEntries);
			var records = new List<Record>();
			ForEach(days, day => records.AddRange(Plant.Log.Split(day)));
			return records;
		}

		private void Show_Click(object sender, RoutedEventArgs e) => Fill(Plant.Log.Read(dateEditFrom.DateTime.ToString(Log.DateFormat, CultureInfo.InvariantCulture), dateEditTo.DateTime.ToString(Log.DateFormat, CultureInfo.InvariantCulture)));

		private async void ShowLocal_Click(object sender, RoutedEventArgs e)
		{
			var result = await Site.GetReport(dateEditFrom.DateTime.ToString(Log.DateFormat, CultureInfo.InvariantCulture), dateEditTo.DateTime.ToString(Log.DateFormat, CultureInfo.InvariantCulture)).ConfigureAwait(false);

			if (result.Success == true)
				Fill(Parse(result.Data));

			ShowMessageBoxAuthentication(result.Success);
		}

		private void Export_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Application oXL = new Application() { Visible = true };
				Workbook oWB;
				Worksheet oSheet;

				oWB = oXL.Workbooks.Add(System.Reflection.Missing.Value);
				oSheet = (Worksheet)oWB.ActiveSheet;

				oSheet.Cells[1, 1] = FindResource("Date");
				oSheet.Cells[1, 2] = FindResource("Time");
				oSheet.Cells[1, 3] = FindResource("Status");
				oSheet.Cells[1, 4] = FindResource("Description");
				oSheet.Cells[1, 5] = FindResource("Module");
				oSheet.Cells[1, 6] = FindResource("Name");
				oSheet.Cells[1, 7] = FindResource("Reason");
				oSheet.Cells[1, 8] = FindResource("User");

				for (int i = 1; i <= 8; i++)
					oSheet.Cells[1, i].HorizontalAlignment = XlHAlign.xlHAlignCenter;

				int row = 2;

				foreach (var record in Logs)
				{
					oSheet.Cells[row, 1] = record.Date;
					oSheet.Cells[row, 2] = record.Time;
					oSheet.Cells[row, 3] = record.Status;
					oSheet.Cells[row, 4] = record.Description;
					oSheet.Cells[row, 5] = record.Module;
					oSheet.Cells[row, 6] = record.Name;
					oSheet.Cells[row, 7] = record.Reason;
					oSheet.Cells[row++, 8] = record.User;
				}

				oSheet.Columns.AutoFit();
			}
			catch (Exception ex) { WriteToDebug(typeof(ReportWindow), "", nameof(Export_Click), ex); }
		}

		public override void Load()
		{
			base.Load();
			Logs.Clear();
			gridControl.RefreshData();
		}
	}
}