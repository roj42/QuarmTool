﻿using Dapper;
using Dapper.Contrib.Extensions;
using EQTool.Models;
using EQTool.Properties;
using EQToolShared.HubModels;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static EQTool.Services.FindEq;

namespace EQTool.Services.Parsing
{
	public class CustomOverlayParser
	{

		public static CustomOverlay Parse(string message, IEnumerable<CustomOverlay> overlays)
		{
			if(overlays == null)
			{
				return null;
			}
			if(overlays.Count() > 0)
			{
				foreach(var overlay in overlays)
				{
					var triggerMatches = Regex.Matches(message, overlay.Trigger, RegexOptions.IgnoreCase);
					var altTriggerMatches = Regex.Matches(message, overlay.Alternate_Trigger, RegexOptions.IgnoreCase);

					if (overlay.IsEnabled && !string.IsNullOrWhiteSpace(overlay.Trigger) 
						&& !string.IsNullOrWhiteSpace(overlay.Message) && triggerMatches.Count > 0)
					{
						var retOverlay = overlay.ShallowClone();
						if (triggerMatches[0].Groups.Count >= 1)
						{
							retOverlay.Message = retOverlay.Message.Replace("{0}", triggerMatches[0].Groups[1].Value);
							retOverlay.AudioMessage = retOverlay.AudioMessage.Replace("{0}", triggerMatches[0].Groups[1].Value);
						}
						if (triggerMatches[0].Groups.Count >= 2)
						{
							retOverlay.Message = retOverlay.Message.Replace("{1}", triggerMatches[0].Groups[2].Value);
							retOverlay.AudioMessage = retOverlay.AudioMessage.Replace("{1}", triggerMatches[0].Groups[2].Value);
						}
						return retOverlay;
					}
					if (overlay.IsEnabled && !string.IsNullOrWhiteSpace(overlay.Alternate_Trigger)
						&& !string.IsNullOrWhiteSpace(overlay.Message) && altTriggerMatches.Count > 0)
					{
						var retOverlay = overlay.ShallowClone();
						if (altTriggerMatches[0].Groups.Count >= 1)
						{
							retOverlay.Message = retOverlay.Message.Replace("{0}", altTriggerMatches[0].Groups[1].Value);
							retOverlay.AudioMessage = retOverlay.AudioMessage.Replace("{0}", altTriggerMatches[0].Groups[1].Value);
						}
						if (altTriggerMatches[0].Groups.Count >= 2)
						{
							retOverlay.Message = retOverlay.Message.Replace("{1}", altTriggerMatches[0].Groups[2].Value);
							retOverlay.AudioMessage = retOverlay.AudioMessage.Replace("{1}", altTriggerMatches[0].Groups[2].Value);
						}
						return retOverlay;
					}
				}
			}
			return null;
		}

		public bool Parse(string message, IEnumerable<CustomOverlay> overlays, out CustomOverlay ret)
		{
			if (overlays == null)
			{
				ret = null;
				return false;
			}
			if (overlays.Count() > 0)
			{
				foreach (var overlay in overlays)
				{
					var triggerMatches = Regex.Matches(message, overlay.Trigger, RegexOptions.IgnoreCase);
					var altTriggerMatches = Regex.Matches(message, overlay.Alternate_Trigger, RegexOptions.IgnoreCase);

					if (overlay.IsEnabled && !string.IsNullOrWhiteSpace(overlay.Trigger)
						&& !string.IsNullOrWhiteSpace(overlay.Message) && triggerMatches.Count > 0)
					{
						ret = overlay.ShallowClone();
						if (triggerMatches[0].Groups.Count >= 1)
						{
							ret.Message = ret.Message.Replace("{0}", triggerMatches[0].Groups[1].Value);
							ret.AudioMessage = ret.AudioMessage.Replace("{0}", triggerMatches[0].Groups[1].Value);
						}
						if (triggerMatches[0].Groups.Count >= 2)
						{
							ret.Message = ret.Message.Replace("{1}", triggerMatches[0].Groups[2].Value);
							ret.AudioMessage = ret.AudioMessage.Replace("{1}", triggerMatches[0].Groups[2].Value);
						}
						return true;
					}
					if (overlay.IsEnabled && !string.IsNullOrWhiteSpace(overlay.Alternate_Trigger)
						&& !string.IsNullOrWhiteSpace(overlay.Message) && altTriggerMatches.Count > 0)
					{
						ret = overlay.ShallowClone();
						if (altTriggerMatches[0].Groups.Count >= 1)
						{
							ret.Message = ret.Message.Replace("{0}", altTriggerMatches[0].Groups[1].Value);
							ret.AudioMessage = ret.AudioMessage.Replace("{0}", altTriggerMatches[0].Groups[1].Value);
						}
						if (altTriggerMatches[0].Groups.Count >= 2)
						{
							ret.Message = ret.Message.Replace("{1}", altTriggerMatches[0].Groups[2].Value);
							ret.AudioMessage = ret.AudioMessage.Replace("{1}", altTriggerMatches[0].Groups[2].Value);
						}
						return true;
					}
				}
			}

			ret = null;
			return false;
		}
	}
}
