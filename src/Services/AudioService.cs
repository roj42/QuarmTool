﻿using EQTool.EventArgModels;
using EQTool.Models;
using EQTool.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;

namespace EQTool.Services
{
    public class ChainAudioData : ChainData
    {
        public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
        public string TargetName { get; set; }
    }

    public class AudioService
    {
        private readonly LogParser _logParser;
		private readonly PipeParser _pipeParser;
        private readonly ActivePlayer _activePlayer;
        private readonly EQToolSettings _settings;
        private readonly List<ChainAudioData> chainDatas = new List<ChainAudioData>();

        public AudioService(LogParser logParser, ActivePlayer activePlayer, EQToolSettings settings, PipeParser pipeParser)
        {
			_logParser = logParser;
			_pipeParser = pipeParser;
			_activePlayer = activePlayer;
			_settings = settings;
            this._logParser.InvisEvent += LogParser_InvisEvent;
            this._logParser.EnrageEvent += LogParser_EnrageEvent;
            this._logParser.LevEvent += LogParser_LevEvent;
            this._logParser.FTEEvent += LogParser_FTEEvent;
            this._logParser.CharmBreakEvent += LogParser_CharmBreakEvent;
            this._logParser.FailedFeignEvent += LogParser_FailedFeignEvent;
            this._logParser.GroupInviteEvent += LogParser_GroupInviteEvent;
            this._logParser.StartCastingEvent += LogParser_StartCastingEvent;
            this._logParser.CHEvent += LogParser_CHEvent;
            this._logParser.SpellWornOtherOffEvent += LogParser_SpellWornOtherOffEvent1;
            this._logParser.ResistSpellEvent += LogParser_ResistSpellEvent;
			this._logParser.CustomOverlayEvent += LogParser_CustomOverlayEvent;
			
			_pipeParser.CustomOverlayEvent += LogParser_CustomOverlayEvent;
			_pipeParser.ResistSpellEvent += LogParser_ResistSpellEvent;
			_pipeParser.GroupInviteEvent += LogParser_GroupInviteEvent;
			_pipeParser.FailedFeignEvent += LogParser_FailedFeignEvent;
			_pipeParser.CharmBreakEvent += LogParser_CharmBreakEvent;
			_pipeParser.InvisEvent += LogParser_InvisEvent;
			_pipeParser.LevEvent += LogParser_LevEvent;
			_pipeParser.EnrageEvent += LogParser_EnrageEvent;
		}

		private void LogParser_ResistSpellEvent(object sender, ResistSpellParser.ResistSpellData e)
        {
            var overlay = this._activePlayer?.Player?.ResistWarningAudio ?? false;
            if (!overlay)
            {
                return;
            }
            var target = e.isYou ? "You " : "Your target ";
            this.PlayResource($"{target} resisted the {e.Spell.name} spell");
        }

        private List<string> RootSpells = new List<string>()
        {
            "Root",
            "Fetter",
            "Enstill",
            "Immobalize",
            "Paralyzing Earth",
            "Grasping Roots",
            "Ensnaring Roots",
            "Enveloping Roots",
            "Engulfing Roots",
            "Engorging Roots",
            "Entrapping Roots"
        };

        private void LogParser_SpellWornOtherOffEvent1(object sender, LogParser.SpellWornOffOtherEventArgs e)
        {
            var overlay = this._activePlayer?.Player?.RootWarningAudio ?? false;
            if (!overlay)
            {
                return;
            }

            if (RootSpells.Any(a => string.Equals(a, e.SpellName, StringComparison.OrdinalIgnoreCase)))
            {
                this.PlayResource($"{e.SpellName} has worn off!");
            }
        }

        private void LogParser_CHEvent(object sender, ChParser.ChParseData e)
        {
            var overlay = this._activePlayer?.Player?.ChChainWarningAudio ?? false;
            if (!overlay)
            {
                return;
            }

            var chaindata = this.GetOrCreateChain(e);
            var shouldwarn = CHService.ShouldWarnOfChain(chaindata, e);
            if (shouldwarn)
            {
                this.PlayResource($"CH Warning");
            }
        }

        private ChainAudioData GetOrCreateChain(ChParser.ChParseData e)
        {
            var d = DateTime.UtcNow;
            var toremove = this.chainDatas.Where(a => (d - a.UpdatedTime).TotalSeconds > 20).ToList();
            foreach (var item in toremove)
            {
                this.chainDatas.Remove(item);
            }

            var f = this.chainDatas.FirstOrDefault(a => a.TargetName == e.Recipient);
            if (f == null)
            {
                f = new ChainAudioData
                {
                    UpdatedTime = d,
                    TargetName = e.Recipient
                };
                this.chainDatas.Add(f);
            }
            f.UpdatedTime = d;
            return f;
        }

        private void LogParser_StartCastingEvent(object sender, LogParser.SpellEventArgs e)
        {
            var overlay = this._activePlayer?.Player?.DragonRoarAudio ?? false;
            if (!overlay || e.Spell.Spell.name != "Dragon Roar")
            {
                return;
            }

            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                System.Threading.Thread.Sleep(1000 * 30);
                this.PlayResource($"Dragon Roar in 6 Seconds!");
                System.Threading.Thread.Sleep(1000);
                System.Threading.Thread.Sleep(1000);
                this.PlayResource($"4 Seconds!");
                System.Threading.Thread.Sleep(1000);
                System.Threading.Thread.Sleep(1000);
                this.PlayResource($"2");
                System.Threading.Thread.Sleep(1000);
                this.PlayResource($"1");
                System.Threading.Thread.Sleep(1000);
            });
        }

        private void LogParser_GroupInviteEvent(object sender, string e)
        {
            if (this._activePlayer?.Player?.GroupInviteAudio == true)
            {
                this.PlayResource(e);
            }
        }

        private void LogParser_FailedFeignEvent(object sender, string e)
        {
            if (this._activePlayer?.Player?.FailedFeignAudio == true)
            {
                this.PlayResource($"Failed Feign Death");
            }
        }

        private void LogParser_CharmBreakEvent(object sender, LogParser.CharmBreakArgs e)
        {
            if (this._activePlayer?.Player?.CharmBreakAudio == true)
            {
                this.PlayResource($"Charm Break");
            }
        }

        private void LogParser_FTEEvent(object sender, FTEParser.FTEParserData e)
        {
            if (this._activePlayer?.Player?.FTEAudio == true)
            {
                this.PlayResource($"{e.FTEPerson} F T E {e.NPCName}");
            }
        }

        private void LogParser_LevEvent(object sender, LevParser.LevStatus e)
        {
            if (this._activePlayer?.Player?.LevFadingAudio == true)
            {
                this.PlayResource("Levitate Fading");
            }
        }

        private void PlayResource(string text)
        {
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                var synth = new SpeechSynthesizer();
                if (string.IsNullOrWhiteSpace(this._settings.SelectedVoice))
                {
                    synth.SetOutputToDefaultAudioDevice();
                }
                else
                {
                    synth.SelectVoice(this._settings.SelectedVoice);
                }
				synth.Volume = this._settings.AudioTriggerVolume;
                synth.Speak(text);
            });
        }

        private void LogParser_EnrageEvent(object sender, EnrageParser.EnrageEvent e)
        {
            if (this._activePlayer?.Player?.EnrageAudio == true)
            {
                this.PlayResource($"{e.NpcName} is enraged.");
            }
        }

        private void LogParser_InvisEvent(object sender, InvisParser.InvisStatus e)
        {
            if (this._activePlayer?.Player?.InvisFadingAudio == true)
            {
                this.PlayResource($"Invisability Fading.");
            }
        }

		private void LogParser_CustomOverlayEvent(object sender, CustomOverlayEventArgs e)
		{
			if (e.CustomOverlay.IsAudioEnabled)
			{
				this.PlayResource(e.CustomOverlay.AudioMessage);
			}
		}
	}
}
