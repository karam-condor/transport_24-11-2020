using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows;
using System.Net;
using Microsoft.Maps.MapControl.WPF;

namespace transport
{
    class MapsRequest
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string bingKey = "WfxFWb3dXmy4JnWw1kpG~FX_KaKXZ4wVgHduN5YpQPA~AjA8yf6Z3F4887_kgWPSipd2CFpPUlcyQRROzDqa2bPx0iK9T3BqHL0odYJ80aMD";


        public static string seperateCep(string adress)
        {
            if (adress == null)
            {
                throw new Exception("Adress cant be null");
            }            
            Regex re1 = new Regex(@"@CEP[0-9]{5}-[0-9]{3}|@CEP[0-9]{8}|CEP[0-9]{5}-[0-9]{3}|CEP[0-9]{8}|@CEP\s*[0-9]{5}-[0-9]{3}|@CEP\s*[0-9]{8}|CEP\s*[0-9]{5}-[0-9]{3}|CEP\s*[0-9]{8}", RegexOptions.IgnoreCase);
            Regex re2 = new Regex(@"@CEP[0-9]{5}|CEP[0-9]{5}|@CEP\s*[0-9]{5}|CEP\s*[0-9]{5}", RegexOptions.IgnoreCase);
            MatchCollection matchs1 = re1.Matches(adress);
            MatchCollection matchs2 = re2.Matches(adress);
            if (matchs1.Count > 0)
            {
                GroupCollection groupCollection = matchs1[0].Groups;
                return splitNumbers(groupCollection[0].Value);
                
            }
            if (matchs2.Count > 0)
            {
                GroupCollection groupCollection = matchs2[0].Groups;
                return addZeros(splitNumbers(groupCollection[0].Value));
            }
            return null;
        }

        private static string addZeros(string str)
        {
            if(str.Length < 8)
            {
                for (int i = 0; i < 8-str.Length; i++)
                {
                    str += "0";
                }
                return str;
            }
            else
            {
                return str;
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



       


        public static DistDur[,] generateDistanceMatrix(Pedido startLocation,Pedido[] locs)
        {
            int size = locs.Count()+1,indexOregin=0,indexDestination=0;
            if (size > 50)
            {                
                return null;
            }            
            DistDur[,] distanceMatrix = new DistDur[size, size];
            string originsDestionations =$"{startLocation.X},{startLocation.Y};" + string.Join(";", locs.AsEnumerable().Select(x => $"{x.X},{x.Y}").ToArray());
            string decodedUrl = "https://dev.virtualearth.net/REST/v1/Routes/DistanceMatrix?origins="+originsDestionations+"&destinations="+originsDestionations+"&travelMode=driving&key=WfxFWb3dXmy4JnWw1kpG~FX_KaKXZ4wVgHduN5YpQPA~AjA8yf6Z3F4887_kgWPSipd2CFpPUlcyQRROzDqa2bPx0iK9T3BqHL0odYJ80aMD";            
            try
            {
                string responseBody = new WebClient().DownloadString(decodedUrl);
                JObject ob = JObject.Parse(responseBody);                
                int statusCode = (int)ob.SelectToken("statusCode");
                if(statusCode == 200)
                {                
                    JArray resultJArray = JArray.Parse(Convert.ToString(ob.SelectToken("resourceSets[0].resources[0].results")));
                    for (int i = 0; i < resultJArray.Count(); i++)
                    {
                        indexOregin = (int) resultJArray[i]["originIndex"];
                        indexDestination = (int)resultJArray[i]["destinationIndex"];                        
                        distanceMatrix[indexOregin,indexDestination] = new DistDur(Methods.doubleParser(Convert.ToString(resultJArray[i]["travelDistance"])),Methods.doubleParser(Convert.ToString(resultJArray[i]["travelDuration"])));
                    }
                }
                return distanceMatrix;
            }
            catch(Exception ex)
            {
                return null;
            }
        }


        public static JArray generateRoute(Pedido startLocation, Pedido[] locs, string startingTime, string numcar)
        {
            try
            {
                JArray arr = new JArray();
                List<string> wpList;
                StringBuilder sb = new StringBuilder();
                int totalCount = (int)Math.Ceiling((double)(locs.Length) / (double)25);
                string decodedUrl = "", responseBody = "";
                JObject ob;
                int statusCode, c1 = 0, c2 = locs.Length, innerCounter = 0;
                if (totalCount > 1)
                {
                    c1 = 0;
                    c2 = 24;
                }
                string firstLocStr = $"wp.0={startLocation.X},{startLocation.Y}&";
                for (int i = 1; i <= totalCount; i++)
                {
                    wpList = new List<string>();
                    sb = new StringBuilder();
                    if (i == 1)
                    {
                        sb.Append(firstLocStr);
                    }
                    innerCounter = 0;
                    for (int j = c1; j < c2; j++)
                    {
                        Console.WriteLine("j: " + j);
                        wpList.Add($"wp.{innerCounter + 1}={locs[j].X},{locs[j].Y}");                        
                        innerCounter++;
                    }
                    sb.Append(string.Join("&", wpList));
                    //if (i == totalCount)
                    //{
                    //    sb.Append($"wp.{c2}={startLocation.X},{startLocation.Y}");
                    //}
                    decodedUrl = "https://dev.virtualearth.net/REST/v1/Routes?" + sb.ToString() + "&optimize=distance&avoid=highways&routeAttributes=routePath&dateTime=" + startingTime + "&maxSolutions=1&travelMode=Driving&c=pt-BR&key=" + bingKey;
                    Console.WriteLine(decodedUrl);
                    responseBody = Methods.encodeUTF8(new WebClient().DownloadString(decodedUrl));
                    ob = JObject.Parse(responseBody);
                    statusCode = (int)ob.SelectToken("statusCode");
                    if (statusCode == 200)
                    {
                        arr.Add(ob);
                    }

                    else
                    {
                        return null;
                    }
                    if (i == 1)
                    {
                        c1 += 24;
                    }
                    else
                    {
                        c1 += 25;
                    }
                    if ((c2 + 25) > locs.Count())
                    {
                        c2 = locs.Length;
                    }
                    else
                    {
                        c2 += 25;
                    }
                }
                try
                {
                    System.IO.File.WriteAllText($"json/map_data/{numcar}.json", arr.ToString());
                }
                catch
                {
                    return null;
                }
                return arr;
            }
            catch
            {
                return null;
            }
        }

        private static string takeWhiteSpacesUrl(string url)
        {
            return Uri.EscapeDataString(url);
        }                
    }
}
