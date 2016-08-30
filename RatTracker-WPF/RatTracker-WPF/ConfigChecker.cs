using Microsoft.ApplicationInsights;
using RatTracker_WPF.Properties;
using System;
using System.IO;
using System.Windows;
using System.Xml.Linq;
using log4net;
using System.Xml;

namespace RatTracker_WPF
{
	public class ConfigChecker
    {
		private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private readonly string edProductDir= Settings.Default.EDPath + "\\Products";
        private readonly TelemetryClient tc = new TelemetryClient();

        public bool ParseEdAppConfig()
        {
            if (!Directory.Exists(edProductDir))
            {
                logger.Fatal("Couldn't find E:D product directory, aborting AppConfig parse.");
                return false;
            }

            foreach (string dir in Directory.GetDirectories(edProductDir))
            {
                logger.Info("Checking AppConfig in Product directory " + dir);
                try
                {
                    logger.Debug("Loading " + dir + @"\AppConfig.xml");
                    XDocument appconf = XDocument.Load(dir + @"\AppConfig.xml");
                    XElement networknode = appconf.Element("AppConfig").Element("Network");
                    if (networknode.Attribute("VerboseLogging") == null)
                    {
                        // Nothing is set up! This makes testing the attributes difficult, so initialize VerboseLogging at least.
                        networknode.SetAttributeValue("VerboseLogging", 0);
                        logger.Info("No VerboseLogging configuration at all. Setting temporarily for testing.");
                    }

                    if (networknode.Attribute("VerboseLogging").Value != "1" || networknode.Attribute("ReportSentLetters") == null ||
                        networknode.Attribute("ReportReceivedLetters") == null)
                    {
                        logger.Error("WARNING: Your Elite:Dangerous AppConfig is not set up correctly to allow RatTracker to work!");
                        MessageBoxResult result =
                            MessageBox.Show(
                                "Warning: Your AppConfig in " + dir +
                                " is not configured correctly to allow RatTracker to perform its function. Would you like to alter the configuration to enable Verbose Logging? Your old AppConfig will be backed up.",
                                "Incorrect AppConfig", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                        tc.TrackEvent("AppConfigNotCorrectlySetUp");
                        switch (result)
                        {
                            case MessageBoxResult.Yes:
                                File.Copy(dir + @"\AppConfig.xml", dir + @"\AppConfig-BeforeRatTracker.xml", true);

                                networknode.SetAttributeValue("VerboseLogging", "1");
                                networknode.SetAttributeValue("ReportSentLetters", 1);
                                networknode.SetAttributeValue("ReportReceivedLetters", 1);
		                        XmlWriterSettings settings = new XmlWriterSettings
		                        {
			                        OmitXmlDeclaration = true,
			                        Indent = true,
			                        NewLineOnAttributes = true
		                        };
		                        using (XmlWriter xw = XmlWriter.Create(dir + @"\AppConfig.xml", settings))
		                        {
			                        appconf.Save(xw);
		                        }

                                logger.Info("Wrote new configuration to " + dir + @"\AppConfig.xml");
                                tc.TrackEvent("AppConfigAutofixed");
                                return true;
                            case MessageBoxResult.No:
                                logger.Info("No alterations performed.");
                                tc.TrackEvent("AppConfigDenied");
                                return false;
                        }

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    logger.Fatal("Exception in AppConfigReader!", ex);
                    return false;
                }
            }

            return true;
        }

    }
}
