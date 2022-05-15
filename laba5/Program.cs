using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace laba5
{
    static class HttpServer
    {
        private static HttpListener listener;
        private const string url = "http://localhost:8000/";


        private static bool HandleIncomingConnections()
        {
            bool runServer = true;
            HttpListenerContext ctx = listener.GetContext();

            HttpListenerRequest req = ctx.Request;
            HttpListenerResponse resp = ctx.Response;

            Console.WriteLine(req.Url.ToString());
            Console.WriteLine(req.HttpMethod);
            Console.WriteLine(req.UserHostName);

            switch (req.HttpMethod)
            {
                case "GET":
                {
                    //display files in directory
                    if (req.Url.AbsolutePath.Contains("/files/") && (req.Url.AbsolutePath.Substring(1, 6) == "files/"))
                    {
                        String getPath = req.Url.LocalPath;
                        var localPath = getPath.Replace("/files/", String.Empty);
                        if (Directory.Exists(localPath))
                        {
                            string[] fileEntries = Directory.GetFiles(localPath);
                            resp.ContentType = "application/json";
                            resp.ContentEncoding = Encoding.UTF8;
                            var jsonFileEnteries = JsonConvert.SerializeObject(fileEntries);
                            using (var output = resp.OutputStream)
                            {
                                var buffer = Encoding.ASCII.GetBytes(jsonFileEnteries);
                                output.Write(buffer, 0, jsonFileEnteries.Length);
                                Console.WriteLine("status: {0}", resp.StatusCode);
                            }
                        }
                        else
                        {
                            Console.WriteLine("directory not found");
                            using (var output = resp.OutputStream)
                            {
                                var buffer = Encoding.ASCII.GetBytes("404 Not Found/directory not found");
                                output.Write(buffer, 0, buffer.Length);
                            }
                            Console.WriteLine("404 Not Found");
                        }
                    }
                    else if (req.Url.AbsolutePath.Contains("/file/") &&
                             (req.Url.AbsolutePath.Substring(1, 5) == "file/"))
                    {
                        //download file
                        String getPath = req.Url.LocalPath;
                        var localPath = getPath.Replace("/file/", String.Empty);
                        FileInfo fileToDownload = new FileInfo(localPath);
                        if (fileToDownload.Exists)
                        {
                            Console.WriteLine("file is there");
                            resp.ContentType = "application/force-download";
                            resp.Headers.Add("Content-Transfer-Encoding", "binary");
                            resp.Headers.Add("Content-Disposition", $"attachment; filename={fileToDownload.Name}");

                            using (var output = resp.OutputStream)
                            {
                                resp.ContentLength64 = fileToDownload.Length;
                                var buffer = File.ReadAllBytes(localPath);
                                output.Write(buffer, 0, buffer.Length);
                            }
                            Console.WriteLine("ststus: 201 Created");
                        }
                        else
                        {
                            Console.WriteLine("directory or file not found");
                            using (var output = resp.OutputStream)
                            {
                                var buffer = Encoding.ASCII.GetBytes("404 Not Found/directory or file not found");
                                output.Write(buffer, 0, buffer.Length);
                            }
                            Console.WriteLine("404 Not Found");
                        }
                    }
                    else if (req.Url.AbsolutePath == "/shutdown")
                    {//shutdown
                        Console.WriteLine("Shutdown requested");
                        runServer = false;
                    }
                    else
                    {
                        Console.WriteLine("invalid command");
                        Console.WriteLine("400 Bad Request");
                        using (var output = resp.OutputStream)
                        {
                            var buffer = Encoding.ASCII.GetBytes("400 Bad Request/invalid command");
                            output.Write(buffer, 0, buffer.Length);
                        }
                    }

                    break;
                }
                //put file into directory
                case "PUT":
                {
                    if (req.Url.AbsolutePath.Contains("/file/") &&
                        (req.Url.AbsolutePath.Substring(1, 5) == "file/"))
                    {
                        String getPath = req.Url.LocalPath;
                        var localPath = getPath.Replace("/file/", String.Empty);
                        var index = localPath.LastIndexOf("/", StringComparison.Ordinal);
                        var dirpath = localPath.Substring(0, index);
                        if (!Directory.Exists(dirpath))
                        {
                            Directory.CreateDirectory(dirpath);
                        }
                        using (var input = req.InputStream)
                        {
                            FileStream fileStream = File.Create(localPath);
                            input.CopyTo(fileStream);
                            fileStream.Close();
                        }
                        Console.WriteLine("status: {0}", resp.StatusCode);
                    }
                    else
                    {
                        Console.WriteLine("invalid command");
                        Console.WriteLine("400 Bad Request");
                        using (var output = resp.OutputStream)
                        {
                            var buffer = Encoding.ASCII.GetBytes("400 Bad Request/invalid command");
                            output.Write(buffer, 0, buffer.Length);
                        }
                    }

                    break;
                }
                //display fileinfo
                case "HEAD":
                {
                    if ((req.Url.AbsolutePath.Contains("/file/") &&
                         (req.Url.AbsolutePath.Substring(1, 5) == "file/")))
                    {
                        String getPath = req.Url.LocalPath;
                        var localPath = getPath.Replace("/file/", String.Empty);
                        FileInfo fileToGetInfo = new FileInfo(localPath);
                        if (fileToGetInfo.Exists)
                        {
                            Console.WriteLine("file found");
                            String fileType = fileToGetInfo.Extension;
                            if (fileType == ".txt" || fileType == ".doc" || fileType == ".docx" || fileType == ".odt" ||
                                fileType == ".pdf" || fileType == "rtf" || fileType == ".tex" || fileType == ".wpd")
                            {
                                StreamReader reader = new StreamReader(localPath);
                                resp.ContentEncoding = reader.CurrentEncoding;
                                reader.Close();
                            }

                            resp.ContentType = fileType;
                            resp.ContentLength64 = fileToGetInfo.Length;
                            resp.Headers.Add("Content-Transfer-Encoding", "binary");
                            resp.Headers.Add("Content-Disposition", $"attachment; filename={fileToGetInfo.Name}");
                            Console.WriteLine(resp.ContentType);
                            Console.WriteLine(resp.ContentLength64);
                            Console.WriteLine("status: {0}", resp.StatusCode);
                        }
                        else
                        {
                            Console.WriteLine("file not found");
                            Console.WriteLine("404 Not Found/file not found");
                            // using (var output = resp.OutputStream)
                            // {
                            //     var buffer = Encoding.ASCII.GetBytes("file not found");
                            //     resp.ContentLength64 = buffer.Length;
                            //     output.Write(buffer, 0, buffer.Length);
                            // }
                        }
                    }
                    else
                    {
                        Console.WriteLine("invalid command");
                        Console.WriteLine("400 Bad Request");
                        using (var output = resp.OutputStream)
                        {
                            var buffer = Encoding.ASCII.GetBytes("400 Bad Request/invalid command");
                            output.Write(buffer, 0, buffer.Length);
                        }
                    }

                    break;
                }
                //delete file or directory
                case "DELETE":
                {
                    if (req.Url.AbsolutePath.Contains("/files/") &&
                        (req.Url.AbsolutePath.Substring(1, 6) == "files/"))
                    {
                        String getPath = req.Url.LocalPath;
                        var localPath = getPath.Replace("/files/", String.Empty);
                        FileInfo fileToDelete = new FileInfo(localPath);
                        if (fileToDelete.Exists)
                        {
                            Console.WriteLine("file found");
                            File.Delete(localPath);
                            Console.WriteLine("status: {0}", resp.StatusCode);
                        }
                        else if ((!fileToDelete.Exists) && (Directory.Exists(localPath)))
                        {
                            Console.WriteLine("directory found");
                            DirectoryInfo di = new DirectoryInfo(localPath);
                            foreach (FileInfo file in di.GetFiles())
                            {
                                file.Delete();
                            }

                            foreach (DirectoryInfo dir in di.GetDirectories())
                            {
                                dir.Delete(true);
                            }

                            Directory.Delete(localPath);
                            Console.WriteLine("status: {0}", resp.StatusCode);
                        }
                        else
                        {
                            Console.WriteLine("directory of file not found");
                            using (var output = resp.OutputStream)
                            {
                                var buffer = Encoding.ASCII.GetBytes("404 Not Forund/directory or file not found");
                                resp.ContentLength64 = buffer.Length;
                                output.Write(buffer, 0, buffer.Length);
                            }
                            Console.WriteLine("404 Not Forund");
                        }
                    }
                    else
                    {
                        Console.WriteLine("invalid command");
                        Console.WriteLine("400 Bad Request");
                        using (var output = resp.OutputStream)
                        {
                            var buffer = Encoding.ASCII.GetBytes("400 Bad Request/invalid command");
                            output.Write(buffer, 0, buffer.Length);
                        }
                    }

                    break;
                }
            }
            Console.WriteLine();
            resp.Close();

            return runServer;
        }


        public static void Main()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            while (HandleIncomingConnections())
            {
            }

            listener.Close();
        }
    }
}