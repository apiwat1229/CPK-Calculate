using System.Collections.Generic;

namespace CPK_Calculate
{
    public class CPKResultData
    {
        public List<double> Values { get; set; } = new();
        public double LSL { get; set; }
        public double USL { get; set; }
        public int SubgroupSize { get; set; }
        public CPKResults Results { get; set; } = new();
        public string Title { get; set; } = "";
        public string Date { get; set; } = "";
    }
}
