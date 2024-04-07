﻿using EQTool.Services.Parsing;
using EQTool.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace EQTool.Models
{
	public class JsonMonster
	{
		#region Properties
		public int AC { get; set; }
		public int CR { get; set; }
		public int DR { get; set; }
		public int FR { get; set; }
		public int MR { get; set; }
		public int PR { get; set; }
		public int hp { get; set; }
		public int id { get; set; }
		public int mana { get; set; }
		public string name { get; set; }
		public string race { get; set; }
		public string npc_class { get; set; }
		public int greed { get; set; }
		public int level { get; set; }
		public int maxdmg { get; set; }
		public int mindmg { get; set; }
		public int isquest { get; set; }
		public int maxlevel { get; set; }
		public float runspeed { get; set; }
		public int see_invis { get; set; }
		public int see_sneak { get; set; }
		public string zone_code { get; set; }
		public string zone_name { get; set; }
		public int merchant_id { get; set; }
		public int attack_count { get; set; }
		public int attack_delay { get; set; }
		public int loottable_id { get; set; }
		public int npc_spells_id { get; set; }
		public int mitigates_slow { get; set; }
		public int npc_faction_id { get; set; }
		public int combat_hp_regen { get; set; }
		public string primary_faction { get; set; }
		public int slow_mitigation { get; set; }
		public int see_invis_undead { get; set; }
		public int combat_mana_regen { get; set; }
		public int see_improved_hide { get; set; }
		public string special_abilities { get; set; }
		public int unique_spawn_by_name { get; set; }

		public List<JsonMonsterFaction> Factions { get; set; } = new List<JsonMonsterFaction>();
		public List<JsonMonsterDrops> Drops { get; set; } = new List<JsonMonsterDrops>();
		#endregion

		

	}
}
