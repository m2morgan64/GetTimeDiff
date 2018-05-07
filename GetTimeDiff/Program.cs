using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using GetTimeDiff.lib;

namespace GetTimeDiff
{
    class Program
    {
        private static bool _printingReport;
        private static bool _haveReport;
        static void Main(string[] args)
        {
            lib.AtomicTime.HaveTimeDiffReport += ReportTimeDiffResults;
            int timeLeft = 300;
            AtomicTime.GetTimeDiff(timeLeft);
            // Give it a second or two finish.
            timeLeft += 2;
            Console.Write($"\t{DateTime.Now}\tGathering data. This will take {timeLeft} Seconds");
            while (!_haveReport && timeLeft > 0)
            {
                if (!_printingReport)
                {
                    Console.CursorLeft = 0;
                    Console.Write($"\t{DateTime.Now}\tGathering data. This will take {timeLeft} Seconds   ");
                }
                timeLeft--;
                Thread.Sleep(1000);
            }
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Press any key to close.");
            Console.ReadKey();
        }

        private static void ReportTimeDiffResults(object sender, AtomicTime.HaveTimeDiffReportEventArgs e)
        {
            _printingReport = true;
            Console.CursorLeft = 0;
            Console.WriteLine($"\t{DateTime.Now}\tReport Complete:\t\t\t\t\t");
            Console.WriteLine();
            Console.WriteLine(e.Report.Report());
            _haveReport = true;
        }
    }
}
