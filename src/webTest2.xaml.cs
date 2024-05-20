﻿using EQTool.Models;
using EQTool.Services;
using System;
using System.Web;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace EQTool
{
	/// <summary>
	/// Interaction logic for webTest.xaml
	/// </summary>
	public partial class WebTest2 : BaseSaveStateWindow
	{
		EQToolSettings _settings;
		private readonly IAppDispatcher appDispatcher;
		public WebTest2(
			EQToolSettings settings,
			EQToolSettingsLoad toolSettingsLoad,
			IAppDispatcher appDispatcher) : base(settings.WebViewWindowState, toolSettingsLoad, settings)
		{
			_settings = settings;
			this.appDispatcher = appDispatcher;
			InitializeComponent();

			this.Topmost = true;
			webViewerBorder.Background = new SolidColorBrush() { Color = Colors.White,  Opacity = 0 };
			webViewer.DefaultBackgroundColor = System.Drawing.Color.FromArgb(0, 0, 0, 0);

			_settings.DiscordSettings.DiscordServer = "0";
			_settings.DiscordSettings.DiscordChannel = "1";

			Uri uri = new UriBuilder(_settings.DiscordSettings.DiscordUrl).Uri;
			webViewer.Source = uri;
		}

		private void Grid_MouseEnter(object sender, MouseEventArgs e)
		{
			WindowResizeChrome.ResizeBorderThickness = new Thickness(8);
			webViewerBorder.BorderThickness = new Thickness(1, 1, 1, 1);
			LastWindowInteraction = DateTime.UtcNow;
		}

		private void Grid_MouseLeave(object sender, MouseEventArgs e)
		{
			System.Threading.Tasks.Task.Factory.StartNew(() =>
			{
				while (Math.Abs((DateTime.UtcNow - LastWindowInteraction).TotalSeconds) < 10)
				{
					System.Threading.Thread.Sleep(1000 * 1);
				}
				this.appDispatcher.DispatchUI(() =>
				{
					WindowResizeChrome.ResizeBorderThickness = new Thickness(0);
					webViewerBorder.BorderThickness = new Thickness(0, 0, 0, 0);
				});
			});
		}
	}
}