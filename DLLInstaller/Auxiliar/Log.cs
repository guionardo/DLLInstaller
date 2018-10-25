using System;
using System.IO;

namespace DLLInstaller.Auxiliar
{
    public class Log
    {
        static readonly string logFile = Path.Combine(Directory.GetCurrentDirectory(), "DLLInstaller." + DateTime.Now.ToString("yyyyMMdd") + ".log");
        static bool logEnabled = true;

        public static void Add(string msg)
        {
            if (!logEnabled)
                return;
            try
            {
                File.AppendAllText(logFile, DateTime.Now.ToString("HHmmss") + " " + msg + "\r\n");
            }
            catch (Exception e)
            {
                Dialogs.Error("Erro ao registrar log: " + e.Message);
                logEnabled = false;
            }
        }
    }
}
