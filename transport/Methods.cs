using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace transport
{
    class Methods
    {
        public static string loginType="";
        public static int intParser(string number)
        {            
            try
            {
                return int.Parse(number);
            }catch(Exception ex)
            {
                return 0;
            }
        }


        public static bool IsAllDigits(string s)
        {
            foreach (char c in s)
            {
                if (!char.IsDigit(c))
                    return false;
            }
            return true;
        }


        public static bool isURLExist(string url)
        {
            try
            {
                WebRequest req = WebRequest.Create(url);

                WebResponse res = req.GetResponse();

                return true;
            }
            catch (WebException ex)
            {
                return false;
            }
        }
    }
}
