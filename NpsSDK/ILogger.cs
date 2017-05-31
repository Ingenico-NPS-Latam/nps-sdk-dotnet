using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace NpsSDK
{
    public enum LogLevel
    {
        Debug = 1,
        Info = 2
    }

    public interface ILogger
    {
        void Log(String message);
    }

    public class ConsoleLogger : ILogger
    {
        public void Log(string message) { Console.WriteLine(message); }
    }

    public class DebugLogger : ILogger
    {
        public void Log(string message) { Debug.WriteLine(message); }
    }

    public class FileLogger : ILogger
    {
        private readonly String _filePath;
        public FileLogger(String filePath) { _filePath = filePath; }
        public void Log(string message) { System.IO.File.AppendAllText(_filePath, message); }
    }

    internal class LogWrapper
    {
        private readonly LogLevel _minimumLevel;
        private readonly ILogger _logger;

        public LogWrapper(LogLevel minimumLevel, ILogger logger)
        {
            _logger = logger;
            _minimumLevel = minimumLevel;
        }

        private static readonly Regex[] SanitizerRegularExpressions = {
                new Regex("(<psp_CardExpDate[^>]*>)(.*)(</psp_CardExpDate>)", RegexOptions.IgnoreCase | RegexOptions.Multiline),
                new Regex("(<psp_CardSecurityCode[^>]*>)(.*)(</psp_CardSecurityCode>)", RegexOptions.IgnoreCase | RegexOptions.Multiline),
                new Regex("(<psp_CardNumber[^>]*>.{6})(.*)(.{4}</psp_CardNumber>)", RegexOptions.IgnoreCase | RegexOptions.Multiline),
                new Regex("(<Number[^>]*>.{6})(.*)(.{4}</Number>)", RegexOptions.IgnoreCase | RegexOptions.Multiline),
                new Regex("(<ExpirationDate[^>]*>)(.*)(</ExpirationDate>)", RegexOptions.IgnoreCase | RegexOptions.Multiline),
                new Regex("(<SecurityCode[^>]*>)(.*)(</SecurityCode>)", RegexOptions.IgnoreCase | RegexOptions.Multiline)
        };

        public void Log(LogLevel logLevel, string message)
        {
            if (_logger == null || logLevel < _minimumLevel) { return; }

            _logger.Log(message);
        }

        public void LogRequest(LogLevel logLevel, XmlDocument request)
        {
            if (_logger == null || logLevel < _minimumLevel) { return; }

            String message = Beautify(request);
            if (_minimumLevel > LogLevel.Debug)
            {
                foreach (Regex regex in SanitizerRegularExpressions)
                {
                    foreach (Match match in regex.Matches(message))
                    {
                        message = message.Replace(match.Value, String.Format("{0}{1}{2}", match.Groups[1], String.Empty.PadLeft(match.Groups[2].Length, '*'), match.Groups[3]));
                    }
                }
            }
            Log(logLevel, "Request:" + Environment.NewLine + message);
        }

        public void LogResponse(LogLevel logLevel, XmlDocument response)
        {
            Log(logLevel, "Response:" + Environment.NewLine + Beautify(response));
        }

        private static string Beautify(XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }
            return sb.ToString();
        }
    }
}
