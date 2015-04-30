using EldosFileLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebAppEldos.Models;
using System.IO;
using System.Text;

namespace WebAppEldos.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {

            using (var s = new EldosFileSystem(@"C:\eldosStorage\desktop.st"))
            {
                var dir = @"C:\Upload";

                var files = new List<string>();

                files.Add(@"Fiserv.pdf");

                s.AddFiles(dir,files);
                var timerLogs = new StringBuilder();
                if (s.Logs != null)
                {
                    foreach (var log in s.Logs)
                    {
                        timerLogs.AppendLine(log);
                    }
                }

                ViewBag.MessageTimer = timerLogs.ToString();
            }

            return View();

        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [HttpGet]
        public ActionResult UploadFile()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [HttpPost]
        public ActionResult UploadFile(HttpPostedFileBase file)
        {
            try
            {
                if (file != null && file.InputStream != null)
                {
                    //if (System.IO.File.Exists(@"C:\projects\eldosFile\WebAppEldos\WebAppEldos\VirtualDrive\Default1.st"))
                    //    System.IO.File.Delete(@"C:\projects\eldosFile\WebAppEldos\WebAppEldos\VirtualDrive\Default1.st");

                    using (var fileSystem = new EldosFileSystem())
                    {
                        fileSystem.AddFile(null, file.FileName, file.InputStream);
                    }
                }
                return RedirectToAction("Index");
            }
            catch
            {
                
            }


            return View();
        }
    }
}