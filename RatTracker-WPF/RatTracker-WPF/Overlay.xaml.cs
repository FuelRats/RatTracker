using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using RatTracker_WPF.Models.App;

namespace RatTracker_WPF
{
    /// <summary>
    /// Interaction logic for Overlay.xaml
    /// </summary>
    public partial class Overlay : Window
    {
        public Overlay()
        {
            InitializeComponent();
        }
        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
        }
        public void Queue_Message(OverlayMessage message, int time)
        {
            InfoLine1Header.Content = message.line1header;
            InfoLine1Body.Content = message.line1content;
            InfoLine2Header.Content = message.line2header;
            InfoLine2Body.Content = message.line2content;
            InfoLine3Header.Content = message.line3header;
            InfoLine3Body.Content = message.line3content;
            InfoLine4Header.Content = message.line4header;
            InfoLine4Body.Content = message.line4content;

        }
    }
}
