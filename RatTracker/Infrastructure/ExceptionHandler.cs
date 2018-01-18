using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using log4net;

namespace RatTracker.Infrastructure
{
  public class ExceptionHandler
  {
    private readonly ILog log;

    public ExceptionHandler(ILog log)
    {
      this.log = log;
      AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
      TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
      Application.Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
    }

    private void CurrentOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs dispatcherUnhandledExceptionEventArgs)
    {
      LogException(dispatcherUnhandledExceptionEventArgs.Exception);
      dispatcherUnhandledExceptionEventArgs.Handled = true;
    }

    private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
    {
      LogException(unobservedTaskExceptionEventArgs.Exception);
    }

    private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
    {
      LogException(unhandledExceptionEventArgs.ExceptionObject as Exception);
    }

    private void LogException(Exception exception)
    {
      log.Fatal("Error in application: ", exception);
    }
  }
}