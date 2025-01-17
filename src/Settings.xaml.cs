﻿using EQTool.Factories;
using EQTool.Models;
using EQTool.Services;
using EQTool.Services.Spells.Log;
using EQTool.ViewModels;
using EQToolShared.Enums;
using EQToolShared.ExtendedClasses;
using EQToolShared.HubModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Xceed.Wpf.Toolkit;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace EQTool
{
	public class BoolStringClass : INotifyPropertyChanged
	{
		public string TheText { get; set; }

		public PlayerClasses TheValue { get; set; }

		private bool _IsChecked { get; set; }

		public bool IsChecked
		{
			get => _IsChecked; set
			{
				_IsChecked = value;
				OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}

	public partial class Settings : BaseSaveStateWindow
	{
		private readonly SettingsWindowViewModel _viewModel;
		private readonly EQToolSettings _settings;
		private readonly EQToolSettingsLoad _toolSettingsLoad;
		private readonly BaseTimerWindowViewModel _spellWindowViewModel;
		private readonly EQSpells _spells;
		private readonly DPSLogParse _dPSLogParse;
		private readonly TimerWindowFactory _timerWindowFactory;
		private readonly TimerWindowService _timerWindowService;
		private readonly IAppDispatcher _appDispatcher;
		private readonly ISignalrPlayerHub _signalrPlayerHub;
		private readonly LogParser _logParser;
		private readonly PipeParser _pipeParser;
		private readonly MapLoad _mapLoad;

		public Settings(
			LogParser logParser,
			PipeParser pipeParser,
			MapLoad mapLoad,
			IAppDispatcher appDispatcher,
			ISignalrPlayerHub signalrPlayerHub,
			DPSLogParse dPSLogParse,
			EQSpells spells,
			EQToolSettings settings,
			EQToolSettingsLoad toolSettingsLoad,
			SettingsWindowViewModel settingsWindowData,
			BaseTimerWindowViewModel spellWindowViewModel,
			TimerWindowFactory timerFactory,
			TimerWindowService timerWindowService) : base(settings.SettingsWindowState, toolSettingsLoad, settings)
		{
			_signalrPlayerHub = signalrPlayerHub;
			_logParser = logParser;
			_pipeParser = pipeParser;
			_mapLoad = mapLoad;
			_appDispatcher = appDispatcher;
			_dPSLogParse = dPSLogParse;
			_spells = spells;
			_settings = settings;
			_spellWindowViewModel = spellWindowViewModel;
			_toolSettingsLoad = toolSettingsLoad;
			_timerWindowFactory = timerFactory;
			_timerWindowService = timerWindowService;
			DataContext = _viewModel = settingsWindowData;
			_viewModel.EqPath = this._settings.DefaultEqDirectory;
			InitializeComponent();
			base.Init();
			TryCheckLoggingEnabled();
			try
			{
				TryUpdateSettings();
			}
			catch
			{

			}
			this.DebugTab.Visibility = Visibility.Collapsed;
#if DEBUG
			this.DebugTab.Visibility = Visibility.Visible;
#endif

			MapInfoIcon.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Information.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
		}

		#region Private Methods
		private void SaveConfig()
		{
			_toolSettingsLoad.Save(_settings);
		}

		private void TryUpdateSettings()
		{
			var logfounddata = FindEq.GetLogFileLocation(new FindEq.FindEQData { EqBaseLocation = _settings.DefaultEqDirectory, EQlogLocation = string.Empty });
			if (logfounddata?.Found == true)
			{
				_settings.EqLogDirectory = logfounddata.Location;
				_viewModel.EqLogPath = logfounddata.Location;
			}
			_viewModel.Update();
			//BestGuessSpells.IsChecked = _settings.BestGuessSpells;
			//YouSpellsOnly.IsChecked = _settings.YouOnlySpells;
			var player = _viewModel.ActivePlayer.Player;

			if (player?.ShowSpellsForClasses != null)
			{
				foreach (var item in _viewModel.SelectedPlayerClasses)
				{
					item.IsChecked = player.ShowSpellsForClasses.Contains(item.TheValue);
				}
			}
			else
			{
				foreach (var item in _viewModel.SelectedPlayerClasses)
				{
					item.IsChecked = false;
				}
			}
			var hasvalideqdir = FindEq.IsValidEqFolder(_settings.DefaultEqDirectory);
			if (hasvalideqdir && FindEq.TryCheckLoggingEnabled(_settings.DefaultEqDirectory) == true)
			{
				((App)System.Windows.Application.Current).ToggleMenuButtons(true);
			}
		}
		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			_ = Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}
		private bool IsEqRunning()
		{
			return Process.GetProcessesByName("eqgame").Length > 0;
		}

		private void TryCheckLoggingEnabled()
		{
			_viewModel.IsLoggingEnabled = FindEq.TryCheckLoggingEnabled(_settings.DefaultEqDirectory) ?? false;
		}

		private void EqFolderButtonClicked(object sender, RoutedEventArgs e)
		{
			var descriptiontext = "Select Project 1999 EQ Directory";
#if QUARM
			descriptiontext = "Select Quarm EQ Directory";
#endif

			using (var fbd = new FolderBrowserDialog() { Description = descriptiontext, ShowNewFolderButton = false })
			{
				var result = fbd.ShowDialog();
				if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
				{
					if (FindEq.IsValidEqFolder(fbd.SelectedPath))
					{
						_viewModel.EqPath = _settings.DefaultEqDirectory = fbd.SelectedPath;
						this._appDispatcher.DispatchUI(() =>
						{
							TryUpdateSettings();
							TryCheckLoggingEnabled();
						});
					}
					else
					{
						_ = System.Windows.Forms.MessageBox.Show("eqgame.exe was not found in this folder. Make sure this is a valid Folder!", "Message");
					}
				}
			}
		}

		private void enableLogging_Click(object sender, RoutedEventArgs e)
		{
			if (IsEqRunning())
			{
				_ = System.Windows.MessageBox.Show("You must exit EQ before you can enable Logging!", "Configuration", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			try
			{
				if (_settings.DefaultEqDirectory.ToLower().Contains("program files"))
				{
					_ = System.Windows.MessageBox.Show("Everquest is installed in program files. YOU MUST ADD LOG=TRUE to the eqclient.ini yourself.", "Configuration", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
				else
				{
					var data = File.ReadAllLines(_settings.DefaultEqDirectory + "/eqclient.ini");
					var newlist = new List<string>();
					foreach (var item in data)
					{
						var line = item.ToLower().Trim().Replace(" ", string.Empty);
						if (line.StartsWith("log="))
						{
							newlist.Add("Log=TRUE");
						}
						else
						{
							newlist.Add(item);
						}
					}
					File.WriteAllLines(_settings.DefaultEqDirectory + "/eqclient.ini", newlist);
				}
			}
			catch { }
			TryUpdateSettings();
			TryCheckLoggingEnabled();
		}

		private void SaveSettings(object sender, RoutedEventArgs e)
		{
			if (sender == Zeal_HealthThreshEnabledCheck)
			{
				_pipeParser.SendStaticOverlayOff(Zeal_StaticOverlayType.Health);
			}
			else if(sender == Zeal_ManaThreshEnabledCheck)
			{
				_pipeParser.SendStaticOverlayOff(Zeal_StaticOverlayType.Mana);
			}
			else if(sender == Health_ShowTop_Check)
			{
				_viewModel.Health_ShowTop = Health_ShowTop_Check.IsChecked ?? false;
			}
			else if(sender == Health_ShowLeft_Check)
			{
				_viewModel.Health_ShowLeft = Health_ShowLeft_Check.IsChecked ?? false;
			}
			else if(sender == Health_ShowRight_Check)
			{
				_viewModel.Health_ShowRight = Health_ShowRight_Check.IsChecked ?? false;
			}
			else if(sender == Health_ShowBottom_Check)
			{
				_viewModel.Health_ShowBottom = Health_ShowBottom_Check.IsChecked ?? false;
			}
			else if(sender == Mana_ShowTop_Check)
			{
				_viewModel.Mana_ShowTop = Mana_ShowTop_Check.IsChecked ?? false;
			}
			else if(sender == Mana_ShowLeft_Check)
			{
				_viewModel.Mana_ShowLeft = Mana_ShowLeft_Check.IsChecked ?? false;
			}
			else if(sender == Mana_ShowRight_Check)
			{
				_viewModel.Mana_ShowRight = Mana_ShowRight_Check.IsChecked ?? false;
			}
			else if(sender == Mana_ShowBottom_Check)
			{
				_viewModel.Mana_ShowBottom = Mana_ShowBottom_Check.IsChecked ?? false;
			}
			else if(sender == Zeal_CastingEnabled_Check)
			{
				_viewModel.Zeal_CastingEnabled = Zeal_CastingEnabled_Check.IsChecked ?? false;
			}


			SaveConfig();
		}

		private void SaveSettings(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			SaveConfig();
		}

		private void TextShadow_Click(object sender, RoutedEventArgs e)
		{
			var s = sender as System.Windows.Controls.CheckBox;
			_settings.ShowTimerDropShadows = s.IsChecked ?? false;
			SaveConfig();
		}
		private void MapVisibleTracking_Click(object sender, RoutedEventArgs e)
		{
			var s = sender as System.Windows.Controls.CheckBox;
			_settings.TrackingVisibility = s.IsChecked ?? false;
			SaveConfig();
		}

		private void OverlayColor_Selected(object sender, RoutedEventArgs e)
		{
			var s = sender as ColorPicker;
			if (s.Name == "LevFadingOverlayColor")
			{
				_settings.LevFadingOverlayColor = s.SelectedColor.Value;
			}
			else if (s.Name == "InvisFadingOverlayColor")
			{
				_settings.InvisFadingOverlayColor = s.SelectedColor.Value;
			}
			else if (s.Name == "EnrageOverlayColor")
			{
				_settings.EnrageOverlayColor = s.SelectedColor.Value;
			}
			else if (s.Name == "FTEOverlayColor")
			{
				_settings.FTEOverlayColor = s.SelectedColor.Value;
			}
			else if (s.Name == "CharmBreakOverlayColor")
			{
				_settings.CharmBreakOverlayColor = s.SelectedColor.Value;
			}
			else if (s.Name == "FailedFeignOverlayColor")
			{
				_settings.FailedFeignOverlayColor = s.SelectedColor.Value;
			}
			else if (s.Name == "GroupInviteOverlayColor")
			{
				_settings.GroupInviteOverlayColor = s.SelectedColor.Value;
			}
			else if (s.Name == "DragonRoarOverlayColor")
			{
				_settings.DragonRoarOverlayColor = s.SelectedColor.Value;
			}
			else if (s.Name == "RootWarningOverlayColor")
			{
				_settings.RootWarningOverlayColor = s.SelectedColor.Value;
			}
			else if (s.Name == "ResistWarningOverlayColor")
			{
				_settings.ResistWarningOverlayColor = s.SelectedColor.Value;
			}
			else if (s.Name == "SpellTimerNameColor")
			{
				_settings.SpellTimerNameColor = s.SelectedColor.Value;
			}
			else if (s.Name == "BeneficialSpellTimerColor")
			{
				_settings.BeneficialSpellTimerColor = s.SelectedColor.Value;
			}
			else if (s.Name == "DetrimentalSpellTimerColor")
			{
				_settings.DetrimentalSpellTimerColor = s.SelectedColor.Value;
			}
			else if (s.Name == "RespawnTimerColor")
			{
				_settings.RespawnTimerColor = s.SelectedColor.Value;
			}
			else if (s.Name == "ModRodTimerColor")
			{
				_settings.ModRodTimerColor = s.SelectedColor.Value;
			}
			else if (s.Name == "DisciplineTimerColor")
			{
				_settings.DisciplineTimerColor = s.SelectedColor.Value;
			}
			else if (s.Name == "OtherTimerColor")
			{
				_settings.OtherTimerColor = s.SelectedColor.Value;
			}
			else if (s.Name == "Health_Color")
			{
				_settings.Health_Color = s.SelectedColor.Value;
				((App)System.Windows.Application.Current).UpdateStaticOverlayColors();
			}
			else if (s.Name == "Mana_Color")
			{
				_settings.Mana_Color = s.SelectedColor.Value;
				((App)System.Windows.Application.Current).UpdateStaticOverlayColors();
			}
			else if (s.Name == "Health_Mana_Color")
			{
				_settings.Health_Mana_Color = s.SelectedColor.Value;
				((App)System.Windows.Application.Current).UpdateStaticOverlayColors();
			}

			SaveConfig();
		}

		private void testspellsclicked(object sender, RoutedEventArgs e)
		{
			var listofspells = new List<SpellParsingMatch>
			{
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Disease Cloud"), TargetName = "Joe", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Lesser Shielding"), TargetName = "Joe", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Shadow Compact"), TargetName = "Joe", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Heroic Bond"), TargetName = "Joe", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Improved Invis to Undead"), TargetName = "Joe", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Grim Aura"), TargetName = "Joe", MultipleMatchesFound = false },

				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Heroic Bond"), TargetName = EQSpells.SpaceYou, MultipleMatchesFound = false },

				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Heroic Bond"), TargetName = "Aasgard", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Chloroplast"), TargetName = "Aasgard", MultipleMatchesFound = true },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Shield of Words"), TargetName = "Aasgard", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Boon of the Clear Mind"), TargetName = "Aasgard", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Gift of Brilliance"), TargetName = "Aasgard", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Mana Sieve"), TargetName = "a bad guy", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Mana Sieve"), TargetName = "a bad guy", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Harvest"), TargetName = EQSpells.SpaceYou, MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "LowerElement"), TargetName = "Tunare", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "LowerElement"), TargetName = "Tunare", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "LowerElement"), TargetName = "Tunare", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "LowerElement"), TargetName = "Tunare", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "LowerElement"), TargetName = "Tunare", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "LowerElement"), TargetName = "Tunare", MultipleMatchesFound = false },

				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Concussion"), TargetName = "Tunare", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Concussion"), TargetName = "Tunare", MultipleMatchesFound = false },

				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Flame Lick"), TargetName = "Tunare", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Flame Lick"), TargetName = "Tunare", MultipleMatchesFound = false },

				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Jolt"), TargetName = "Tunare", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Jolt"), TargetName = "Tunare", MultipleMatchesFound = false },

				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Cinder Jolt"), TargetName = "Tunare", MultipleMatchesFound = false },
				new SpellParsingMatch { Spell = _spells.AllSpells.FirstOrDefault(a => a.name == "Cinder Jolt"), TargetName = "Tunare", MultipleMatchesFound = false }
			};

			foreach (var item in listofspells)
			{
				_spellWindowViewModel.TryAdd(item, false);
			}
			_spellWindowViewModel.TryAddCustom(new CustomTimer { DurationInSeconds = 45, Name = "--DT-- Luetin", SpellType = SpellTypes.BadGuyCoolDown, SpellNameIcon = "Disease Cloud" });
			_spellWindowViewModel.TryAddCustom(new CustomTimer { DurationInSeconds = 60 * 27, Name = "King" });
			_spellWindowViewModel.TryAddCustom(new CustomTimer { DurationInSeconds = 60 * 18, Name = "hall Wanderer 1" });
		}

		private void CheckBoxZone_Checked(object sender, RoutedEventArgs e)
		{
			var chkZone = (System.Windows.Controls.CheckBox)sender;
			var player = _viewModel.ActivePlayer.Player;
			if (player != null)
			{
				var item = (PlayerClasses)chkZone.Tag;
				if (chkZone.IsChecked == true && !player.ShowSpellsForClasses.Any(a => a == item))
				{
					player.ShowSpellsForClasses.Add(item);
					SaveConfig();
				}
				else if (chkZone.IsChecked == false && player.ShowSpellsForClasses.Any(a => a == item))
				{
					player.ShowSpellsForClasses.Remove(item);
					SaveConfig();
				}
			}
		}

		private void zoneselectionchanged(object sender, SelectionChangedEventArgs e)
		{
			var player = _viewModel.ActivePlayer.Player;
			if (player != null)
			{
				var t = DateTime.Now;
				var format = "ddd MMM dd HH:mm:ss yyyy";
				var msg = "[" + t.ToString(format) + "] You have entered " + player.Zone;
				_logParser.Push(msg);
				SaveConfig();
			}
		}

		private void testRandomRolls(object sender, RoutedEventArgs e)
		{
			var testbutton = sender as System.Windows.Controls.Button;
			if (!testbutton.IsEnabled)
			{
				return;
			}
			testbutton.IsEnabled = false;
			_ = Task.Factory.StartNew(() =>
			{
				try
				{
					var random = new Random();
					var r = random.Next(0, 333);
					PushLog("**A Magic Die is rolled by Whitewitch.");
					PushLog($"**It could have been any number from 0 to 333, but this time it turned up a {r}.");
					r = random.Next(0, 333);
					PushLog("**A Magic Die is rolled by Huntor.");
					PushLog($"**It could have been any number from 0 to 333, but this time it turned up a {r}.");
					r = random.Next(0, 333);
					PushLog("**A Magic Die is rolled by Vasanle.");
					PushLog($"**It could have been any number from 0 to 333, but this time it turned up a {r}.");
					r = random.Next(0, 333);
					PushLog("**A Magic Die is rolled by Sanare.");
					PushLog($"**It could have been any number from 0 to 333, but this time it turned up a {r}.");

					_appDispatcher.DispatchUI(() => { testbutton.IsEnabled = true; });
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.ToString());
					_appDispatcher.DispatchUI(() => { testbutton.IsEnabled = true; });
				}
			});
		}

		private void testDPS(object sender, RoutedEventArgs e)
		{
			var testdpsbutton = sender as System.Windows.Controls.Button;
			if (!testdpsbutton.IsEnabled)
			{
				return;
			}
			testdpsbutton.IsEnabled = false;
			_ = Task.Factory.StartNew(() =>
			{
				try
				{

					var fightlines = Properties.Resources.TestFight2.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
					var fightlist = new List<KeyValuePair<string, DPSParseMatch>>();
					foreach (var item in fightlines)
					{
						if (item == null || item.Length < 27)
						{
							continue;
						}

						var date = item.Substring(1, 24);
						var message = item.Substring(27).Trim();
						var format = "ddd MMM dd HH:mm:ss yyyy";
						var timestamp = DateTime.Now;
						try
						{
							timestamp = DateTime.ParseExact(date, format, CultureInfo.InvariantCulture);
						}
						catch (FormatException)
						{
						}
						var match = _dPSLogParse.Match(message, timestamp);
						if (match != null)
						{
							fightlist.Add(new KeyValuePair<string, DPSParseMatch>(item, match));
						}
					}

					var starttime = fightlist.FirstOrDefault().Value.TimeStamp;
					var starttimediff = DateTime.Now - starttime;
					var index = 0;
					do
					{
						for (; index < fightlist.Count; index++)
						{
							var t = fightlist[index].Value.TimeStamp + starttimediff;
							if (t > DateTime.Now)
							{
								break;
							}
							else
							{
								var line = fightlist[index].Key;
								var indexline = line.IndexOf("]");
								var msgwithout = line.Substring(indexline);
								var format = "ddd MMM dd HH:mm:ss yyyy";
								msgwithout = "[" + t.ToString(format) + msgwithout;
								_logParser.Push(msgwithout);
							}
						}
						Thread.Sleep(100);
					} while (index < fightlist.Count);
					_appDispatcher.DispatchUI(() => { testdpsbutton.IsEnabled = true; });
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.ToString());
					_appDispatcher.DispatchUI(() => { testdpsbutton.IsEnabled = true; });
				}
			});
		}

		private void logpush(object sender, RoutedEventArgs e)
		{
			var logtext = LogPushText.Text?.Trim();
			this.PushLog(logtext);
		}

		private void PushLog(string message)
		{
			var logtext = message?.Trim();
			if (string.IsNullOrWhiteSpace(logtext))
			{
				return;
			}
			if (!logtext.StartsWith("["))
			{
				var format = "ddd MMM dd HH:mm:ss yyyy";
				var d = DateTime.Now;
				logtext = "[" + d.ToString(format) + "] " + logtext;
			}
			_logParser.Push(logtext);
		}

		private void selectLogFile(object sender, RoutedEventArgs e)
		{
			var button = sender as System.Windows.Controls.Button;
			if (!button.IsEnabled)
			{
				return;
			}
			var dialog = new Microsoft.Win32.OpenFileDialog();
			bool? result = dialog.ShowDialog();
			if (result == true)
			{

				var filename = dialog.FileName;
				button.IsEnabled = false;
				_ = Task.Factory.StartNew(() =>
				{
					var fightlines = File.ReadAllLines(filename);
					var lines = new List<KeyValuePair<DateTime, string>>();
					foreach (var item in fightlines)
					{
						if (item == null || item.Length < 27)
						{
							continue;
						}

						var date = item.Substring(1, 24);
						var message = item.Substring(27).Trim();
						var format = "ddd MMM dd HH:mm:ss yyyy";
						var timestamp = DateTime.Now;
						try
						{
							timestamp = DateTime.ParseExact(date, format, CultureInfo.InvariantCulture);
							lines.Add(new KeyValuePair<DateTime, string>(timestamp, item));
						}
						catch (FormatException)
						{
						}
					}
					var starttime = lines.FirstOrDefault().Key;
					try
					{

						var starttimediff = DateTime.Now - starttime;
						var index = 0;
						do
						{
							for (; index < lines.Count; index++)
							{
								var t = lines[index].Key + starttimediff;
								if (t > DateTime.Now)
								{
									break;
								}
								else
								{
									var line = lines[index].Value;
									var indexline = line.IndexOf("]");
									var msgwithout = line.Substring(indexline);
									var format = "ddd MMM dd HH:mm:ss yyyy";
									msgwithout = "[" + t.ToString(format) + msgwithout;
									_logParser.Push(msgwithout);
								}
							}
							Thread.Sleep(100);
						} while (index < lines.Count);

						_appDispatcher.DispatchUI(() => { button.IsEnabled = true; });
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex.ToString());
						_appDispatcher.DispatchUI(() => { button.IsEnabled = true; });
					}
				});
			}
		}

		private void textmapclicked(object sender, RoutedEventArgs e)
		{
			var testmap = sender as System.Windows.Controls.Button;
			if (!testmap.IsEnabled)
			{
				return;
			}
			testmap.IsEnabled = false;
			_ = Task.Factory.StartNew(() =>
			{
				var fightlines = Properties.Resources.testmap.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
				var lines = new List<KeyValuePair<DateTime, string>>();
				foreach (var item in fightlines)
				{
					if (item == null || item.Length < 27)
					{
						continue;
					}

					var date = item.Substring(1, 24);
					var message = item.Substring(27).Trim();
					var format = "ddd MMM dd HH:mm:ss yyyy";
					var timestamp = DateTime.Now;
					try
					{
						timestamp = DateTime.ParseExact(date, format, CultureInfo.InvariantCulture);
						lines.Add(new KeyValuePair<DateTime, string>(timestamp, item));
					}
					catch (FormatException)
					{
					}
				}
				var starttime = lines.FirstOrDefault().Key;
				try
				{

					var starttimediff = DateTime.Now - starttime;
					var index = 0;
					do
					{
						for (; index < lines.Count; index++)
						{
							var t = lines[index].Key + starttimediff;
							if (t > DateTime.Now)
							{
								break;
							}
							else
							{
								var line = lines[index].Value;
								var indexline = line.IndexOf("]");
								var msgwithout = line.Substring(indexline);
								var format = "ddd MMM dd HH:mm:ss yyyy";
								msgwithout = "[" + t.ToString(format) + msgwithout;
								_logParser.Push(msgwithout);
							}
						}
						Thread.Sleep(100);
					} while (index < lines.Count);

					_appDispatcher.DispatchUI(() => { testmap.IsEnabled = true; });
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.ToString());
					_appDispatcher.DispatchUI(() => { testmap.IsEnabled = true; });
				}
			});
		}

		private void testsignalrlocations(object sender, RoutedEventArgs e)
		{
			var testmap = sender as System.Windows.Controls.Button;
			if (!testmap.IsEnabled)
			{
				return;
			}

			var player = _viewModel.ActivePlayer?.Player;
			if (player == null)
			{
				return;
			}

			var map = _mapLoad.Load(player.Zone);
			if (map == null)
			{
				return;
			}

			testmap.IsEnabled = false;
			_ = Task.Factory.StartNew(() =>
			{
				var names = new List<string>() { "faiil", "irishfaf", "chuunt", "jakab", "nima", "healmin" };
				var pnames = new List<EQToolShared.Map.SignalrPlayer>();

				var movementoffset = (int)(map.AABB.MaxWidth / 10);
				var r = new Random();
				var offset = r.Next(-movementoffset, movementoffset);
				foreach (var item in names)
				{
					offset = r.Next(-movementoffset, movementoffset);
					var p = new EQToolShared.Map.SignalrPlayer
					{
						GuildName = "The Drift",
						MapLocationSharing = EQToolShared.Map.MapLocationSharing.Everyone,
						Name = item,
						PlayerClass = PlayerClasses.Necromancer,
						Server = Servers.Green,
						Zone = player.Zone,
						X = map.AABB.Center.X + map.Offset.X + r.Next(-movementoffset, movementoffset),
						Y = map.AABB.Center.Y + map.Offset.Y + r.Next(-movementoffset, movementoffset),
						Z = map.AABB.Center.Z + map.Offset.Z + r.Next(-movementoffset, movementoffset)
					};

					pnames.Add(p);
					_signalrPlayerHub.PushPlayerLocationEvent(p);
				}
				movementoffset = (int)(map.AABB.MaxWidth / 50);
				try
				{
					var starttime = DateTime.UtcNow;
					while ((starttime - DateTime.UtcNow).TotalMinutes < 3)
					{
						foreach (var item in pnames)
						{
							var p = new EQToolShared.Map.SignalrPlayer
							{
								GuildName = "The Drift",
								MapLocationSharing = EQToolShared.Map.MapLocationSharing.Everyone,
								Name = item.Name,
								PlayerClass = PlayerClasses.Necromancer,
								Server = Servers.Green,
								Zone = player.Zone,
								X = item.X + r.Next(-movementoffset, movementoffset),
								Y = item.Y + r.Next(-movementoffset, movementoffset),
								Z = item.Z + r.Next(-movementoffset, movementoffset)
							};

							_signalrPlayerHub.PushPlayerLocationEvent(p);
						}
						Thread.Sleep(1000);
					}

					_appDispatcher.DispatchUI(() => { testmap.IsEnabled = true; });
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.ToString());
					_appDispatcher.DispatchUI(() => { testmap.IsEnabled = true; });
				}
			});
		}
		private void SaveAlwaysOntopCheckBoxSettings(object sender, RoutedEventArgs e)
		{
			SaveConfig();
			((App)System.Windows.Application.Current).ApplyAlwaysOnTop();
		}
		private void testenrage(object sender, RoutedEventArgs e)
		{
			var z = _viewModel.ActivePlayer?.Player?.Zone;
			if (string.IsNullOrWhiteSpace(z))
			{
				return;
			}
			if (_viewModel.ActivePlayer?.Player == null)
			{
				return;
			}
			_viewModel.ActivePlayer.Player.EnrageAudio = true;
			_viewModel.ActivePlayer.Player.EnrageOverlay = true;
			((App)System.Windows.Application.Current).OpenOverLayWindow();
			this.PushLog("Lord Nagafen has become ENRAGED.");
		}

		private void testlevfading(object sender, RoutedEventArgs e)
		{
			if (_viewModel.ActivePlayer?.Player == null)
			{
				return;
			}
			_viewModel.ActivePlayer.Player.LevFadingAudio = true;
			_viewModel.ActivePlayer.Player.LevFadingOverlay = true;
			((App)System.Windows.Application.Current).OpenOverLayWindow();
			this.PushLog("You feel as if you are about to fall.");
		}
		private void testinvisfading(object sender, RoutedEventArgs e)
		{
			if (_viewModel.ActivePlayer?.Player == null)
			{
				return;
			}
			_viewModel.ActivePlayer.Player.InvisFadingAudio = true;
			_viewModel.ActivePlayer.Player.InvisFadingOverlay = true;
			((App)System.Windows.Application.Current).OpenOverLayWindow();
			this.PushLog("You feel yourself starting to appear.");
		}

		private void testCharmBreak(object sender, RoutedEventArgs e)
		{
			if (_viewModel.ActivePlayer?.Player == null)
			{
				return;
			}
			_viewModel.ActivePlayer.Player.CharmBreakAudio = true;
			_viewModel.ActivePlayer.Player.CharmBreakOverlay = true;
			((App)System.Windows.Application.Current).OpenOverLayWindow();
			this.PushLog("Your charm spell has worn off.");
		}

		private void testFailedFeign(object sender, RoutedEventArgs e)
		{
			if (_viewModel.ActivePlayer?.Player == null)
			{
				return;
			}
			_viewModel.ActivePlayer.Player.FailedFeignAudio = true;
			_viewModel.ActivePlayer.Player.FailedFeignOverlay = true;
			((App)System.Windows.Application.Current).OpenOverLayWindow();
			this.PushLog($"{_viewModel.ActivePlayer?.Player?.Name} has fallen to the ground.");
		}
		private void testFTE(object sender, RoutedEventArgs e)
		{
			if (_viewModel.ActivePlayer?.Player == null)
			{
				return;
			}
			_viewModel.ActivePlayer.Player.FTEOverlay = true;
			_viewModel.ActivePlayer.Player.FTEAudio = true;
			((App)System.Windows.Application.Current).OpenOverLayWindow();
			this.PushLog("Dagarn the Destroyer engages Tzvia!");
		}
		private void testGroupInvite(object sender, RoutedEventArgs e)
		{
			if (_viewModel.ActivePlayer?.Player == null)
			{
				return;
			}
			_viewModel.ActivePlayer.Player.GroupInviteOverlay = true;
			_viewModel.ActivePlayer.Player.GroupInviteAudio = true;
			((App)System.Windows.Application.Current).OpenOverLayWindow();
			this.PushLog($"Tzvia invites you to join a group.");
		}
		private void testDragonRoar(object sender, RoutedEventArgs e)
		{
			if (_viewModel.ActivePlayer?.Player == null)
			{
				return;
			}
			_viewModel.ActivePlayer.Player.DragonRoarAudio = true;
			_viewModel.ActivePlayer.Player.DragonRoarOverlay = true;
			((App)System.Windows.Application.Current).OpenOverLayWindow();
			this.PushLog($"You resist the Dragon Roar spell!");
		}
		private void textvoice(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(this._viewModel.SelectedVoice))
			{
				return;
			}

			System.Threading.Tasks.Task.Factory.StartNew(() =>
			{
				var synth = new SpeechSynthesizer();
				synth.SelectVoice(this._viewModel.SelectedVoice);
				synth.Speak($"You resist the Dragon Roar spell!");
			});
		}

		private void testChChain(object sender, RoutedEventArgs e)
		{
			var button = sender as System.Windows.Controls.Button;
			if (!button.IsEnabled)
			{
				return;
			}
			if (_viewModel.ActivePlayer?.Player == null)
			{
				return;
			}
			_viewModel.ActivePlayer.Player.ChChainOverlay = true;
			_viewModel.ActivePlayer.Player.ChChainWarningOverlay = true;
			_viewModel.ActivePlayer.Player.ChChainWarningAudio = true;
			((App)System.Windows.Application.Current).OpenOverLayWindow();
			button.IsEnabled = false;
			_ = Task.Factory.StartNew(() =>
			{
				try
				{
					var tag = _viewModel.ActivePlayer.Player.ChChainTagOverlay;
					var msg = $"You shout, '{tag} 001 CH -- Beefwich'";
					PushLog(msg);
					Thread.Sleep(2000);

					msg = $"Mycro shouts, '{tag}  002 CH -- Huntor'";
					PushLog(msg);
					Thread.Sleep(1000);

					msg = $"Sleeper shouts, '{tag}  003 CH -- Beefwich'";
					PushLog(msg);
					Thread.Sleep(2000);

					msg = $"Sleeper shouts, '{tag}  004 CH -- Beefwich'";
					PushLog(msg);
					Thread.Sleep(1800);

					msg = $"You shout, '{tag}  001 CH -- Beefwich'";
					PushLog(msg);
					Thread.Sleep(2000);

					msg = $"Mycro shouts, '{tag}  002 CH -- Huntor'";
					PushLog(msg);
					Thread.Sleep(1500);

					msg = $"Sleeper shouts, '{tag}  003 CH -- Beefwich'";
					PushLog(msg);
					Thread.Sleep(2000);

					msg = $"Sleeper shouts, '{tag}  004 CH -- Beefwich'";
					PushLog(msg);
					Thread.Sleep(1700);

					msg = $"Hanbox shouts, '{tag}  002 CH -- Huntor'";
					PushLog(msg);
					Thread.Sleep(1500);

					msg = $"Hanbox shouts, '{tag}  001 CH -- Beefwich'";
					PushLog(msg);
					Thread.Sleep(1700);

					msg = $"Sleeper shouts, '{tag}  003 CH -- Beefwich'";
					PushLog(msg);
					Thread.Sleep(2000);

					msg = $"Sleeper shouts, '{tag}  004 CH -- Beefwich'";
					PushLog(msg);
					Thread.Sleep(1800);

					msg = $"Mycro shouts, '{tag}  002 CH -- Huntor'";
					PushLog(msg);
					Thread.Sleep(1500);
					_appDispatcher.DispatchUI(() => { button.IsEnabled = true; });
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.ToString());
					_appDispatcher.DispatchUI(() => { button.IsEnabled = true; });
				}
			});
		}

		private void testRootBreak(object sender, RoutedEventArgs e)
		{
			var button = sender as System.Windows.Controls.Button;
			if (!button.IsEnabled)
			{
				return;
			}
			if (_viewModel.ActivePlayer?.Player == null)
			{
				return;
			}
			_viewModel.ActivePlayer.Player.RootWarningAudio = true;
			_viewModel.ActivePlayer.Player.RootWarningOverlay = true;
			((App)System.Windows.Application.Current).OpenOverLayWindow();
			button.IsEnabled = false;
			_ = Task.Factory.StartNew(() =>
			{
				try
				{

					PushLog("Your Paralyzing Earth spell has worn off.");
					_appDispatcher.DispatchUI(() => { button.IsEnabled = true; });
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.ToString());
					_appDispatcher.DispatchUI(() => { button.IsEnabled = true; });
				}
			});
		}

		private void testResists(object sender, RoutedEventArgs e)
		{
			var button = sender as System.Windows.Controls.Button;
			if (!button.IsEnabled)
			{
				return;
			}
			if (_viewModel.ActivePlayer?.Player == null)
			{
				return;
			}
			_viewModel.ActivePlayer.Player.ResistWarningAudio = true;
			_viewModel.ActivePlayer.Player.ResistWarningOverlay = true;
			((App)System.Windows.Application.Current).OpenOverLayWindow();
			button.IsEnabled = false;
			_ = Task.Factory.StartNew(() =>
			{
				try
				{

					PushLog("Your target resisted the Rest the Dead spell.");
					_appDispatcher.DispatchUI(() => { button.IsEnabled = true; });
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.ToString());
					_appDispatcher.DispatchUI(() => { button.IsEnabled = true; });
				}
			});
		}

		private void NewOverlay_Save(object sender, RoutedEventArgs e)
		{
			CustomOverlay newOverlay = new CustomOverlay
			{
				Name = NewOverlay_Name.Text,
				Message = NewOverlay_Message.Text,
				Color = NewOverlay_TriggerColor.SelectedColor.Value,
				Trigger = NewOverlay_Trigger.Text,
				Alternate_Trigger = NewOverlay_AltTrigger.Text,
				AudioMessage = NewOverlay_AudioMessage.Text,
				IsEnabled = true,
				IsAudioEnabled = true
			};

			if (CustomOverlayService.AddNewCustomOverlay(newOverlay))
			{
				if (_settings.CustomOverlays == null)
				{
					_settings.CustomOverlays = new ObservableCollectionRange<CustomOverlay>();
				}
				List<CustomOverlay> overlays = CustomOverlayService.LoadCustomOverlayMessages();
				foreach (var overlay in overlays)
				{
					if (!_settings.CustomOverlays.Any(co => co.ID == overlay.ID))
					{
						_settings.CustomOverlays.Add(overlay);
					}
				}
			}

			//unset
			NewOverlay_Name.Text = "";
			NewOverlay_Message.Text = "";
			NewOverlay_Trigger.Text = "";
			NewOverlay_AltTrigger.Text = "";
			NewOverlay_TriggerColor.SelectedColor = Colors.White;

			//close popup
			ToggleCreateNewButton.IsChecked = false;
		}

		private void CustomOverlayVisual_Click(object sender, RoutedEventArgs e)
		{
			var s = sender as System.Windows.Controls.CheckBox;
			CustomOverlayService.UpdateCustomOverlay((CustomOverlay)s.DataContext);
			SaveConfig();
		}

		private void CustomOverlayAudio_Click(object sender, RoutedEventArgs e)
		{
			var s = sender as System.Windows.Controls.CheckBox;
			CustomOverlayService.UpdateCustomOverlay((CustomOverlay)s.DataContext);
			SaveConfig();
		}
		#endregion

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
		}

		private void Open_EditExistingCustomOverlay(object sender, RoutedEventArgs ev)
		{
			var temp = (sender as System.Windows.Controls.Button).DataContext as int?;
			if (temp != null && temp.HasValue)
			{
				CustomOverlayEditWindow editWindow = new CustomOverlayEditWindow(temp.Value);
				editWindow.CustomOverlayEdited += (s, e) =>
				{
					if (e.Success)
					{
						var found = _settings.CustomOverlays.FirstOrDefault(a => a.ID == e.UpdatedOverlay.ID);
						int i = _settings.CustomOverlays.IndexOf(found);
						_settings.CustomOverlays[i] = e.UpdatedOverlay;
					}
				};

				editWindow.ShowDialog();
			}
		}

		private void Delete_ExistingCustomOverlay(object sender, RoutedEventArgs e)
		{
			var temp = (sender as System.Windows.Controls.Button).DataContext as int?;
			var overlayToDelete = _settings.CustomOverlays.FirstOrDefault(a => a.ID == temp.Value);

			var messageBoxText = $"Are you sure that you want to delete your {overlayToDelete.Name} trigger?";
			var caption = "Are you sure?";
			var button = (MessageBoxButton)Enum.Parse(typeof(MessageBoxButton), "YesNo");
			var icon = (MessageBoxImage)Enum.Parse(typeof(MessageBoxImage), "Warning");
			var defaultResult =
				(MessageBoxResult)Enum.Parse(typeof(MessageBoxResult), "None");
			var options = (System.Windows.MessageBoxOptions)Enum.Parse(typeof(System.Windows.MessageBoxOptions), "None");

			// Show message box, passing the window owner if specified
			MessageBoxResult result = System.Windows.MessageBox.Show(this, messageBoxText, caption, button, icon, defaultResult, options);

			// Show the result
			if (result == MessageBoxResult.Yes)
			{
				if (temp != null && temp.HasValue)
				{
					if (CustomOverlayService.DeleteCustomOverlay(overlayToDelete))
					{
						_settings.CustomOverlays.Remove(overlayToDelete);
					}
				}
			}
		}

		private void ClearCachedMapsClicked(object sender, RoutedEventArgs e)
		{
			_mapLoad.ClearCachedMaps();
			var player = _viewModel.ActivePlayer.Player;
			if (player != null)
			{
				var t = DateTime.Now;
				var format = "ddd MMM dd HH:mm:ss yyyy";
				var msg = "[" + t.ToString(format) + "] You have entered " + player.Zone + " FORCECLEAR";
				_logParser.Push(msg);
				SaveConfig();
			}
		}

		private void CreateTimerWindow_Click(object sender, RoutedEventArgs e)
		{
			var temp = (sender as System.Windows.Controls.Button).DataContext as int?;
			var window = new EditTimerWindow(_settings, _timerWindowFactory, temp);

			window.TimerWindowEdited += (s, ev) =>
			{
				if (ev.Success)
				{
					var found = _settings.TimerWindows.FirstOrDefault(a => a.ID == ev.UpdatedWindow.ID);
					int i = _settings.TimerWindows.IndexOf(found);
					_settings.TimerWindows[i] = ev.UpdatedWindow;
				}
			};

			window.Show();
		}

		private void TimerWindow_ResetPositionClick(object sender, RoutedEventArgs e)
		{
			var temp = (sender as System.Windows.Controls.Button).DataContext as int?;
			var windowOptions = TimerWindowService.LoadTimerWindow(temp.Value);
			var window = (App.Current as App).GetSpawnableTimerWindowBase(windowOptions);

			window.Left = 1;
			window.Top = 1;

			windowOptions.WindowRect = $"1,1,{window.Width},{window.Height}";

			TimerWindowService.UpdateTimerWindow(windowOptions);

			LastWindowInteraction = DateTime.UtcNow;
		}

		private void TimerWindowAlwaysOnTop_Click(object sender, RoutedEventArgs e)
		{
			var temp = (sender as System.Windows.Controls.CheckBox).DataContext as TimerWindowOptions;
			var timer = _settings.TimerWindows.FirstOrDefault(a => a.ID == temp.ID);

			if (timer != null)
			{
				var s = sender as System.Windows.Controls.CheckBox;
				temp.AlwaysOnTop = s.IsChecked.Value;
				TimerWindowService.UpdateTimerWindow(temp);
				(System.Windows.Application.Current as App).UpdateSpawnableTimerWindowContext(timer);

				OnPropertyChanged(new DependencyPropertyChangedEventArgs(TopmostProperty, !s.IsChecked.Value, s.IsChecked.Value));
			}
		}

		private void DeleteTimerWindow_Click(object sender, RoutedEventArgs e)
		{
			var temp = (sender as System.Windows.Controls.Button).DataContext as int?;
			var timerToDelete = _settings.TimerWindows.FirstOrDefault(a => a.ID == temp.Value);

			var messageBoxText = $"Are you sure that you want to delete your {timerToDelete.Title} window?";
			var caption = "Are you sure?";
			var button = (MessageBoxButton)Enum.Parse(typeof(MessageBoxButton), "YesNo");
			var icon = (MessageBoxImage)Enum.Parse(typeof(MessageBoxImage), "Warning");
			var defaultResult =
				(MessageBoxResult)Enum.Parse(typeof(MessageBoxResult), "None");
			var options = (System.Windows.MessageBoxOptions)Enum.Parse(typeof(System.Windows.MessageBoxOptions), "None");

			// Show message box, passing the window owner if specified
			MessageBoxResult result = System.Windows.MessageBox.Show(this, messageBoxText, caption, button, icon, defaultResult, options);

			// Show the result
			if (result == MessageBoxResult.Yes)
			{
				(App.Current as App).GetSpawnableTimerWindowBase(timerToDelete)?.Close();

				if (temp != null && temp.HasValue)
				{
					if (TimerWindowService.DeleteTimerWindow(timerToDelete))
					{
						_settings.TimerWindows.Remove(timerToDelete);
					}
				}
			}
		}

		private void HealthThreshold_Value_TextChanged(object sender, TextChangedEventArgs e)
		{
			if(decimal.TryParse(HealthThreshold_Value.Text, out decimal result))
			{
				_settings.Zeal_HealthThreshold = result;
			}
        }

		private void ManaThreshold_Value_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (decimal.TryParse(ManaThreshold_Value.Text, out decimal result))
			{
				_settings.Zeal_ManaThreshold = result;
			}
		}

		private void ImageOverlay_ToggleLock(object sender, RoutedEventArgs e)
		{
			_settings.Lock_ImageOverlay_Position = !_settings.Lock_ImageOverlay_Position;

		}

		private void Threshold_Value_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
        }

		private void NumberValue_FieldChanged(object sender, TextChangedEventArgs e)
		{
			if(sender == RespawnMultiplier_Value && double.TryParse(RespawnMultiplier_Value.Text, out double spawnMulti))
			{
				_settings.SpawnRateMultiplier = spawnMulti;
			}
			else if (sender == BeneSpellDurationMultiplier_Value && double.TryParse(BeneSpellDurationMultiplier_Value.Text, out double durationBeneMulti))
			{
				_settings.BeneficialSpellDurationMultiplier = durationBeneMulti;
			}
			else if (sender == DetrSpellDurationMultiplier_Value && double.TryParse(DetrSpellDurationMultiplier_Value.Text, out double durationDetrMulti))
			{
				_settings.DetrimentalSpellDurationMultiplier = durationDetrMulti;
			}
			if (sender == StaticOverlay_SizeTop && decimal.TryParse(StaticOverlay_SizeTop.Text, out decimal resultTop))
			{
				_settings.StaticOverlay_SizeTop = resultTop;
			}
			else if (sender == StaticOverlay_SizeLeft && decimal.TryParse(StaticOverlay_SizeLeft.Text, out decimal resultLeft))
			{
				_settings.StaticOverlay_SizeLeft = resultLeft;
			}
			else if (sender == StaticOverlay_SizeRight && decimal.TryParse(StaticOverlay_SizeRight.Text, out decimal resultRight))
			{
				_settings.StaticOverlay_SizeRight = resultRight;
			}
			else if (sender == StaticOverlay_SizeBottom && decimal.TryParse(StaticOverlay_SizeBottom.Text, out decimal resultBottom))
			{
				_settings.StaticOverlay_SizeBottom = resultBottom;
			}
		}

		private void DPS_KeepThreshold_ValueChanged(object sender, TextChangedEventArgs e)
		{
			if (int.TryParse(DpsRemovalTimerThreshold_Field.Text, out int result))
			{
				_settings.DpsRemovalTimerThreshold = result;
			}
		}

		private void ImageOverlay_Maximize(object sender, RoutedEventArgs e)
		{
			(System.Windows.Application.Current as App).ToggleImageOverlayWindowSize();
		}
	}
}