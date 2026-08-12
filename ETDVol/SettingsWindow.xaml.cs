using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace ETDVol
{
    public partial class SettingsWindow : Window
    {
        private readonly VolumeController _vc = new VolumeController();
        private List<DeviceItem> _deviceItems = new();

        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = SettingsManager.Current;
            LoadDevices();
        }

        private void LoadDevices()
        {
            var devices = _vc.GetAudioDevices();
            _deviceItems = devices.Select(d => new DeviceItem
            {
                Id = d.Id,
                Name = d.Name,
                IsEnabled = SettingsManager.Current.EnabledDevices?.Contains(d.Id) ?? true
            }).ToList();
            DeviceList.ItemsSource = _deviceItems;
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.EnabledDevices = _deviceItems
                .Where(di => di.IsEnabled)
                .Select(di => di.Id)
                .ToList();

            SettingsManager.Save();
            this.Close();
        }

        private void BtnLangTR_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.Language = "tr";
        }

        private void BtnLangEN_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.Language = "en";
        }
    }

    public class DeviceItem : INotifyPropertyChanged
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
