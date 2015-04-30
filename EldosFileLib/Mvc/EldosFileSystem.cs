using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mime;
using System.IO;
using System.Web.Mvc;
using System.Web;
using EldosFileLib.Extentions;

namespace EldosFileLib.Mvc
{
    public class FileStreamingContentDispositionResult : ActionResult
    {
        // default buffer size as defined in BufferedStream type
        private const int _bufferSize = 0x1000;
        private string _fileDownloadName;
        public FileStreamingContentDispositionResult.DispositionResultType DispositionResult { get; private set; }
        public Stream FileStream { get; private set; }
        public string ContentType { get; private set; }
        public string FileDownloadName
        {
            get { return _fileDownloadName ?? String.Empty; }
            set { _fileDownloadName = value; }
        }

        /// <summary>
        /// Initializes a new instance of the FileStreamingContentDispositionResult class.
        /// </summary>
        /// <param name="fileDownloadName"></param>
        public FileStreamingContentDispositionResult(byte[] fileBytes, string fileDownloadName, FileStreamingContentDispositionResult.DispositionResultType propertyName, string contentType)
            : this(new MemoryStream(fileBytes), fileDownloadName, propertyName, contentType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FileStreamingContentDispositionResult class.
        /// </summary>
        /// <param name="fileDownloadName"></param>
        public FileStreamingContentDispositionResult(Stream fileStream, string fileDownloadName, FileStreamingContentDispositionResult.DispositionResultType dispostionResultType, string contentType)
        {
            _fileDownloadName = fileDownloadName;
            DispositionResult = dispostionResultType;
            FileStream = fileStream;
            ContentType = contentType;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context.IsNull())
            {
                throw new ArgumentNullException("context");
            }
            HttpResponseBase response = context.HttpContext.Response;
            response.ContentType = ContentType;
            if (!String.IsNullOrEmpty(FileDownloadName))
            {
                // From RFC 2183, Sec. 2.3:
                // The sender may want to suggest a filename to be used if the entity is
                // detached and stored in a separate file. If the receiving MUA writes
                // the entity to a file, the suggested filename should be used as a
                // basis for the actual filename, where possible.
                string headerValue = ContentDispositionUtil.GetHeaderValue(FileDownloadName, DispositionResult);
                context.HttpContext.Response.AddHeader("Content-Disposition", headerValue);
            }
            WriteFile(response);
        }

        protected void WriteFile(HttpResponseBase response)
        {
            // grab chunks of data and write to the output stream
            Stream outputStream = response.OutputStream;
            using (FileStream)
            {
                byte[] buffer = new byte[_bufferSize];
                while (true)
                {
                    int bytesRead = FileStream.Read(buffer, 0, _bufferSize);
                    if (bytesRead == 0)
                    {
                        // no more data
                        break;
                    }
                    outputStream.Write(buffer, 0, bytesRead);
                }
            }
        }

        public enum DispositionResultType
        {
            Attchement = 0,
            Inline = 1
        }

        private static class ContentDispositionUtil
        {
            private const string _hexDigits = "0123456789ABCDEF";
            private static void AddByteToStringBuilder(byte b, StringBuilder builder)
            {
                builder.Append('%');
                int i = b;
                AddHexDigitToStringBuilder(i >> 4, builder);
                AddHexDigitToStringBuilder(i % 16, builder);
            }

            private static void AddHexDigitToStringBuilder(int digit, StringBuilder builder)
            {
                builder.Append(_hexDigits[digit]);
            }

            private static string CreateRfc2231HeaderValue(string filename, DispositionResultType dispositionResultType)
            {
                StringBuilder builder = new StringBuilder(string.Format("{0}; filename*=UTF-8''", dispositionResultType.ToString().ToLower()));
                byte[] filenameBytes = Encoding.UTF8.GetBytes(filename);
                foreach (byte b in filenameBytes)
                {
                    if (IsByteValidHeaderValueCharacter(b))
                    {
                        builder.Append((char)b);
                    }
                    else
                    {
                        AddByteToStringBuilder(b, builder);
                    }
                }
                return builder.ToString();
            }

            public static string GetHeaderValue(string fileName, DispositionResultType dispositionResultType)
            {
                try
                {
                    // first, try using the .NET built-in generator
                    ContentDisposition disposition = new ContentDisposition()
                    {
                        FileName = fileName,
                        Inline = dispositionResultType == DispositionResultType.Inline ? true : false
                    };
                    return disposition.ToString();
                }
                catch (FormatException)
                {
                    // otherwise, fall back to RFC 2231 extensions generator
                    return CreateRfc2231HeaderValue(fileName, dispositionResultType);
                }
            }

            // Application of RFC 2231 Encoding to Hypertext Transfer Protocol (HTTP) Header Fields, sec. 3.2
            // http://greenbytes.de/tech/webdav/draft-reschke-rfc2231-in-http-latest.html
            private static bool IsByteValidHeaderValueCharacter(byte b)
            {
                if ((byte)'0' <= b && b <= (byte)'9')
                {
                    return true; // is digit
                }
                if ((byte)'a' <= b && b <= (byte)'z')
                {
                    return true; // lowercase letter
                }
                if ((byte)'A' <= b && b <= (byte)'Z')
                {
                    return true; // uppercase letter
                }
                switch (b)
                {
                    case (byte)'-':
                    case (byte)'.':
                    case (byte)'_':
                    case (byte)'~':
                    case (byte)':':
                    case (byte)'!':
                    case (byte)'$':
                    case (byte)'&':
                    case (byte)'+':
                        return true;
                }
                return false;
            }
        }
    }
}
