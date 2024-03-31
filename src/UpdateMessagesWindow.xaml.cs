﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;

namespace EQTool
{
    public class UpdateMessageData : INotifyPropertyChanged
    {
        public string Date { get { return DateTime.ToShortDateString(); } }
        public DateTime DateTime { get; set; }
        public string Message { get; set; }
        public string Image { get; set; }
        public Visibility ImageVisibility { get; set; } = Visibility.Collapsed;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public partial class UpdateMessagesWindow : Window
    {
        public ObservableCollection<UpdateMessageData> UpdateMessages { get; set; } = new ObservableCollection<UpdateMessageData>();
        public UpdateMessagesWindow()
        {
            this.Topmost = true;
            this.DataContext = this;
            InitializeComponent();
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(Messages.ItemsSource);
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(UpdateMessageData.Date)));
            view.LiveGroupingProperties.Add(nameof(UpdateMessageData.Message));
            view.IsLiveGrouping = true;
            view.SortDescriptions.Add(new SortDescription(nameof(UpdateMessageData.DateTime), ListSortDirection.Descending));
            view.IsLiveSorting = true;

            UpdateMessages.Add(new UpdateMessageData
            {
                DateTime = new DateTime(2024, 3, 31),
                Message = $"* This will be the last time this update window is used. All future updates will be listed in the discord channel." +
                 $"* City of mist reaver spawn times addded. {Environment.NewLine}" +
                  $"* Timers are now sorted to be shortest to longest."
            });

            UpdateMessages.Add(new UpdateMessageData
            {
                DateTime = new DateTime(2024, 3, 26),
                Message = $"* Updated spawn time for mobs in BB."
            });

            UpdateMessages.Add(new UpdateMessageData
            {
                DateTime = new DateTime(2024, 3, 24),
                Message = $"* Updated the auto update code so that updates will ONLY occur when the computer is idle for at least 10 minutes.{Environment.NewLine}" +
                    $"* Refactored window code. {Environment.NewLine}" +
                    $"* Fixed overlay window so if is closed, the setting will be saved."
            });

            UpdateMessages.Add(new UpdateMessageData
            {
                DateTime = new DateTime(2024, 3, 23),
                Message = $"* Fixed counters which stopped working a few days ago. {Environment.NewLine}" +
                $"* Fixed SK epic from adding wrong message in triggers window.{Environment.NewLine}" +
                $"* Fixed loot parsing from showing up in window broken for items with commas in their name."
            });
            UpdateMessages.Add(new UpdateMessageData
            {
                DateTime = new DateTime(2024, 3, 17),
                Message =
                $"* Updated random rolls to always show duplicate rolls, but include the ROLL ORDER.{Environment.NewLine}" +
                $"* Below is what is going to go out soon. In below example the winner is Vasanle with a 292. The (#NUMBER) next to the name is the ORDER of that persons roll.{Environment.NewLine}  So, Sanare is number 1 on the FOURTH roll, so it shouldnt count. Whitewhich is in second place, but its the 5th roll so, it shouldnt count.",
                Image = "pack://application:,,,/update1.png",
                ImageVisibility = Visibility.Visible
            });

            //UpdateMessages.Add(new UpdateMessageData
            //{
            //    DateTime = new DateTime(2024, 2, 25),
            //    Message =
            //    $"* Fixed slain targets not showing up in timers.{Environment.NewLine}" +
            //    $"* Fixed Faction pull server notice.{Environment.NewLine}" +
            //    $"* Added Random tracker which will show the TOP 5 rolls automatically. Check out the image below for an example.",
            //    Image = "pack://application:,,,/update1.png",
            //    ImageVisibility = Visibility.Visible
            //});
        }
    }
}
