using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EPriceViewer
{
    public static class Log
    {
        private const string LogFileName = "log.txt";
        private static object _lockObj = new object();

        public static bool WriteToLog(string sender, string message)
        {
            var isLocked = false;
            var isComplete = false;
            var assemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                try
                {
                    Monitor.Enter(_lockObj, ref isLocked);
                    var messageLog = DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss") + " " + sender + " " + message;
                    var path = Path.Combine(assemblyLocation, LogFileName);
                    var fileMode = FileMode.Append;
                    if (!File.Exists(path))
                    {
                        fileMode = FileMode.CreateNew;
                    }
                    using (var stream = File.Open(path, fileMode, FileAccess.Write))
                    {
                        var writer = new StreamWriter(stream);
                        writer.WriteLine(messageLog);
                        writer.Flush();
                        stream.Flush();
                        isComplete = true;
                    }
                }
                catch
                {
                    isComplete = false;
                }
                finally
                {
                    if (isLocked) Monitor.Exit(_lockObj);
                }
                
            }
            return isComplete;
        }
    }
}
