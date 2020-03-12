using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PuppeteerSharp;

namespace PdfPrinter
{
    public static class PdfPrintFunction
    {
        [FunctionName("PdfPrintFunction")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req, ILogger log)
        {
            RequestBody requestBody = JsonConvert.DeserializeObject<RequestBody>(await req.Content.ReadAsStringAsync());
            List<PdfOutPut> pdfOutPuts = new List<PdfOutPut>();
            
            var browser = await Puppeteer.ConnectAsync(new ConnectOptions { BrowserWSEndpoint = $"ws://localhost:3000" });
            foreach (var pageObject in requestBody.PageObjects)
            {
                Page page = null;
                try
                {
                    PdfOutPut pdfOutPut = new PdfOutPut();
                    using (page = await browser.NewPageAsync())
                    {
                        //disable caching, enable Javascript execution
                        await page.SetCacheEnabledAsync(false);
                        await page.SetJavaScriptEnabledAsync(true);

                        //Navigate to the URL, wait until there are no more than 0 network connections for at least 500ms
                        await page.GoToAsync(pageObject.Url, WaitUntilNavigation.Networkidle0);

                        using (var ms = new MemoryStream())
                        using (var stream = await page.PdfStreamAsync())
                        {
                            stream.CopyTo(ms);

                            pdfOutPut.Content = ms.ToArray();
                            pdfOutPut.Filename = $"{pageObject.FileName}.pdf";
                        }
                    }

                    pdfOutPuts.Add(pdfOutPut);
                }
                catch (Exception ex)
                {
                    log.Log(LogLevel.Error, new Exception(ex.Message), ex.Message);
                }
                finally
                {
                    if (page != null)
                    {
                        await page.CloseAsync();
                    }
                }
            }

            var zip = Zip(pdfOutPuts);

            return new FileContentResult(zip, "application/zip")
            {
                FileDownloadName = $"{Guid.NewGuid().ToString()}.zip"
            };
        }

        private static byte[] Zip(IEnumerable<PdfOutPut> results)
        {
            byte[] archiveFile;
            using (var archiveStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
                {
                    foreach (var result in results)
                    {
                        var zipArchiveEntry = archive.CreateEntry(result.Filename);
                        using (var zipStream = zipArchiveEntry.Open())
                        {
                            zipStream.Write(result.Content, 0, result.Content.Length);
                        }
                    }
                }

                archiveFile = archiveStream.ToArray();
            }

            return archiveFile;
        }
    }

    public class RequestBody
    {
        public IEnumerable<PageObject> PageObjects { get; set; }
    }

    public class PageObject
    {
        public string FileName { get; set; }
        public string Url { get; set; }
    }

    public class PdfOutPut
    {
        public byte[] Content { get; set; }
        public string Filename { get; set; }
    }
}
