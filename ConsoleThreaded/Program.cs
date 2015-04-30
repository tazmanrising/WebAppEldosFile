using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleThreaded
{
    class Program
    {
        static void Main(string[] args)
        {


            //var filePatterns = new List<FilePattern>();


        }
    }

    [DataContract]
    public class Path
    {
        
        public int Order { get; set; }
       
        public string PathLocation { get; set; }

    }

    [DataContract]
    public class FilePattern
    {

         
        public string Key { get; set; }
        public List<PathLocation> Paths { get; set; }
    }





}
