﻿using System;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;

namespace Quandl.Shared.Excel
{
    public class StatusBar : IStatusBar
    {
        private const int RetryWaitTimeMs = 1000;
        private const int MaximumRetries = 10;

        private Application application;

        public StatusBar()
        {
            try
            {
                application = (Application)Marshal.GetActiveObject("Excel.Application");
            }
            catch (COMException)
            {
                application = new Application();
            }
        }

        // Be sure to cleanup any references to excel COM objects that may exist.
        ~StatusBar()
        {
            application = null;
        }

        // Thread the status bar updates to prevent the main application thread from locking waiting to update the status bar.
        public void AddMessage(string msg)
        {
            AddMessageWithoutThreading(msg);
        }

        public void AddException(Exception error)
        {
            AddMessage("⚠ Error : " + error.Message);
        }

        private void AddMessageWithoutThreading(string msg, int retryCount = MaximumRetries)
        {
            // Fail out after maximum retries.
            if (retryCount == 0)
            {
                Utilities.LogToSentry(new Exception("Could not update status bar."), new Dictionary<string, string> { { "Message", msg }, { "Retries", MaximumRetries.ToString() } });
                return;
            }

            try
            {
                application.StatusBar = msg;
            }
            catch (COMException e)
            {
                // Excel is locked atm. Need to wait till its free
                if (e.HResult == -2147417846 || e.HResult == -2146777998)
                {
                    Thread.Sleep(RetryWaitTimeMs);
                    AddMessageWithoutThreading(msg, retryCount - 1);
                    return;
                }
                throw;
            }
            catch (NullReferenceException e)
            {
                Utilities.LogToSentry(e);
            }
        }
    }
}
