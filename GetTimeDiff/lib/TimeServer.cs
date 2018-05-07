using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static System.Threading.Thread;

namespace GetTimeDiff.lib
{
    public class TimeServer
    {
        public string Server { get; }
        public bool HasGotTime { get; protected set; }
        private bool _keepRunning;
        public class GotTimeEventArgs : EventArgs
        {
            public DateTime AtomicTime { get; protected set; }
            public DateTime SystemTime { get; protected set; }
            public GotTimeEventArgs(DateTime atomicTime, DateTime sysTime)
            {
                AtomicTime = atomicTime;
                SystemTime = sysTime;
            }
        }
        public event EventHandler<GotTimeEventArgs> GotTime;
        private void RaiseGotTime(DateTime atomicTime, DateTime sysTime)
        {
            HasGotTime = true;
            GotTime?.Invoke(this, new GotTimeEventArgs(atomicTime, sysTime));
        }

        private static readonly string _msSeperator = CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        public TimeServer(string server)
        {
            Server = server;
        }

        public void GetTime(object sender, AtomicTime.TimeRequestedEventArgs args)
        {
            _keepRunning = args.RepeatUntilStopped;
            Task.Factory.StartNew(_getDateTimeFromServer);
        }

        public void Stop()
        {
            _keepRunning = false;
        }

        private void _getDateTimeFromServer()
        {
            do
            {
                try
                {
                    HasGotTime = false;
                    // Connect to the server (at port 13) and get the response
                    string serverResponse;
                    Stopwatch reqTime = new Stopwatch();
                    DateTime thisSysTime = DateTime.UtcNow;
                    reqTime.Start();
                    using (StreamReader reader =
                        new StreamReader(new System.Net.Sockets.TcpClient(Server, 13).GetStream()))
                    {
                        serverResponse = reader.ReadToEnd();
                    }

                    reqTime.Stop();
                    thisSysTime = thisSysTime.AddMilliseconds(reqTime.ElapsedMilliseconds / 2.0d);
                    System.Diagnostics.Debug.WriteLine($"{Server} returned in {reqTime.ElapsedMilliseconds}ms - {serverResponse}");

                    // If a response was received
                    if (!string.IsNullOrEmpty(serverResponse))
                    {
                        // Split the response string ("55596 11-02-14 13:54:11 00 0 0 478.1 UTC(NIST) *")
                        //format is RFC-867, see example here: http://www.kloth.net/software/timesrv1.php
                        //some other examples of how to parse can be found in this: http://cosinekitty.com/nist/
                        string[] tokens = serverResponse.Replace("n", "").Split(' ');

                        // Check the number of tokens
                        if (tokens.Length >= 6)
                        {
                            // Check the health status
                            string health = tokens[5];
                            if (health == "0")
                            {
                                // Get date and time parts from the server response
                                string[] dateParts = tokens[1].Split('-');
                                string[] timeParts = tokens[2].Split(':');

                                // Create a DateTime instance
                                var utcDateTime = new DateTime(
                                    Convert.ToInt32(dateParts[0]) + 2000,
                                    Convert.ToInt32(dateParts[1]), Convert.ToInt32(dateParts[2]),
                                    Convert.ToInt32(timeParts[0]), Convert.ToInt32(timeParts[1]),
                                    Convert.ToInt32(timeParts[2]));

                                //subject milliseconds from it
                                tokens[6] = tokens[6].Replace(".", _msSeperator).Replace(",", _msSeperator);

                                double.TryParse(tokens[6], out var millis);
                                utcDateTime = utcDateTime.AddMilliseconds(-millis);

                                // Convert received (UTC) DateTime value to the local timezone
                                RaiseGotTime(utcDateTime, thisSysTime);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore exception and try the next server
                }

                if (_keepRunning)
                {
                    Sleep(750);
                }
                System.Diagnostics.Debug.WriteLine($"{Server} will{(_keepRunning ? string.Empty : " NOT")} continue");
            } while (_keepRunning);
        }
    }
}
