namespace TeedyPackage.Services
{
    public static class LogService
    {
        public static void LogError(string errorData)
        {
            string logFile = AppContext.BaseDirectory + "teedy.log";

            FileInfo file = new FileInfo(logFile);
            if (file.Exists)
            {
                if (file.Length > 1024 * 1024)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {

                    }
                }
            }

            try
            {
                File.AppendAllText(logFile, DateTime.Now + ": " + errorData + Environment.NewLine + Environment.NewLine);
            }
            catch
            { }
        }

        public static void LogInfo(string infoData)
        {
            string logFile = AppContext.BaseDirectory + "teedyInfo.log";

            FileInfo file = new FileInfo(logFile);
            if (file.Exists)
            {
                if (file.Length > 1024 * 1024)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {

                    }
                }
            }

            try
            {
                File.AppendAllText(logFile, DateTime.Now + ": " + infoData + Environment.NewLine + Environment.NewLine);
            }
            catch
            { }
        }
    }
}
