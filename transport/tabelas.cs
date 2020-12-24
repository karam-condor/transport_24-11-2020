using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace transport
{
    public static class tabelas
    {
        public static bool mode = false;//false teste | true produção
        public static string userTable { get; }
        public  static string cargaTable { get; }
        public static string processTable { get; }
        public static string prodTable { get; }
        public static string geoTable { get; }
        public static string seqIdGeo { get; }
        public static string imgUrl { get; }
        public static string audioUrl { get; }

        static tabelas()
        {
            switch (mode)
            {
                case true:
                    userTable = "logtransusu";
                    cargaTable = "logtranscar";
                    processTable = "logtransprocess";
                    prodTable = "logtransprod";
                    geoTable = "logtransgeo";
                    seqIdGeo = "seq_id_geo";
                    imgUrl = "http://192.168.0.203/transporte/Images/";
                    audioUrl = "http://192.168.0.203/transporte/records/";
                    break;
                case false:
                    userTable = "logtransusuts";
                    cargaTable = "logtranscarts";
                    processTable = "logtransprocessts";
                    prodTable = "logtransprodts";
                    geoTable = "logtransgeots";
                    seqIdGeo = "seq_id_geots";
                    imgUrl = "http://192.168.0.203/transporte/Images_test/";
                    audioUrl = "http://192.168.0.203/transporte/records_test/";
                    break;
            }            
        }
    }
}