using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Any.Proxy
{
    public class HttpParser
    {
        public enum Methods
        {
            GET = 1,
            POST = 2,
            CONNECT = 3
        }

        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>();
        private readonly string _host = string.Empty;

        private readonly Methods _method = Methods.GET;
        private readonly string _path = String.Empty;
        private readonly int _port;
        private readonly byte[] _source;
        private readonly string _version = "1.1";

        public HttpParser(byte[] source)
        {
            if (source == null || source.Length <= 0) return;
            _source = source;
            string sourceString = Encoding.UTF8.GetString(_source);
            string httpInfo = sourceString.Substring(0, sourceString.IndexOf("\r\n", StringComparison.Ordinal));
            var regMethod = new Regex(@"(?<method>.+)\s+(?<path>.+)\s+HTTP/(?<version>[\d\.]+)",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Match matchMethod = regMethod.Match(httpInfo);
            if (matchMethod.Groups["method"].Value.ToUpper() == "POST")
            {
                _method = Methods.POST;
            }
            else if (matchMethod.Groups["method"].Value.ToUpper() == "CONNECT")
            {
                _method = Methods.CONNECT;
            }
            else
            {
                _method = Methods.GET;
            }
            _path = matchMethod.Groups["path"].Value;
            _version = matchMethod.Groups["version"].Value;
            var regHost = new Regex(@"Host: (((?<host>.+?):(?<port>\d+?))|(?<host>.+?))\s+",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Match matchHost = regHost.Match(sourceString);
            _host = matchHost.Groups["host"].Value;
            if (!int.TryParse(matchHost.Groups["port"].Value, out _port))
            {
                _port = _method == Methods.CONNECT ? 443 : 80;
            }

            // парсим заголовки и заносим их в коллекцию
            var regHeaders = new Regex(@"^(?<key>[^\x3A]+)\:\s{1}(?<value>.+)$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);
            MatchCollection matchHeaders = regHeaders.Matches(sourceString);
            foreach (Match mm in matchHeaders)
            {
                string key = mm.Groups["key"].Value;
                if (!_headers.ContainsKey(key))
                {
                    // если указанного заголовка нет в коллекции, добавляем его
                    _headers.Add(key, mm.Groups["value"].Value.Trim("\r\n ".ToCharArray()));
                }
            }
        }

        public Methods Method
        {
            get { return _method; }
        }

        public string Path
        {
            get { return _path; }
        }

        public byte[] Source
        {
            get { return _source; }
        }

        public string Host
        {
            get { return _host; }
        }

        public int Port
        {
            get { return _port; }
        }

        public string Version
        {
            get { return _version; }
        }

        public Dictionary<string, string> Headers
        {
            get { return _headers; }
        }

        public static bool IsValidQuery(string query)
        {
            int index = query.IndexOf("\r\n\r\n", StringComparison.InvariantCulture);
            if (index == -1)
            {
                return false;
            }

            string httpInfo = query.Substring(0, query.IndexOf("\r\n", StringComparison.Ordinal));
            var regMethod = new Regex(@"(?<method>.+)\s+(?<path>.+)\s+HTTP/(?<version>[\d\.]+)",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Match matchMethod = regMethod.Match(httpInfo);
            if (matchMethod.Groups["method"].Value.ToUpper() == "POST")
            {
                var headers = new Dictionary<string, string>();
                var regHeaders = new Regex(@"^(?<key>[^\x3A]+)\:\s{1}(?<value>.+)$",
                    RegexOptions.Multiline | RegexOptions.IgnoreCase);
                MatchCollection matchHeaders = regHeaders.Matches(query);
                foreach (Match mm in matchHeaders)
                {
                    string key = mm.Groups["key"].Value;
                    if (!headers.ContainsKey(key))
                    {
                        // если указанного заголовка нет в коллекции, добавляем его
                        headers.Add(key, mm.Groups["value"].Value.Trim("\r\n ".ToCharArray()));
                    }
                }
                int length = int.Parse(headers["Content-Length"]);
                return query.Length >= index + 6 + length;
            }

            return true;
        }
    }
}