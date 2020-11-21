using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AIMS3.BackEnd.Cryptography;
using AIMS3.BackEnd.Site;
using DevExpress.Xpf.Core;

using static System.Array;
using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.Cryptography.AES;
using static AIMS3.BackEnd.DataBase;
using static AIMS3.BackEnd.Modules.Fault;
using static AIMS3.BackEnd.Site.TelemetricSite;

namespace AIMS3.BackEnd
{
	public class Log
	{
		private ITelemetricSite site;
		public virtual ITelemetricSite Site
		{
			get => site;
			set
			{
				site = value;
				plant = site.Plant;
			}
		}

		private Plant plant;
		public virtual Plant Plant
		{
			get => plant;
			set
			{
				plant = value;
				site = plant.Owner;
			}
		}

		private static byte[] AESKey => AES2.DefaultKey(222, AESType.AES128);
		private static byte[] AESIV => AES2.DefaultIV(388);

		public const string DateFormat = "yyyy-MM-dd";
		public const string TimeFormat = "HH:mm:ss";
		private const string Spacer = "\r\n";

		object lockObject = new object();

		private string DateToPath(string date) => Site.LogDirectory + date + Extentions.Log;

		public static DateTime[] GetDays(string from, string to)
		{
			DateTime start = DateTime.Parse(from);
			DateTime end = DateTime.Parse(to);
			int count = Math.Max((end - start).Days + 1, 0);
			DateTime[] days = new DateTime[count];

			for (int i = 0; i < count; i++)
			{
				days[i] = start;
				start += TimeSpan.FromDays(1);
			}

			return days;
		}

		public void Save(string status, string description, string name, FaultReason? reason, string module)
		{
			var thread = new Thread(() =>
			{
				lock (lockObject)
				{
					var date = DateTime.Now.Date.ToString(DateFormat, CultureInfo.InvariantCulture);
					var data = 
						$"{DateTime.Now.ToString(DateFormat)},{DateTime.Now.ToString(TimeFormat)},{status},{description},{((name != module || module == null) ? name : "SOS")},{module},{(reason?.ToString())},{CurrentUser?.Username}";

					if (!Directory.Exists(Site.LogDirectory))
						Directory.CreateDirectory(Site.LogDirectory);

					ToLog(date, data);
				}
			});

			thread.Name = "Save Log";
			thread.Start();
		}

		public Record Parse(string data)
		{
			try
			{
				var index = 0;
				var pars = data.Split(',');

				return new Record()
				{
					Date = pars[index++],
					Time = pars[index++],
					Status = pars[index++],
					Description = pars[index++],
					Name = pars[index++],
					Module = pars[index++],
					Reason = pars[index++],
					User = pars[index++],
				};
			}
			catch (Exception ex) { WriteToDebug(typeof(Log), Plant.Name, nameof(Parse), ex); }
			return null;
		}

		public List<Record> Split(string data)
		{
			var records = new List<Record>();
			var raws = data.Split(new string[] { Spacer }, StringSplitOptions.RemoveEmptyEntries);
			ForEach(raws, raw => records.Add(Parse(raw)));
			return records;
		}

		private List<Record> Read(string date)
		{
			var records = new List<Record>();
			var data = FromLog(date);
			ForEach(data, dat => records.AddRange(Split(dat)));
			return records;
		}

		public List<Record> Read(string from, string to)
		{
			var days = GetDays(from, to);
			var record = new List<Record>[days.Length];

			lock (lockObject)
				Parallel.ForEach(days, (day) => {
					record[IndexOf(days, day)] = new List<Record>();
					record[IndexOf(days, day)].AddRange(Read(day.ToString(DateFormat, CultureInfo.InvariantCulture)));
				});

			var records = new List<Record>();
			ForEach(record, rec => records.AddRange(rec));
			return records;
		}

		private string[] ReadRaw(string from, string to)
		{
			var days = GetDays(from, to);
			var record = new string[days.Length];

			lock (lockObject)
				Parallel.ForEach(days, (day) => { record[IndexOf(days, day)] = FromLogRaw(day.ToString(DateFormat, CultureInfo.InvariantCulture)); });

			return record;
		}

		internal string GetRawData(string data)
		{
			var pars = data.Split(new string[] { Spacers.Date }, StringSplitOptions.None);
			var output = "";
			ForEach(ReadRaw(pars[0], pars[1]), day => output += day + Spacers.Report);
			return output;
		}

		private void ToLog(string date, string data)
		{
			try
			{
				var path = DateToPath(date);
				string old = FromLogRaw(date) + Spacer;
				File.WriteAllBytes(path, AES2.Encrypt(old + data, AESKey, AESIV));
			}
			catch (Exception ex) { DXMessageBox.Show(ex.ToString()); }
		}

		private string FromLogRaw(string date)
		{
			try
			{
				var path = DateToPath(date);

				lock (path)
					return File.Exists(path) ? AES2.Decrypt(File.ReadAllBytes(path), AESKey, AESIV) : "";
			}
			catch (Exception ex) { DXMessageBox.Show(ex.ToString()); }
			return null;
		}

		private string[] FromLog(string date) => FromLogRaw(date).Split(new string[] { Spacer }, StringSplitOptions.RemoveEmptyEntries);
	}

	public class Record
	{
		public string Date { get; set; }
		public string Time { get; set; }
		public string Status { get; set; }
		public string Description { get; set; }
		public string Name { get; set; }
		public string Module { get; set; }
		public string Reason { get; set; }
		public string User { get; set; }
	}
}