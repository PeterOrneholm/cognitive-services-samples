using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Forms
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var fileStream = new FileStream(@"BVD3_5.pdf", FileMode.Open);
            var fileBytes = ReadFully(fileStream);
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SecretKeys.FormKey);

                using (var content = new ByteArrayContent(fileBytes))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                    var response = await httpClient.PostAsync($"https://westeurope.api.cognitive.microsoft.com/formrecognizer/v1.0-preview/custom/models/748d7611-50ae-465f-a15a-0eab036d7c79/analyze", content).ConfigureAwait(false);
                    var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var form = JsonConvert.DeserializeObject<Form>(result);
                    var productNames = GetValues(form, "Product name");
                    var emails = GetValues(form, "E-mail");
                    var websites = GetValues(form, "Website:");
                    var company = GetValues(form, "Company");

                    foreach (var page in form.pages)
                    {
                        foreach (var kvp in page.keyValuePairs)
                        {
                            foreach (var v in kvp.value)
                            {
                                Console.WriteLine($"{kvp.key.FirstOrDefault()?.text}: {v.text}");
                            }
                        }
                    }
                }
            }

            Console.ReadLine();
        }

        private static List<Value> GetValues(Form form, string key)
        {
            return form.pages.SelectMany(
                        a => a.keyValuePairs
                            .Where(x => x.key.Any(y => y.text == key))
                            .SelectMany(x => x.value)
                    ).ToList();
        }

        private static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }


    public class Form
    {
        public string status { get; set; }
        public Page[] pages { get; set; }
        public object[] errors { get; set; }
    }

    public class Page
    {
        public int number { get; set; }
        public int height { get; set; }
        public int width { get; set; }
        public Keyvaluepair[] keyValuePairs { get; set; }
        public Table[] tables { get; set; }
    }

    public class Keyvaluepair
    {
        public Key[] key { get; set; }
        public Value[] value { get; set; }
    }

    public class Key
    {
        public string text { get; set; }
        public float[] boundingBox { get; set; }
    }

    public class Value
    {
        public string text { get; set; }
        public float[] boundingBox { get; set; }
        public double confidence { get; set; }
    }

    public class Table
    {
        public string id { get; set; }
        public Column[] columns { get; set; }
    }

    public class Column
    {
        public Header[] header { get; set; }
        public Entry[][] entries { get; set; }
    }

    public class Header
    {
        public string text { get; set; }
        public float[] boundingBox { get; set; }
    }

    public class Entry
    {
        public string text { get; set; }
        public float[] boundingBox { get; set; }
        public double confidence { get; set; }
    }

}
