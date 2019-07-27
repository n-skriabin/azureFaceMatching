using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AzureFaceMatchingConsole
{
    class Program
    {
        // Replace <Subscription Key> with your valid subscription key.
        const string subscriptionKey = "sub_key";

        // NOTE: You must use the same region in your REST call as you used to
        // obtain your subscription keys. For example, if you obtained your
        // subscription keys from westus, replace "westcentralus" in the URL
        // below with "westus".
        //
        // Free trial subscription keys are generated in the "westus" region.
        // If you use a free trial subscription key, you shouldn't need to change
        // this region.
        const string uriBaseDetect = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/detect";
        const string uriBaseVerify = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/verify";

        public static string faceId_first = string.Empty;
        public static string faceId_second = string.Empty;

        static void Main(string[] args)
        {
            // Get the path and filename to process from the user.
            Console.WriteLine("Detect faces:");

            string firstImageFilePath = "C:\\Users\\Nick\\Desktop\\maxresdefault.jpg";
            string secondImageFilePath = "C:\\Users\\Nick\\Desktop\\fredie-test.png";

            try
            {
                faceId_first = MakeAnalysisRequest(firstImageFilePath).Result;
                faceId_second = MakeAnalysisRequest(secondImageFilePath).Result;

                var result = CompareFacesRequest(faceId_first, faceId_second).Result;

                Console.WriteLine(result);

                Console.WriteLine("\nWait a moment for the results to appear.\n");
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.Message + "\nPress Enter to exit...\n");
            }

            Console.ReadLine();
        }

        // Gets the analysis of the specified image by using the Face REST API.
        static async Task<string> MakeAnalysisRequest(string imageFilePath)
        {
            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add(
                "Ocp-Apim-Subscription-Key", subscriptionKey);

            // Request parameters. A third optional parameter is "details".
            string requestParameters = "returnFaceId=true&returnFaceLandmarks=false" +
                "&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses," +
                "emotion,hair,makeup,occlusion,accessories,blur,exposure,noise";

            // Assemble the URI for the REST API Call.
            string uri = uriBaseDetect + "?" + requestParameters;

            HttpResponseMessage response;

            // Request body. Posts a locally stored JPEG image.
            byte[] byteData = GetImageAsByteArray(imageFilePath);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json"
                // and "multipart/form-data".
                content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/octet-stream");

                // Execute the REST API call.
                response = await client.PostAsync(uri, content);

                // Get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();
                contentString = contentString.Substring(1, contentString.Length - 2);

                // Display the JSON response.
                Console.WriteLine("\nResponse:\n");
                var jsonResult = JsonConvert.DeserializeObject<AzureFaceApiResponceDetect>(contentString);
                Console.WriteLine(JsonPrettyPrint(contentString));
                Console.WriteLine("\nPress Enter to exit...");

                return jsonResult.faceId;
            }
        }

        static async Task<string> CompareFacesRequest(string faceId_1, string faceId_2)
        {
            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add(
                "Ocp-Apim-Subscription-Key", subscriptionKey);

            // Request parameters. A third optional parameter is "details".
            //string requestParameters = "returnFaceId=true&returnFaceLandmarks=false" +
            //    "&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses," +
            //    "emotion,hair,makeup,occlusion,accessories,blur,exposure,noise";

            // Assemble the URI for the REST API Call.
            string uri = uriBaseVerify/* + "?" + requestParameters*/;

            HttpResponseMessage response;

            // Request body. Posts a locally stored JPEG image.
            //byte[] byteData = GetImageAsByteArray(imageFilePath);

            string requestBody = JsonConvert.SerializeObject(new { faceId1 = faceId_1, faceId2 = faceId_2 });
            requestBody = requestBody.Insert(1, "\"@type\": \"job\",");
            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(requestBody);
            var content = new ByteArrayContent(messageBytes);

            // This example uses content type "application/octet-stream".
            // The other content types you can use are "application/json"
            // and "multipart/form-data".
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            // Execute the REST API call.
            response = await client.PostAsync(uri, content);

            // Get the JSON response.
            string contentString = await response.Content.ReadAsStringAsync();
            //contentString = contentString.Substring(1, contentString.Length - 2);

            // Display the JSON response.
            Console.WriteLine("\nResponse:\n");
            var jsonResult = JsonConvert.DeserializeObject<AzureFaceApiResponse>(contentString);
            Console.WriteLine(JsonPrettyPrint(contentString));
            Console.WriteLine("\nPress Enter to exit...");

            return jsonResult.isIdentical;
        }

        // Returns the contents of the specified file as a byte array.
        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }

        // Formats the given JSON string by adding line breaks and indents.
        static string JsonPrettyPrint(string json)
        {
            if (string.IsNullOrEmpty(json))
                return string.Empty;

            json = json.Replace(Environment.NewLine, "").Replace("\t", "");

            StringBuilder sb = new StringBuilder();
            bool quote = false;
            bool ignore = false;
            int offset = 0;
            int indentLength = 3;

            foreach (char ch in json)
            {
                switch (ch)
                {
                    case '"':
                        if (!ignore) quote = !quote;
                        break;
                    case '\'':
                        if (quote) ignore = !ignore;
                        break;
                }

                if (quote)
                    sb.Append(ch);
                else
                {
                    switch (ch)
                    {
                        case '{':
                        case '[':
                            sb.Append(ch);
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', ++offset * indentLength));
                            break;
                        case '}':
                        case ']':
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', --offset * indentLength));
                            sb.Append(ch);
                            break;
                        case ',':
                            sb.Append(ch);
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', offset * indentLength));
                            break;
                        case ':':
                            sb.Append(ch);
                            sb.Append(' ');
                            break;
                        default:
                            if (ch != ' ') sb.Append(ch);
                            break;
                    }
                }
            }

            return sb.ToString().Trim();
        }
    }
}
