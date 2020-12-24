using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace transport
{
    public class DistDur
    {
        public DistDur(double distance, double duration)
        {            
            this.distance = distance;
            this.duration = duration;
        }

        public double distance { get; set; }
        public double duration { get; set; }
        
        
    }
}
