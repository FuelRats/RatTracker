﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Animation;
using RatTracker_WPF.Models.App;

namespace RatTracker_WPF
{
  /// <summary>
  ///   Interaction logic for Overlay.xaml
  /// </summary>
  public partial class Overlay : Window, INotifyPropertyChanged
  {
    private RescueInfo rescueInfo;

    public Overlay()
    {
      InitializeComponent();
    }

    public bool IsRescueActive => RescueInfo != null;

    public RescueInfo RescueInfo
    {
      get => rescueInfo;
      set
      {
        rescueInfo = value;
        // ReSharper disable once ExplicitCallerInfoArgument
        NotifyPropertyChanged(nameof(IsRescueActive));
        NotifyPropertyChanged();
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    //TODO: Actually make a queue facility that allows more than one message to
    // be added asynchronously. Pop messages off the stack when one message has
    // finished displaying.
    public void Queue_Message(OverlayMessage message, int time)
    {
      InfoLine1Header.Content = message.Line1Header;
      InfoLine1Body.Content = message.Line1Content;
      InfoLine2Header.Content = message.Line2Header;
      InfoLine2Body.Content = message.Line2Content;
      InfoLine3Header.Content = message.Line3Header;
      InfoLine3Body.Content = message.Line3Content;
      InfoLine4Header.Content = message.Line4Header;
      InfoLine4Body.Content = message.Line4Content;
      // Fairly certain this is 3 lines in XAML, but I can't get the fucker to work, damnit.
      var sb = new Storyboard();
      sb.Duration = TimeSpan.FromSeconds(time);
      var fadein = new DoubleAnimation
      {
        To = 1,
        BeginTime = TimeSpan.FromSeconds(0),
        Duration = TimeSpan.FromSeconds(2),
        FillBehavior = FillBehavior.HoldEnd
      };
      var fadeout = new DoubleAnimation
      {
        To = 0,
        BeginTime = TimeSpan.FromSeconds(time - 2),
        Duration = TimeSpan.FromSeconds(2),
        FillBehavior = FillBehavior.HoldEnd
      };
      sb.Children.Add(fadein);
      sb.Children.Add(fadeout);
      Storyboard.SetTarget(fadein, MessageGrid);
      Storyboard.SetTarget(fadeout, MessageGrid);
      Storyboard.SetTargetProperty(fadein, new PropertyPath(OpacityProperty));
      Storyboard.SetTargetProperty(fadeout, new PropertyPath(OpacityProperty));
      sb.Begin();
    }

    public void SetCurrentClient(RescueInfo client)
    {
      RescueInfo = client;
      // TODO: Link the panels and button visibility properties in WPF instead.
      if (client.Rat2.Rat == null)
      {
        Rat2Panel.Visibility = Visibility.Hidden;
        Rat2Buttons.Visibility = Visibility.Hidden;
      }
      if (client.Rat3.Rat == null)
      {
        Rat3Panel.Visibility = Visibility.Hidden;
        Rat3Buttons.Visibility = Visibility.Hidden;
      }
    }

    protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
      var onPropertyChanged = PropertyChanged;
      onPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
      var window = (Window) sender;
      window.Topmost = true;
    }
  }
}