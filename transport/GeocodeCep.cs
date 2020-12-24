using System;
using System.Data;
using System.Text.RegularExpressions;

namespace transport
{
    class GeocodeCep
    {
        private string latitude, longitude;
        string cep;
        public GeocodeCep(string str)
        {
            cep = seperateCep(str);
            if (cep != null)
            {
                DBConnection connection = new DBConnection();
                DataTable dt = connection.readByAdapter("select case when latitude is null then -1000 else latitude end latitude,case when longitude is null then -1000 else longitude end longitude from kacep where cep = :cep", new string[] { ":cep" }, new string[] { this.cep });
                if (dt.Rows.Count > 0)
                {
                    latitude = Convert.ToString(dt.Rows[0]["latitude"]);
                    longitude = Convert.ToString(dt.Rows[0]["longitude"]);
                }
                else
                {
                    latitude = "-1000";
                    longitude = "-1000";
                }
            }
            else
            {
                latitude = "-1000";
                longitude = "-1000";
            }
        }
        private string seperateCep(string str)
        {
            if (str == null)
            {
                throw new Exception("Adress cant be null");
            }
            Regex re = new Regex(@"(@|@\s*CEP|CEP)\s*(:|.|-|=|\s*)\s*(\d{8}|\d{5}(-|.)\s*\d{3}|\d{2}(-|.)\s*\d{3}\s*(-|.)\s*\d{3})", RegexOptions.IgnoreCase);
            Regex reNumbers = new Regex(@"[^\d]");
            MatchCollection matchs = re.Matches(str);
            if (matchs.Count > 0)
            {
                GroupCollection groupCollection = matchs[0].Groups;
                return reNumbers.Replace(groupCollection[0].Value, "");
            }
            return null;
        }


        public string getLatitude()
        {
            return this.latitude;
        }

        public string getLongitude()
        {
            return this.longitude;
        }
    }
}
