﻿using EQTool.Models;
using EQTool.ViewModels;
using System.Diagnostics;
using System.Linq;

namespace EQTool.Services.Spells.Log
{
    public class ParseSpellGuess
    {
        private readonly ActivePlayer activePlayer;
        private readonly EQSpells spells;

        public ParseSpellGuess(ActivePlayer activePlayer, EQSpells spells)
        {
            this.activePlayer = activePlayer;
            this.spells = spells;
        }

        public SpellParsingMatch HandleBestGuessSpell(string message)
        {
            var removename = message.IndexOf("'");
            if (removename != -1)
            {
                var spellmessage = message.Substring(removename).Trim();
                if (spells.CastOtherSpells.TryGetValue(spellmessage, out var foundspells))
                {
                    var foundspell = SpellDurations.MatchClosestLevelToSpell(foundspells, activePlayer.Player);
                    var targetname = message.Replace(foundspell.cast_on_other, string.Empty).Trim();
                    Debug.WriteLine($"Other Spell: {foundspell.name} Message: {spellmessage}");
                    var multiplematches = foundspell.Classes.All(a => a.Key == PlayerClasses.Unknown || a.Value == 255) && foundspells.Count > 1;
                    if (EQSpells.RealSpells.Any(a => a == foundspell.name))
                    {
                        multiplematches = false;
                    }
                    return new SpellParsingMatch
                    {
                        Spell = foundspell,
                        TargetName = targetname,
                        MutipleMatchesFound = multiplematches
                    };
                }
            }
            else
            {
                removename = message.IndexOf(" ");
                if (removename != -1)
                {
                    var spellmessage = message.Substring(removename).Trim();
                    if (spells.CastOtherSpells.TryGetValue(spellmessage, out var foundspells))
                    {
                        var foundspell = SpellDurations.MatchClosestLevelToSpell(foundspells, activePlayer.Player);
                        var targetname = message.Replace(foundspell.cast_on_other, string.Empty).Trim();
                        Debug.WriteLine($"Other Spell: {foundspell.name} Message: {spellmessage}");
                        var multiplematches = foundspell.Classes.All(a => a.Key == PlayerClasses.Unknown || a.Value == 255) && foundspells.Count > 1;
                        if (EQSpells.RealSpells.Any(a => a == foundspell.name))
                        {
                            multiplematches = false;
                        }
                        return new SpellParsingMatch
                        {
                            Spell = foundspell,
                            TargetName = targetname,
                            MutipleMatchesFound = multiplematches
                        };
                    }
                }
            }
            return null;
        }
    }
}