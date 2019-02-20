using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIP.Helper
{
     class  XmlFactoryHelper
    {
        protected string getTreeNumRandom()
        {
            Random ro = new Random();
            int iResult;
            int iUp = 999;
            int iDown = 100;
            iResult = ro.Next(iDown, iUp);
            //Response.Write(iResult.ToString());
            return iResult.ToString().Trim();
        }
    }
}
