﻿using EQTool.EventArgModels;
using EQTool.Models;
using EQTool.Services.Map;
using EQTool.Services.Parsing;
using EQTool.Services.Spells.Log;
using EQTool.Utilities;
using EQTool.ViewModels;
using EQToolShared.HubModels;
using EQToolShared.Map;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media.Media3D;
using ZealPipes.Services;
using static ZealPipes.Services.ZealMessageService;
using EQToolShared.Enums;
using static Slapper.AutoMapper;
using System.Text.RegularExpressions;
using EQToolShared.ExtendedClasses;
using static EQTool.Services.LogParser;
using static EQTool.Services.ChParser;
using static EQTool.Services.DTParser;
using static EQTool.Services.EnrageParser;
using static EQTool.Services.FTEParser;
using static EQTool.Services.InvisParser;
using static EQTool.Services.LevParser;
using static EQTool.Services.ResistSpellParser;
using System.Windows.Shapes;

namespace EQTool.Services
{
	public class PipeParser : IDisposable
	{
		private readonly EQSpells _spells;

		private System.Timers.Timer _uiTimer;
		private readonly ActivePlayer _activePlayer;
		private readonly IAppDispatcher _appDispatcher;
		private string _lastLogFilename = string.Empty;
		private readonly EQToolSettings _settings;
		private readonly QuarmDataService _quarmService;
		private readonly LevelLogParse _levelLogParse;
		private readonly EQToolSettingsLoad _toolSettingsLoad;
		private readonly ISignalrPlayerHub _signalrPlayerHub;
		private readonly LoggingService _logging;

		private readonly SpellLogParse _spellParser;
		//private readonly SpellWornOffLogParse _spellWornOffLogParse;
		private readonly DTParser _dtParser;
		private readonly EnrageParser _enrageParser;
		private readonly InvisParser _invisParser;
		private readonly LevParser _levParser;
		private readonly FailedFeignParser _failedFeignParser;
		private readonly GroupInviteParser _groupInviteParser;
		private readonly ResistSpellParser _resistSpellParser;
		private readonly CustomOverlayParser _customOverlayParser;

		private bool StartingWhoOfZone = false;
		private bool Processing = false;
		private bool StillCamping = false;
		private bool HasUsedStartupEnterWorld = false;
		private bool _IsAutoAttacking = false;

		private string _petName = string.Empty;

		public bool JustZoned = false;


		ZealMessageService _zealMessageService;


		public PipeParser(
			ResistSpellParser resistSpellParser,
			GroupInviteParser groupInviteParser,
			FailedFeignParser failedFeignParser,
			LevParser levParser,
			InvisParser invisParser,
			EnrageParser enrageParser,
			DTParser dtParser,
			SpellLogParse spellParser,
			CustomOverlayParser customOverlayParser,
			//SpellWornOffLogParse spellWornOffLogParse,

			EQToolSettingsLoad toolSettingsLoad,
			ActivePlayer activePlayer,
			IAppDispatcher appDispatcher,
			EQToolSettings settings,
			QuarmDataService quarmService,
			EQSpells spells,
			ZealMessageService zealMessageService,
			ISignalrPlayerHub signalrPlayerHub,
			LoggingService logging
			)
		{
			_toolSettingsLoad = toolSettingsLoad;
			_activePlayer = activePlayer;
			_appDispatcher = appDispatcher;
			_levelLogParse = new LevelLogParse(activePlayer);
			_settings = settings;
			_quarmService = quarmService;
		
			_spells = spells;
			_zealMessageService = zealMessageService;
			_signalrPlayerHub = signalrPlayerHub;

			_logging = logging;

			//_spellWornOffLogParse = _spellWornOffLogParse;
			_spellParser = spellParser;
			_dtParser = dtParser;
			_enrageParser = enrageParser;
			_invisParser = invisParser;
			_levParser = levParser;
			_failedFeignParser = failedFeignParser;
			_groupInviteParser = groupInviteParser;
			_resistSpellParser = resistSpellParser;
			_customOverlayParser = customOverlayParser;

			_zealMessageService.OnLabelMessageReceived += _zealMessageService_OnLabelMessageReceived;
			_zealMessageService.OnLogMessageReceived += _zealMessageService_OnLogMessageReceived;
			_zealMessageService.OnPlayerMessageReceived += _zealMessageService_OnPlayerMessageReceived;
			_zealMessageService.OnConnectionTerminated += _zealMessageService_onConnectionTerminated;
			_zealMessageService.OnPipeCmdMessageReceived += _zealMessageService_OnPipeCmdMessageReceived;

		}

		private void _zealMessageService_onConnectionTerminated(object sender, ConnectionTerminatedEventArgs e)
		{
			_settings.SelectedCharacter = null;
			_settings.ZealProcessID = 0;
			_activePlayer.Player.ZoneId = 0;
			SignalRPushDisconnect();
			SendStaticOverlayOff(Zeal_StaticOverlayType.Health);
			SendStaticOverlayOff(Zeal_StaticOverlayType.Mana);
		}

		private void _zealMessageService_OnLabelMessageReceived(object sender, ZealMessageService.LabelMessageReceivedEventArgs e)
		{
			if (e.Message != null && e.Message.Character == _settings.SelectedCharacter && e.ProcessId == _settings.ZealProcessID)
			{
				if (e.Message.Type == ZealPipes.Common.PipeMessageType.Label &&
					e.Message.Data != null && e.Message.Data.Length > 0)
				{
					if(_petName == string.Empty)
					{
						var petName = e.Message.Data.FirstOrDefault(x => x.Type == ZealPipes.Common.LabelType.PlayerPetName);
						if (petName != null && !string.IsNullOrWhiteSpace(petName.Value))
						{
							_petName = petName.Value;
						}
					}
					else
					{
						var petName = e.Message.Data.FirstOrDefault(x => x.Type == ZealPipes.Common.LabelType.PlayerPetName);
						if (petName == null || string.IsNullOrWhiteSpace(petName.Value))
						{
							_petName = string.Empty;
						}
					}

					var spellLabel = e.Message.Data.FirstOrDefault(x => x.Type == ZealPipes.Common.LabelType.CastingName);
					if (!string.IsNullOrWhiteSpace(spellLabel.Value))
					{
						var target = e.Message.Data.FirstOrDefault(x => x.Type == ZealPipes.Common.LabelType.TargetName)?.Value;
						if (string.Compare(target, e.Message.Character) == 0)
						{
							target = "You";
						}
						var spell = _spells.AllSpells.FirstOrDefault(a => a.name == spellLabel.Value);
						if (spell != null)
						{
							var spellparse = new SpellParsingMatch
							{
								Spell = _spells.AllSpells.FirstOrDefault(a => a.name == spell.name),
								TargetName = target,
								MultipleMatchesFound = false,
								IsYourCast = true
							};
							StartCastingEvent?.Invoke(this, new SpellEventArgs() { Spell = spellparse });
						}
					}
				}
				if(e.Message.Type == ZealPipes.Common.PipeMessageType.Label &&
					e.Message.Data != null && e.Message.Data.Length > 0)
				{
					var classLabel = e.Message.Data.FirstOrDefault(x => x.Type == ZealPipes.Common.LabelType.Class);
					List<string> nonManaClasses = new List<string> { "Warrior", "Rogue", "Monk"};
					if (!nonManaClasses.Contains(classLabel.Value))
					{
						var manaPercLabel = e.Message.Data.FirstOrDefault(x => x.Type == ZealPipes.Common.LabelType.ManaPerc);
						if(manaPercLabel != null && _settings.Zeal_ManaThresholdEnabled && decimal.TryParse(manaPercLabel.Value, out decimal manaPercent))
						{
							if(manaPercent <= _settings.Zeal_ManaThreshold)
							{
								ManaThresholdEvent?.Invoke(this, new ManaThresholdEventArgs() { ManaPercent = manaPercent, IsLow = true });
							}
							else
							{
								ManaThresholdEvent?.Invoke(this, new ManaThresholdEventArgs() { ManaPercent = manaPercent, IsLow = false });
							}
						}

						var healthPercLabel = e.Message.Data.FirstOrDefault(x => x.Type == ZealPipes.Common.LabelType.HPPerc);
						if(healthPercLabel != null && _settings.Zeal_HealthThresholdEnabled && decimal.TryParse(healthPercLabel.Value, out decimal healthPercent))
						{
							if(healthPercent <= _settings.Zeal_HealthThreshold)
							{
								HealthThresholdEvent?.Invoke(this, new HealthThresholdEventArgs() { HealthPercent = healthPercent, IsLow = true });
							}
							else
							{
								HealthThresholdEvent?.Invoke(this, new HealthThresholdEventArgs() { HealthPercent = healthPercent, IsLow = false });
							}
						}
					}

				}
			}
		}

		private void _zealMessageService_OnLogMessageReceived(object sender, ZealMessageService.LogMessageReceivedEventArgs e)
		{
			if (e.Message != null && e.Message.Character == _settings.SelectedCharacter && e.ProcessId == _settings.ZealProcessID)
			{
				if (e.Message.Type == ZealPipes.Common.PipeMessageType.LogText)
				{
					string yourFizzle = "Your spell fizzles!";
					string yourInterrupt = "Your spell is interrupted.";
					if (string.Compare(e.Message.Data.Text, yourFizzle) == 0)
					{
						FizzleCastingEvent?.Invoke(this, new FizzleEventArgs() { ExecutionTime = DateTime.Now });
						return;
					}
					else if (string.Compare(e.Message.Data.Text, yourInterrupt) == 0)
					{
						InterruptCastingEvent?.Invoke(this, new InterruptEventArgs() { ExecutionTime = DateTime.Now });
						return;
					}

					else if (_spellParser.MatchSpell(e.Message.Data.Text, out SpellParsingMatch spellMatch))
					{
						if (spellMatch != null && spellMatch.Spell.name != "Modulation")
						{
							StartCastingEvent?.Invoke(this, new SpellEventArgs { Spell = spellMatch });
							return;
						}
					}

					#region Prebuilt Event Overlays
					else if (_activePlayer?.Player?.CharmBreakOverlay ?? false && string.Compare(e.Message.Data.Text, "Your charm spell has worn off.", true) == 0)
					{
						CharmBreakEvent?.Invoke(this, new CharmBreakArgs());
						return;
					}
					else if ((_activePlayer?.Player?.ResistWarningOverlay ?? false) && _resistSpellParser.ParseNPCSpell(e.Message.Data.Text, out ResistSpellData resist_data))
					{
						ResistSpellEvent?.Invoke(this, resist_data);
						return;
					}
					else if ((_activePlayer?.Player?.GroupInviteOverlay ?? false) && e.Message.Data.Text.EndsWith(" invites you to join a group."))
					{
						GroupInviteEvent?.Invoke(this, e.Message.Data.Text);
						return;
					}
					else if ((_activePlayer?.Player?.FailedFeignOverlay ?? false) && e.Message.Data.Text == $"{_activePlayer?.Player?.Name} has fallen to the ground.")
					{
						FailedFeignEvent?.Invoke(this, string.Empty);
						return;
					}
					//else if((_activePlayer?.Player?.overlay ?? false) && _dtParser.DtCheck(e.Message.Data.Text, out DT_Event dt_data))
					//{
					//	DTEvent?.Invoke(this, dt_data);
					//}
					else if((_activePlayer?.Player?.EnrageOverlay ?? false) && _enrageParser.EnrageCheck(e.Message.Data.Text, out EnrageEvent enrage_data))
					{
						EnrageEvent?.Invoke(this, enrage_data);
						return;
					}
					else if ((_activePlayer?.Player?.InvisFadingOverlay ?? false) && _invisParser.Parse(e.Message.Data.Text) == InvisStatus.Fading)
					{
						InvisEvent?.Invoke(this, InvisStatus.Fading);
						return;
					}
					else if ((_activePlayer?.Player?.LevFadingOverlay ?? false) && _levParser.Parse(e.Message.Data.Text) == LevStatus.Fading)
					{
						LevEvent?.Invoke(this, LevStatus.Fading);
						return;
					}
					#endregion

					else if(_customOverlayParser.Parse(e.Message.Data.Text, _settings.CustomOverlays, out CustomOverlay customOverlay))
					{
						CustomOverlayEvent?.Invoke(this, new CustomOverlayEventArgs { CustomOverlay = customOverlay });
						return;
					}

				}
			}
		}

		private void _zealMessageService_OnPlayerMessageReceived(object sender, ZealMessageService.PlayerMessageReceivedEventArgs e)
		{
			if(e.Message != null)
			{
				if (_settings.ZealProcessID == 0)
				{
					if (string.IsNullOrWhiteSpace(_settings.SelectedCharacter))
					{
						_settings.ZealProcessID = e.ProcessId;
						_settings.SelectedCharacter = e.Message.Character;
					}
				}
				else if (!string.IsNullOrWhiteSpace(_settings.SelectedCharacter)
					&& string.Compare(_settings.SelectedCharacter, e.Message.Character, true) == 0)
				{
					_settings.ZealProcessID = e.ProcessId;
				}
				else if (_settings.ZealProcessID != 0 && _settings.ZealProcessID != e.ProcessId)
				{
					return;
				}
				if(e.Message.Data != null && e.Message.Data.AutoAttack != _IsAutoAttacking)
				{
					_IsAutoAttacking = e.Message.Data.AutoAttack;
					AutoAttackStatusChangedEvent?.Invoke(this, new AutoAttackStatusChangedEventArgs() { IsAutoAttacking = _IsAutoAttacking });
				}

				if(e.Message.Data != null && _activePlayer.Player != null && e.Message.Data.ZoneId > 0 && e.Message.Data.ZoneId != _activePlayer.Player.ZoneId)
				{
					_activePlayer.Player.LastZoneEntered = _activePlayer.Player.Zone;
					ZealZoneChangeEvent?.Invoke(this, new ZealLocationEventArgs() 
						{ 
							ZoneId = e.Message.Data.ZoneId, 
							Previous_ZoneId = _activePlayer.Player.ZoneId, 
							ProcessId = e.ProcessId 
						}
					);
					_activePlayer.Player.ZoneId = e.Message.Data.ZoneId;
				}

				ZealLocationEvent?.Invoke(this, e);
			}
		}

		private void _zealMessageService_OnPipeCmdMessageReceived(object sender, ZealMessageService.PipeCmdMessageReceivedEventArgs e)
		{
			if (e.Message != null && !string.IsNullOrWhiteSpace(e.Message.Data.Text))
			{
				if (LockCurrentCharacterCheck(e))
				{
					return;
				}
				else if (ToggleMap(e))
				{
					return;
				}
				else if (ToggleMobInfo(e))
				{
					return;
				}
				else if (ToggleOverlay(e))
				{
					return;
				}
				else if (ToggleAuraOverlay(e))
				{
					return;
				}
				else if (ToggleDPS(e))
				{
					return;
				}

				if (ManipulatePointOfInterest(e))
				{
					return;
				}
			}
		}

		private bool ManipulatePointOfInterest(ZealMessageService.PipeCmdMessageReceivedEventArgs e)
		{
			if (e.Message.Data.Text.StartsWith("quto poi add", StringComparison.OrdinalIgnoreCase))
			{
				string stripped = e.Message.Data.Text.Substring(12).Trim();
				string[] locParts = stripped.Split(',');
				Point3D loc;
				string label = locParts[0];
				if (stripped.Length == 0)
				{
					return false;
				}
				else
				{
					if (locParts.Length < 4)
					{
						return false;
					}
					double.TryParse(locParts[2], out double x);
					double.TryParse(locParts[1], out double y);
					double.TryParse(locParts[3], out double z);

					loc = new Point3D(x, y, z);
				}

				if (loc != null && label != string.Empty)
				{
					if(locParts.Any(l => string.Compare(l, "perm") == 0))
					{
						AddPointOfInterestEvent?.Invoke(this, new PointOfInterestEventArgs { Location = loc, Label = label, IsPermanent = true });
					}
					else
					{
						AddPointOfInterestEvent?.Invoke(this, new PointOfInterestEventArgs { Location = loc, Label = label });
					}
					return true;
				}
			}
			if(e.Message.Data.Text.StartsWith("quto poi remove", StringComparison.OrdinalIgnoreCase))
			{
				string stripped = e.Message.Data.Text.Substring(15).Trim();
				if (stripped.Length == 0)
				{
					return false;
				}
				else
				{
					RemovePointOfInterestEvent?.Invoke(this, new PointOfInterestEventArgs { Label = stripped });
					return true;
				}
			}
			return false;
		}
		private bool ToggleMap(ZealMessageService.PipeCmdMessageReceivedEventArgs e)
		{
			if (string.Compare(e.Message.Data.Text, "quto toggle map", true) == 0)
			{
				App.Current.Dispatcher.Invoke((Action)delegate
				{
					(App.Current as App).HardToggleMapWindow();
				});
				return true;
			}
			return false;
		}
		private bool ToggleMobInfo(ZealMessageService.PipeCmdMessageReceivedEventArgs e)
		{
			if (string.Compare(e.Message.Data.Text, "quto toggle mobinfo", true) == 0)
			{
				App.Current.Dispatcher.Invoke((Action)delegate
				{
					(App.Current as App).HardToggleMobInfoWindow();
				});
				return true;
			}
			return false;
		}
		private bool ToggleOverlay(ZealMessageService.PipeCmdMessageReceivedEventArgs e)
		{
			if (string.Compare(e.Message.Data.Text, "quto toggle overlay", true) == 0)
			{
				App.Current.Dispatcher.Invoke((Action)delegate
				{
					(App.Current as App).HardToggleOverlayWindow();
				});
				return true;
			}
			return false;
		}
		private bool ToggleAuraOverlay(ZealMessageService.PipeCmdMessageReceivedEventArgs e)
		{
			if (string.Compare(e.Message.Data.Text, "quto toggle auras", true) == 0)
			{
				App.Current.Dispatcher.Invoke((Action)delegate
				{
					(App.Current as App).HardToggleImageOverlayWindow();
				});
				return true;
			}
			return false;
		}
		private bool ToggleDPS(ZealMessageService.PipeCmdMessageReceivedEventArgs e)
		{
			if (string.Compare(e.Message.Data.Text, "quto toggle dps", true) == 0)
			{
				App.Current.Dispatcher.Invoke((Action)delegate
				{
					(App.Current as App).HardToggleDPSWindow();
				});
				return true;
			}
			return false;
		}
		private bool LockCurrentCharacterCheck(ZealMessageService.PipeCmdMessageReceivedEventArgs e)
		{
			if (e.Message.Data.Text.StartsWith("quto lock character", StringComparison.OrdinalIgnoreCase))
			{
				_settings.SelectedCharacter = e.Message.Character;
				_settings.ZealProcessID = e.ProcessId;
				return true;
			}
			return false;
		}
		private void SignalRPushDisconnect()
		{
			_appDispatcher.DispatchUI(() =>
			{
				var player = new SignalrPlayer
				{
					Zone = this._activePlayer.Player.Zone,
					GuildName = this._activePlayer.Player.GuildName,
					PlayerClass = this._activePlayer.Player.PlayerClass,
					Server = this._activePlayer.Player.Server.Value,
					MapLocationSharing = this._activePlayer.Player.MapLocationSharing,
					Name = this._activePlayer.Player.Name,
					TrackingDistance = this._activePlayer.Player.TrackingDistance
				};
				_signalrPlayerHub.PushPlayerDisconnected(player);
			});
		}

		public void SendStaticOverlayOff(Zeal_StaticOverlayType overlayType)
		{
			if(overlayType == Zeal_StaticOverlayType.Health)
			{

				HealthThresholdEvent?.Invoke(this, new HealthThresholdEventArgs() { HealthPercent = 100, IsLow = false });
			}
			else if(overlayType == Zeal_StaticOverlayType.Mana)
			{
				ManaThresholdEvent?.Invoke(this, new ManaThresholdEventArgs() { ManaPercent = 100, IsLow = false });
			}
		}

		public class SpellEventArgs : EventArgs
		{
			public SpellParsingMatch Spell { get; set; }
		}

		public class SignalRLocationEventArgs : EventArgs
		{
			public Point3D Location { get; set; }
		}

		public class FizzleEventArgs : EventArgs
		{
			public DateTime ExecutionTime { get; set; }
		}

		public class InterruptEventArgs : EventArgs
		{
			public DateTime ExecutionTime { get; set; }
		}
		public class ZealLocationEventArgs : EventArgs
		{
			public int ZoneId { get; set; }
			public int Previous_ZoneId { get; set; }
			public int ProcessId { get; set; }
		}

		public class ManaThresholdEventArgs : EventArgs
		{
			public decimal ManaPercent { get; set; }
			public bool IsLow { get; set; }
		}
		public class HealthThresholdEventArgs : EventArgs
		{
			public decimal HealthPercent { get; set; }
			public bool IsLow { get; set; }
		}
		public class PointOfInterestEventArgs : EventArgs
		{
			public Point3D Location { get; set; }
			public string Label { get; set; }
			public bool IsPermanent { get; set; }
		}
		public class AutoAttackStatusChangedEventArgs : EventArgs
		{
			public bool IsAutoAttacking { get; set; }
		}

		public event EventHandler<SpellEventArgs> StartCastingEvent;
		public event EventHandler<FizzleEventArgs> FizzleCastingEvent;
		public event EventHandler<InterruptEventArgs> InterruptCastingEvent;
		public event EventHandler<PlayerMessageReceivedEventArgs> ZealLocationEvent;
		public event EventHandler<ZealLocationEventArgs> ZealZoneChangeEvent;
		public event EventHandler<ManaThresholdEventArgs> ManaThresholdEvent;
		public event EventHandler<HealthThresholdEventArgs> HealthThresholdEvent;
		public event EventHandler<AutoAttackStatusChangedEventArgs> AutoAttackStatusChangedEvent;
		public event EventHandler<CustomOverlayEventArgs> CustomOverlayEvent;

		//public event EventHandler<SpellWornOffSelfEventArgs> SpellWornOffSelfEvent;
		//public event EventHandler<SpellWornOffOtherEventArgs> SpellWornOtherOffEvent;
		public event EventHandler<DT_Event> DTEvent;
		public event EventHandler<EnrageEvent> EnrageEvent;
		public event EventHandler<LevStatus> LevEvent;
		public event EventHandler<InvisStatus> InvisEvent;
		public event EventHandler<CharmBreakArgs> CharmBreakEvent;
		public event EventHandler<string> FailedFeignEvent;
		public event EventHandler<string> GroupInviteEvent;
		public event EventHandler<ResistSpellData> ResistSpellEvent;

		public event EventHandler<PointOfInterestEventArgs> AddPointOfInterestEvent;
		public event EventHandler<PointOfInterestEventArgs> RemovePointOfInterestEvent;

		public void Dispose()
		{
			_zealMessageService.StopProcessing();
		}

		internal void Start()
		{
			_zealMessageService.StartProcessing();
		}
	}
}