using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ETDVol;

public partial class OSDWindow : Window
{
    private DispatcherTimer _timer;
    public event Action? OnOSDClicked;

    public OSDWindow()
    {
        InitializeComponent();
        _timer = new DispatcherTimer();
        _timer.Tick += Timer_Tick;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        CenterOnScreen();
    }

    public void ShowUpdate(string deviceName, int volumePercent)
    {
        DeviceNameText.Text = deviceName;
        VolumeProgress.Value = volumePercent;
        VolumeText.Text = volumePercent.ToString() + "%";

        UpdateLayout();
        CenterOnScreen();

        _timer.Stop();
        _timer.Interval = TimeSpan.FromMilliseconds(SettingsManager.Current.OSDDurationMs);
        _timer.Start();

        if (this.Visibility != Visibility.Visible)
        {
            this.Show();
        }
    }

    private void CenterOnScreen()
    {
        double width = this.ActualWidth > 0 ? this.ActualWidth : 300;
        double height = this.ActualHeight > 0 ? this.ActualHeight : 100;
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double screenHeight = SystemParameters.PrimaryScreenHeight;

        this.Left = (screenWidth - width) / 2;
        this.Top = screenHeight - height - 80;
    }

    private void Border_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            OnOSDClicked?.Invoke();
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _timer.Stop();
        this.Hide();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        this.Hide();
    }
}
