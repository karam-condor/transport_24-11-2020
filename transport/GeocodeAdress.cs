using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace transport
{
    class GeocodeAdress
    {
        private string latitude, longitude;
        string cep, adress;
        DataTable dt;
        public GeocodeAdress(string cep, string adress,string codcli)
        {
            this.cep = splitNumbers(cep);
            this.adress = adress;
            if (cep != null && cep != string.Empty)
            {
                DBConnection connection = new DBConnection();
                dt = connection.readByAdapter("select case when latitude is null then -1000 else latitude end latitude,case when longitude is null then -1000 else longitude end longitude from kacep where cep = :cep", new string[] { ":cep" }, new string[] { this.cep });
                if (dt.Rows.Count > 0)
                {
                    latitude = Convert.ToString(dt.Rows[0]["latitude"]);
                    longitude = Convert.ToString(dt.Rows[0]["longitude"]);
                    Console.WriteLine($"{codcli}  by cep first");
                }
                else
                {
                    dt = connection.readByAdapter("select case when latitude is null then -1000 else latitude end latitude,case when longitude is null then -1000 else longitude end longitude from kacep where cep in (select cep from kacnpj where cnpj in ( select REGEXP_REPLACE(a.cgcent,'[^0-9]') cnpj from pcclient@wint a where a.codcli = :codcli))", new string[] { ":codcli" }, new string[] { codcli });
                    if (dt.Rows.Count > 0)
                    {
                        latitude = Convert.ToString(dt.Rows[0]["latitude"]);
                        longitude = Convert.ToString(dt.Rows[0]["longitude"]);
                        Console.WriteLine($"{codcli}  by cep second");
                    }
                    else
                    {
                        if (adress != null && adress != string.Empty)
                        {
                            callGoogleMap();
                            Console.WriteLine($"{codcli}  by map1");
                        }
                        else
                        {
                            callGoogleMap();
                            Console.WriteLine($"{codcli}  by map2");
                        }
                    }                    
                }
            }
            else
            {
                if (adress != null && adress != string.Empty)
                {
                    callGoogleMap();
                    Console.WriteLine($"{codcli}  by map3");
                }
                else
                {
                    callGoogleMap();
                    Console.WriteLine($"{codcli}  by map4");
                }
            }
        }

        private static string splitNumbers(string str)
        {
            string b = string.Empty;
            int val;

            for (int i = 0; i < str.Length; i++)
            {
                if (Char.IsDigit(str[i]))
                    b += str[i];
            }
            return b;
        }

        public string getLatitude()
        {
            return this.latitude;
        }

        public string getLongitude()
        {
            return this.longitude;
        }

        private void callBingMap()
        {
            string link = takeWhiteSpacesUrl("http://dev.virtualearth.net/REST/v1/Locations?CountryRegion=BR&q=" + adress + "&key=WfxFWb3dXmy4JnWw1kpG~FX_KaKXZ4wVgHduN5YpQPA~AjA8yf6Z3F4887_kgWPSipd2CFpPUlcyQRROzDqa2bPx0iK9T3BqHL0odYJ80aMD");
            Console.WriteLine(link);
            try
            {
                string jsonStr = new System.Net.WebClient().DownloadString(link);
                JObject obj = JObject.Parse(jsonStr);
                if (Convert.ToString(obj.SelectToken("statusCode")) == "200")
                {
                    latitude = Convert.ToString(obj.SelectToken("resourceSets[0].resources[0].point.coordinates[0]"));
                    longitude = Convert.ToString(obj.SelectToken("resourceSets[0].resources[0].point.coordinates[1]"));
                }
                else
                {
                    latitude = "-1000";
                    longitude = "-1000";
                }
            }
            catch
            {
                latitude = "-1000";
                longitude = "-1000";
            }
        }



        private void callGoogleMap()
        {
            string link = takeWhiteSpacesUrl("https://maps.googleapis.com/maps/api/geocode/json?address=" + adress + "&key=AIzaSyCMdqUXkGNkpiU0bVc6flJ2QKyDY9FiyyY");
            Console.WriteLine(link);
            try
            {
                string jsonStr = new System.Net.WebClient().DownloadString(link);
                JObject obj = JObject.Parse(jsonStr);
                if (Convert.ToString(obj.SelectToken("status")) == "OK")
                {
                    latitude = Convert.ToString(obj.SelectToken("results[0].geometry.location.lat"));
                    longitude = Convert.ToString(obj.SelectToken("results[0].geometry.location.lng"));
                }
                else
                {
                    latitude = "-1000";
                    longitude = "-1000";
                }
            }
            catch
            {
                latitude = "-1000";
                longitude = "-1000";
            }
        }


        public static string takeWhiteSpacesUrl(string url)
        {
            return url.Replace(" ", "%20");
        }
    }
}

