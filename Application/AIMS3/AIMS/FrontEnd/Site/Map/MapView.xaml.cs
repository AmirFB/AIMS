using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AIMS3.FrontEnd.Interfaces;
using AIMS3.BackEnd.Modules;
using AIMS3.BackEnd.Site;

using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.Site.TelemetricSite;

namespace AIMS3.FrontEnd.Site.Map
{
	public partial class MapView : AIMSUserControl
	{
		public bool CanEdit { get; set; }
		private System.Drawing.Size _size;

		public const int XMargin = 50;
		public const int YMargin = 75;
		public const int XBias = 10;
		public const int YBias = 10;

		private enum MapEditStatus { Idle, Dragging, Deleting, Placing };
		private MapEditStatus status = MapEditStatus.Idle;

		Spot selected;

		[DllImport("user32.dll")]
		static extern void ClipCursor(ref System.Drawing.Rectangle rect);

		[DllImport("user32.dll")]
		static extern void ClipCursor(IntPtr rect);

		public MapView()
		{
			InitializeComponent();

			DataContext = this;
			_size = GetImageSize();

			MouseMove += Icon_MouseMove;
		}

		public void UpdateImage()
		{
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.UriSource = new Uri(GetImagePath());
			bitmapImage.EndInit();
			image.Source = bitmapImage;
		}

		private string GetImagePath() { try { return Plant != null && File.Exists(Site.MainDirectory + "map.png") ? Site.MainDirectory + "map.png" : "pack://application:,,,/Resources/DefaultMap.png"; } catch { return "pack://application:,,,/Resources/Icons/MainWindow/Pause.png"; } }

		public void UploadImage(string path)
		{
			try
			{
				File.WriteAllBytes(Site.MainDirectory + "map.png", File.ReadAllBytes(path));
				UpdateImage();
			}
			catch (Exception ex) { WriteToDebug(typeof(MapView), Plant.Name, nameof(UploadImage), ex); }
		}

		public void Place(Spot spot)
		{
			ClipCursor();

			if (!canvas.Children.Contains(spot))
				canvas.Children.Add(spot);

			spot.Cursor = Cursors.Hand;
			spot.Visibility = Visibility.Visible;

			spot.MouseLeftButtonDown += Icon_MouseDown;
			spot.MouseLeftButtonUp += Icon_MouseUp;
			spot.MouseMove += Icon_MouseMove;
			spot.KeyDown += Map_KeyDown;
			image.KeyDown += Map_KeyDown;

			selected = spot;
			status = MapEditStatus.Placing;
			spot.Refresh(false);
		}

		public System.Drawing.Size GetImageSize()
		{
			UpdateImage();
			double scale = Math.Max((double)(new BitmapImage(new Uri(GetImagePath()))).PixelHeight / image.ActualHeight, (double)(new BitmapImage(new Uri(GetImagePath()))).PixelWidth / image.ActualWidth);
			return new System.Drawing.Size((int)Math.Round((new BitmapImage(new Uri(GetImagePath()))).PixelWidth / scale), (int)Math.Round((new BitmapImage(new Uri(GetImagePath()))).PixelHeight / scale));
		}

		void ClipCursor()
		{
			Point point = image.PointToScreen(new System.Windows.Point(0, 0));//(int)(image.ActualWidth - size.Width) / 2, (int)(image.ActualHeight - size.Height) / 2));
			System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle(new System.Drawing.Point((int)point.X, (int)point.Y), new System.Drawing.Size(_size.Width + (int)point.X, _size.Height + (int)point.Y));
			ReleaseCursor();
			ClipCursor(ref rectangle);
		}

		static void ReleaseCursor() => ClipCursor(IntPtr.Zero);

		private void Icon_MouseMove(object sender, MouseEventArgs e)
		{
			if (!(status == MapEditStatus.Dragging || status == MapEditStatus.Placing))
				return;

			Point cursor = new System.Windows.Point(System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y);
			Point pointFromScreen = canvas.PointFromScreen(cursor);

			System.Drawing.Point location = new System.Drawing.Point((int)Math.Min(pointFromScreen.X - selected.ActualWidth / 2, canvas.RenderSize.Width), (int)Math.Min(pointFromScreen.Y - selected.ActualHeight / 2, canvas.ActualHeight));

			selected.SetValue(Canvas.LeftProperty, (double)location.X);
			selected.SetValue(Canvas.TopProperty, (double)location.Y);
		}

		private void Icon_MouseDown(object sender, MouseButtonEventArgs e)
		{
			Spot spot = sender as Spot;
			selected = spot;

			if (status != MapEditStatus.Idle || !CanEdit)
				return;

			spot.MouseMove += Icon_MouseMove;
			spot.MouseLeftButtonUp += Icon_MouseUp;
			image.MouseMove += Icon_MouseMove;

			status = MapEditStatus.Dragging;
			ClipCursor();
		}

		private async void Icon_MouseUp(object sender, MouseButtonEventArgs e)
		{
			var spot = sender as Spot;

			if (e.ChangedButton == MouseButton.Right)
				return;

			if (!CanEdit)
				return;

			if (status == MapEditStatus.Deleting)
			{
				var res = await Site.AuthenticateModifyModule(spot.Module).ConfigureAwait(false);

				if (res == true)
					Remove(spot, true);

				ShowMessageBoxAuthentication(res);
				return;
			}

			if (status != MapEditStatus.Dragging && status != MapEditStatus.Placing)
				return;

			double x = spot.X, y = spot.Y;

			spot.X = (Convert.ToInt32(spot.GetValue(Canvas.LeftProperty)) - (canvas.ActualWidth - _size.Width) / 2) / _size.Width;
			spot.Y = (Convert.ToInt32(spot.GetValue(Canvas.TopProperty)) - (canvas.ActualHeight - _size.Height) / 2) / _size.Height;

			spot.MouseMove -= Icon_MouseMove;
			spot.KeyDown -= Map_KeyDown;
			image.MouseMove -= Icon_MouseMove;
			status = MapEditStatus.Idle;
			ReleaseCursor();

			image.Cursor = null;

			var isDefault = spot.IsDefault;
			spot.IsPlaced = true;
			spot.IsDefault = false;

			var result = await Site.AuthenticateModifyModule(spot.Module).ConfigureAwait(false);

			if (result == true)
				Plant.SaveModules(false);

			else
			{
				spot.X = x;
				spot.Y = y;
				spot.IsDefault = isDefault;

				if (isDefault)
					SetLocationDefault(spot);

				else
					SetLocation(spot);
			}

			ShowMessageBoxAuthentication(result);
		}

		private void Map_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				ReleaseCursor();
				status = MapEditStatus.Idle;
			}
		}

		private void Image_SizeChanged(object sender, EventArgs e)
		{
			try
			{
				_size = GetImageSize();
				var xCount = (_size.Width - XBias * 2) / XMargin;
				var yCount = (_size.Height - XBias * 2) / YMargin;

				foreach (UIElement control in canvas.Children)
				{
					if (!(control is Spot))
						continue;

					var spot = control as Spot;

					if (spot.IsDefault)
						SetLocationDefault(spot, xCount, yCount);

					SetLocation(spot);
				}
			}
			catch (Exception ex) { WriteToDebug(typeof(MapView), Plant.Name, nameof(Image_SizeChanged), ex); }
		}

		private void Map_SizeChanged(object sender, SizeChangedEventArgs e) => Image_SizeChanged(sender, null);

		public void Remove(object sender, bool save)
		{
			Spot spot = sender as Spot;

			if ((spot.Owner as IFault)?.IsZone == true)
				return;

			canvas.Children.Remove(spot);
			spot.Remove(save);
		}

		public void RemoveAll() => Plant.Modules.ForEach(module => module.Icons.ForEach(icon => Remove(icon, false)));

		public void Add(Spot spot)
		{
			System.Drawing.Size size;

			if (!spot.IsPlaced)
				return;

			if (!canvas.Children.Contains(spot))
				canvas.Children.Add(spot);

			size = GetImageSize();

			spot.SetValue(Canvas.LeftProperty, size.Width * spot.X + (int)(canvas.ActualWidth - size.Width) / 2);
			spot.SetValue(Canvas.TopProperty, size.Height * spot.Y);
			spot.IsPlaced = true;

			spot.Cursor = Cursors.Hand;
			spot.Visibility = Visibility.Visible;

			spot.MouseLeftButtonDown += Icon_MouseDown;

			spot.Refresh(false);
		}

		public void Add(IModule module)
		{
			foreach (var icon in module.Icons)
			{
				if (icon.IsPlaced)
					Application.Current.Dispatcher.Invoke(new Action(() => { Add(icon); }), null);

				else
					Application.Current.Dispatcher.Invoke(new Action(() => { Remove(icon, false); }), null);
			}
		}

		public void Add(List<IModule> modules)
		{
			foreach (var module in modules)
				Add(module);
		}

		public void AddAll() => Add(Plant.Modules);

		private void SetLocation(Spot spot)
		{
			var location = new System.Drawing.Point();

			location = new System.Drawing.Point((int)Math.Round(_size.Width * spot.X) + (int)(canvas.ActualWidth - _size.Width) / 2, (int)Math.Round(_size.Height * spot.Y) + (int)(canvas.ActualHeight - _size.Height) / 2);
			spot.SetValue(Canvas.LeftProperty, (double)location.X);
			spot.SetValue(Canvas.TopProperty, (double)location.Y);
		}

		public void SetLocationDefault(Spot spot, int xCount = 0, int yCount = 0)
		{
			double x = _size.Width;
			double y = _size.Height;

			if (xCount == 0 && yCount == 0)
			{
				xCount = (_size.Width - XBias * 2) / XMargin;
				yCount = (_size.Height - XBias * 2) / YMargin;
			}

			var xMargin = (_size.Width - XBias * 2) / xCount;
			var yMargin = (_size.Height - YBias * 2) / yCount;
			var index = ((spot.Owner as IFault).ZoneIndex - 1) % (2 * xCount + 2 * yCount - 4);

			if (index < xCount)
			{
				spot.X = (index * xMargin + XBias) / x;
				spot.Y = YBias / y;
			}

			else if (index < (xCount + yCount - 1))
			{
				spot.X = ((xCount - 1) * xMargin + XBias) / x;
				spot.Y = ((index - xCount + 1) * yMargin + YBias) / y;
			}

			else if (index < (2 * xCount + yCount - 2))
			{
				spot.X = ((2 * xCount + yCount - 3 - index) * xMargin + XBias) / x;
				spot.Y = ((yCount - 1) * yMargin + YBias) / y;
			}

			else
			{
				spot.X = XBias / x;
				spot.Y = ((2 * xCount + 2 * yCount - 4 - index) * yMargin + YBias) / y;
			}
		}
	}
}