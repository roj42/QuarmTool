﻿using EQTool.Models;
using EQTool.Services;
using EQToolShared.Enums;
using EQToolShared.HubModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EQTool.ViewModels
{
    public class UISpell : INotifyPropertyChanged
    {
        public UISpell(DateTime endtime, bool isNPC)
        {
            this.IsNPC = isNPC;
            TimerEndDateTime = endtime;
            TotalSecondsOnSpell = (int)(TimerEndDateTime - DateTime.Now).TotalSeconds;
            UpdateTimeLeft();
		}
		public UISpell(DateTime endTime, DateTime maxEndTime, bool isNPC, bool simpleTimers)
		{
			this.IsNPC = isNPC;
			TimerEndDateTime = endTime;
			MaxTimerEndDateTime = maxEndTime;
			TotalSecondsOnSpell = (int)(TimerEndDateTime - DateTime.Now).TotalSeconds;
			NegativeSecondsOnSpell = (int)(maxEndTime - DateTime.Now).TotalSeconds;
			UpdateTimeLeft();
			ShowSimpleTimers = simpleTimers;
		}

		public SpellIcon SpellIcon { get; set; }

        public DateTime UpdatedDateTime { get; set; }
		public DateTime ExecutionTime { get; set; }

		private TimeSpan _NegativeDurationToShow;

        private TimeSpan _SecondsLeftOnSpell;

		public int NegativeSecondsOnSpell { get; private set; }
        public int TotalSecondsOnSpell { get; private set; }

		public TimeSpan NegativeDurationToShow => _NegativeDurationToShow;
        public TimeSpan SecondsLeftOnSpell => _SecondsLeftOnSpell;

		private DateTime _MaxTimerEndDateTime = DateTime.Now;
		public DateTime MaxTimerEndDateTime
		{
			get { return _MaxTimerEndDateTime; }
			set
			{
				_MaxTimerEndDateTime = value;
				if(ExecutionTime == null)
				{
					NegativeSecondsOnSpell = (int)(_MaxTimerEndDateTime - DateTime.Now).TotalSeconds;
				}
				else
				{
					NegativeSecondsOnSpell = (int)(_MaxTimerEndDateTime - ExecutionTime).TotalSeconds;
				}
				UpdateTimeLeft();
			}
		}

		private DateTime _TimerEndDateTime = DateTime.Now;
        public DateTime TimerEndDateTime
        {
            get { return _TimerEndDateTime; }
            set
            {
                _TimerEndDateTime = value;
				if (ExecutionTime == null)
				{
					TotalSecondsOnSpell = (int)(_TimerEndDateTime - DateTime.Now).TotalSeconds;
				}
				else
				{
					TotalSecondsOnSpell = (int)(_TimerEndDateTime - ExecutionTime).TotalSeconds;
				}
				UpdateTimeLeft();
            }
        }

        public void UpdateTimeLeft()
		{
			_SecondsLeftOnSpell = TimerEndDateTime - DateTime.Now;
			_NegativeDurationToShow = MaxTimerEndDateTime - DateTime.Now;
            if (TotalSecondsOnSpell > 0)
            {
                PercentLeftOnSpell = (int)(_SecondsLeftOnSpell.TotalSeconds / TotalSecondsOnSpell * 100);
            }
			if(_SecondsLeftOnSpell.TotalSeconds < 0)
			{
				PercentLeftOnSpell = (int)(_NegativeDurationToShow.TotalSeconds / NegativeSecondsOnSpell * 100);
			}
			OnPropertyChanged(nameof(NegativeSecondsOnSpell));
            OnPropertyChanged(nameof(SecondsLeftOnSpell));
            OnPropertyChanged(nameof(SecondsLeftOnSpellPretty));
			OnPropertyChanged(nameof(SimpleSpellDuration));
			OnPropertyChanged(nameof(SpellDuration));
			OnPropertyChanged(nameof(PercentLeftOnSpell));
        }

        public bool HasSpellIcon => SpellIcon != null;

        public Int32Rect Rect { get; set; }

        private string _SpellName = string.Empty;

        public string SpellName
        {
            get => _SpellName;
            set
            {
                _SpellName = value;
                OnPropertyChanged();
            }
        }

        public string SpellExtraData
        {
            get
            {
                if (_Counter.HasValue)
                {
                    return " Count --> ";
                }
                else if (Roll >= 0)
                {
                    return $" (#{RollOrder}) Roll --> ";
                }
                return string.Empty;
            }
        }

        public string SpellExtraData2
        {
            get
            {
                if (_Counter.HasValue)
                {
                    return _Counter.Value.ToString();
                }
                else if (Roll >= 0)
                {
                    return Roll.ToString();
                }
                return string.Empty;
            }
        }

        private int? _Counter = null;

        public int? Counter
        {
            get => _Counter;
            set
            {
                _Counter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SpellExtraData));
                OnPropertyChanged(nameof(SpellExtraData2));
            }
        }

        public bool GuessedSpell { get; set; }

        public Dictionary<PlayerClasses, int> Classes { get; set; } = new Dictionary<PlayerClasses, int>();

        private bool _HideGuesses = true;

        public bool HideGuesses
        {
            get => _HideGuesses;
            set
            {
                if (_HideGuesses == value)
                {
                    return;
                }
                _HideGuesses = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ColumnVisibility));
            }
        }

        private bool _ShowOnlyYou = false;
        public bool ShowOnlyYou
        {
            get => _ShowOnlyYou;
            set
            {
                if (_ShowOnlyYou == value)
                {
                    return;
                }
                _ShowOnlyYou = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ColumnVisibility));
            }
        }

        private bool _HideClasses = true;
        public bool HideClasses
        {
            get => _HideClasses;
            set
            {
                if (_HideClasses == value)
                {
                    return;
                }
                _HideClasses = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ColumnVisibility));
            }
        }

        private Visibility _HeaderVisibility = Visibility.Visible;

        public Visibility HeaderVisibility
        {
            get => _HeaderVisibility;
            set
            {
                if (_HeaderVisibility == value)
                {
                    return;
                }
                _HeaderVisibility = value;
                OnPropertyChanged();
            }
        }

        public Visibility ColumnVisibility
        {
            get
            {
                if (TargetName == CustomTimer.CustomerTime || _SpellType == SpellTypes.Detrimental || _SpellType == SpellTypes.DeathTouch)
                {
                    return Visibility.Visible;
                }
                else if (_HideGuesses && GuessedSpell)
                {
                    return Visibility.Collapsed;
                }
                else if (_HideClasses)
                {
                    return Visibility.Collapsed;
                }
                else if (_SpellType <= 0 || TargetName == EQSpells.SpaceYou)
                {
                    return Visibility.Visible;
                }
                else if (_ShowOnlyYou && TargetName != EQSpells.SpaceYou/* && TargetName != "Death Touches"*/)
                {
                    return Visibility.Collapsed;
                }

                return Visibility.Visible;
            }
        }

		private Visibility _dropShadowVisibility = Visibility.Collapsed;
		public Visibility DropShadowVisibility
		{
			get
			{
				return _dropShadowVisibility;
			}
			set
			{
				_dropShadowVisibility = value;
				OnPropertyChanged();
			}
		}

		public bool ShowSimpleTimers;

		public string SpellDuration
		{
			get
			{
				if(ShowSimpleTimers)
				{
					return SimpleSpellDuration;
				}
				else
				{
					return SecondsLeftOnSpellPretty;
				}
			}
		}

        public string SecondsLeftOnSpellPretty
        {
            get
            {
				StringBuilder st = new StringBuilder();
                if (_SecondsLeftOnSpell.Hours > 0)
                {
                    st.Append(_SecondsLeftOnSpell.Hours + "h ");
                }
				else if (_SecondsLeftOnSpell.Hours <= 0 && _SecondsLeftOnSpell.Seconds <= 0 && _NegativeDurationToShow.Hours > 0)
				{
					st.Append("-" + _NegativeDurationToShow.Hours + "h ");
				}
                if (_SecondsLeftOnSpell.Minutes > 0)
                {
                    st.Append(_SecondsLeftOnSpell.Minutes + "m ");
                }
				else if (_SecondsLeftOnSpell.Minutes <= 0 && _SecondsLeftOnSpell.Seconds <= 0 && _NegativeDurationToShow.Minutes > 0)
				{
					st.Append("-" + _NegativeDurationToShow.Minutes + "m ");
				}
                if (_SecondsLeftOnSpell.Seconds > 0)
                {
                    st.Append(_SecondsLeftOnSpell.Seconds + "s");
                }
				else if (_SecondsLeftOnSpell.Seconds <= 0 && _SecondsLeftOnSpell.Seconds <= 0 && _NegativeDurationToShow.Seconds > 0)
				{
					st.Append("-" + _NegativeDurationToShow.Seconds + "s");
				}
                return st.ToString();

            }
		}

		public string SimpleSpellDuration
		{
			get
			{
				if (_SecondsLeftOnSpell.Hours > 0)
				{
					return _SecondsLeftOnSpell.Hours + "h ";
				}
				else if (_SecondsLeftOnSpell.Hours <= 0 && _SecondsLeftOnSpell.Seconds <= 0 && _NegativeDurationToShow.Hours > 0)
				{
					return "-" + _NegativeDurationToShow.Hours + "h ";
				}
				if (_SecondsLeftOnSpell.Minutes > 0)
				{
					return _SecondsLeftOnSpell.Minutes + "m ";
				}
				else if (_SecondsLeftOnSpell.Minutes <= 0 && _SecondsLeftOnSpell.Seconds <= 0 && _NegativeDurationToShow.Minutes > 0)
				{
					return "-" + _NegativeDurationToShow.Minutes + "m ";
				}
				if (_SecondsLeftOnSpell.Seconds > 0)
				{
					return _SecondsLeftOnSpell.Seconds + "s";
				}
				else if (_SecondsLeftOnSpell.Seconds <= 0 && _SecondsLeftOnSpell.Seconds <= 0 && _NegativeDurationToShow.Seconds > 0)
				{
					return "-" + _NegativeDurationToShow.Seconds + "s";
				}

				return string.Empty;
			}
		}

		public bool PersistentSpell { get; set; }
        public string Sorting
        {
            get
            {
                if (TargetName.StartsWith(" "))
                {
                    return TargetName;
                }
                if (this.Roll >= 0)
                {
                    return " y";
                }
                if (this.IsNPC)
                {
                    return " z";
                }
                return TargetName;
            }
        }
        public string TargetName { get; set; }
        public int Roll { get; set; } = -1;
        public int RollOrder { get; set; } = 0;
        public bool IsNPC { get; set; }

        private SpellTypes _SpellType = 0;

        public SpellTypes SpellType
        {
            get => _SpellType;
            set
            {
                _SpellType = value;
                if (_SpellType == SpellTypes.Beneficial)
                {
                    ProgressBarColor = Brushes.MediumAquamarine;
                }
                else if (_SpellType == SpellTypes.Detrimental
					|| _SpellType == SpellTypes.BadGuyCoolDown)
                {
                    ProgressBarColor = Brushes.OrangeRed;
                }
                else if (_SpellType >= SpellTypes.RespawnTimer)
                {
                    ProgressBarColor = Brushes.LightSalmon;
                }
                else if (_SpellType >= SpellTypes.DisciplineCoolDown)
                {
                    ProgressBarColor = Brushes.Gold;
                }
                else
                {
                    ProgressBarColor = Brushes.DarkSeaGreen;
                }
            }
        }

        public SolidColorBrush ProgressBarColor { get; set; }
		public SolidColorBrush SpellNameColor { get; set; }
		public SolidColorBrush SpellNameInverseColor 
		{ 
			get
			{
				return SpellNameColor.InvertColor();
			}
		}

		public int PercentLeftOnSpell { get; set; }

		public bool IsYourCast { get; set; } = false;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
