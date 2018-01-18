using System;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using log4net;

namespace RatTracker.Infrastructure.Resources.Styles
{
  public class Windows7StyleHack
  {
    private readonly ILog logger;

    public Windows7StyleHack(ILog logger)
    {
      this.logger = logger;
    }

    public void Hack()
    {
      var isWindows7 = Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1;
      var fileName = isWindows7 ? "ComboBoxStyleWin7.xaml" : "ComboBoxStyle.xaml";

      var assembly = Assembly.GetExecutingAssembly();
      using (var stream = assembly.GetManifestResourceStream($"RatTracker.Infrastructure.Resources.Styles.{fileName}"))
      {
        if (stream == null)
        {
          logger.Error("Could not read ComboBox style");
          return;
        }

        var reader = new XamlReader();
        var myResourceDictionary = (ResourceDictionary) reader.LoadAsync(stream);
        Application.Current.Resources.MergedDictionaries.Add(myResourceDictionary);
      }
    }
  }
}