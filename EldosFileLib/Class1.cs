using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolFS;

namespace EldosFileLib
{
    public class EldosTest
    {


        public void GetFiles() {
            
            
            string sRegKey = "C05B5ADAD4C53C9A3F988DBA5FBC119ED39B6FF6F71BC83C88FDAACFDF49665B37A61F3CF2B8C4B7272BDE8639E0F92981A03614C4DBD8CDFABF1C71FE649FF58878C3E7BF6E00DBE4C3752DFF23B299B7D8A5172FA71D4909CF618E48BD6A8F6C7CC8";
            try
            {
                SolFSStorage.SetRegistrationKey(sRegKey);
            }
            catch (Exception e)
            {
                //MessageBox.Show("Error happened while trying to set the license key");
                return;
            }

            //return null;
        }

    }

}
