using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetTimeDiff.lib
{
    public static class MathPlus
    {
        public static double Average(List<double> values, int? roundTo = null)
        {
            return roundTo == null ? values.Average() : Math.Round(values.Average(), (int)roundTo);
        }

        public static double StdDev(List<double> values, int? roundTo = null)
        {
            double result = 0;
            if (values.Any())
            {
                double avg = Average(values);
                //Perform the Sum of (value-avg)_2_2      
                double sum = values.Sum(d => Math.Pow(d - avg, 2));
                //Put it all together      
                result = Math.Sqrt(sum / (values.Count - 1));
            }
            return roundTo == null ? result : Math.Round(result, (int)roundTo);
        }
    }
}
