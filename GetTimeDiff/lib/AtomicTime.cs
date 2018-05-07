using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Threading.Thread;

namespace GetTimeDiff.lib
{
    public static class AtomicTime
    {
        public class TimeDiff
        {
            public Dictionary<string, List<double>> TimeDiffsPerServer { get;}
            public List<double> AllTimeDiffs { get; } = new List<double>();

            public TimeDiff(Dictionary<string, List<double>> values)
            {
                TimeDiffsPerServer = values;
                foreach (KeyValuePair<string, List<double>> kvp in TimeDiffsPerServer)
                {
                    AllTimeDiffs.AddRange(kvp.Value);
                }
            }

            public override string ToString()
            {
                return new StringBuilder().Append($"Overall >>\tAVG: {MathPlus.Average(AllTimeDiffs, 0)}")
                    .Append($"\tStDev: {MathPlus.StdDev(AllTimeDiffs, 2)} ")
                    .Append($"\tMin: {AllTimeDiffs.Min()} ")
                    .Append($"\tMax: {AllTimeDiffs.Max()} ")
                    .Append($"\tSamples: {AllTimeDiffs.Count()} ").ToString();
            }

            public string Report()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($" {"Server",30}")
                    .Append($"\t{"AVG",10}")
                    .Append($"\t{"StdDev",10}")
                    .Append($"\t{"Min", 10}")
                    .Append($"\t{"Max",10}")
                    .Append($"\t{"Count",10}")
                    .AppendLine();
                foreach (KeyValuePair<string, List<double>> kvp in TimeDiffsPerServer)
                {
                    sb.Append($" {kvp.Key,30}")
                        .Append($"\t{MathPlus.Average(kvp.Value, 0),10}")
                        .Append($"\t{MathPlus.StdDev(kvp.Value, 2),10}")
                        .Append($"\t{kvp.Value.Min(),10}")
                        .Append($"\t{kvp.Value.Max(),10}")
                        .Append($"\t{kvp.Value.Count,10}")
                        .AppendLine();
                }
                sb.Append($" {"OVERALL",30}")
                    .Append($"\t{MathPlus.Average(AllTimeDiffs, 0),10}")
                    .Append($"\t{MathPlus.StdDev(AllTimeDiffs, 2),10}")
                    .Append($"\t{AllTimeDiffs.Min(),10}")
                    .Append($"\t{AllTimeDiffs.Max(),10}")
                    .Append($"\t{AllTimeDiffs.Count,10}")
                    .AppendLine();
                return sb.ToString();
            }
        }
        public class HaveTimeDiffReportEventArgs : EventArgs
        {
            public TimeDiff Report { get; protected set; }
            public HaveTimeDiffReportEventArgs(TimeDiff val)
            {
                Report = val;
            }
        }
        public static event EventHandler<HaveTimeDiffReportEventArgs> HaveTimeDiffReport;
        private static void RaiseHaveTimeDiffReport(TimeDiff report)
        {
            HaveTimeDiffReport?.Invoke(new object(), new HaveTimeDiffReportEventArgs(report));
        }

        public class TimeRequestedEventArgs : EventArgs
        {
            public bool RepeatUntilStopped { get; protected set; }
            public TimeRequestedEventArgs(bool val)
            {
                RepeatUntilStopped = val;
            }
        }
        public static event EventHandler<TimeRequestedEventArgs> TimeRequested;
        private static void RaiseTimeRequested(bool repeat = false)
        {
            TimeDiffs.Clear();
            TimeRequested?.Invoke(new object(), new TimeRequestedEventArgs(repeat));
        }

        private static DateTime? _now;
        public static DateTime Now
        {
            get
            {
                RaiseTimeRequested();
                return WaitForTime();
            }
        }

        private static readonly List<string> _serverUris = new List<string>()
        {
            "time-a-g.nist.gov",
            "time-b-g.nist.gov",
            "time-c-g.nist.gov",
            "time-d-g.nist.gov",
            "time-d-g.nist.gov",
            "time-a-wwv.nist.gov",
            "time-b-wwv.nist.gov",
            "time-c-wwv.nist.gov",
            "time-d-wwv.nist.gov",
            "time-d-wwv.nist.gov",
            "time-a-b.nist.gov",
            "time-b-b.nist.gov",
            "time-c-b.nist.gov",
            "time-d-b.nist.gov",
            "time-d-b.nist.gov",
            "time.nist.gov",
            "utcnist.colorado.edu",
            "utcnist2.colorado.edu"
        };
        private static readonly List<TimeServer> _servers = new List<TimeServer>();
        public static readonly Dictionary<string, List<double>> TimeDiffs = new Dictionary<string, List<double>>();
        private const int WaitMilliSeconds = 3000;
        private static int _sampleTimeInSeconds = 300;

        static AtomicTime()
        {
            foreach (string s in _serverUris)
            {
                TimeServer newTs = new TimeServer(s);
                newTs.GotTime += HandleGotTime;
                TimeRequested += newTs.GetTime;
                _servers.Add(newTs);
            }
        }

        private static void HandleGotTime(object sender, TimeServer.GotTimeEventArgs args)
        {
            Task.Factory.StartNew(() =>
            {
                TimeServer ts = (TimeServer)sender;
                _now = args.AtomicTime;
                if (!TimeDiffs.ContainsKey(ts.Server))
                {
                    TimeDiffs.Add(ts.Server, new List<double>());
                }

                TimeSpan span = args.AtomicTime - args.SystemTime;
                TimeDiffs[ts.Server].Add(span.TotalMilliseconds);
            });
        }

        private static DateTime WaitForTime()
        {
            DateTime endTime = DateTime.UtcNow.AddMilliseconds(WaitMilliSeconds);
            while (_now == null
                   && DateTime.UtcNow < endTime)
            {
                // Do nothing, just waiting for the thing to finish
            }

            return _now ?? DateTime.MinValue;
        }

        public static void GetTimeDiff(int sampleTimeInSeconds)
        {
            _sampleTimeInSeconds = sampleTimeInSeconds;
            Task.Factory.StartNew(WaitForReport);
        }

        private static void WaitForReport()
        {
            RaiseTimeRequested(true);
            Sleep(_sampleTimeInSeconds * 1000);
            foreach (TimeServer ts in _servers)
            {
                ts.Stop();
            }

            RaiseHaveTimeDiffReport(new TimeDiff(TimeDiffs));
        }
    }
}
