using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Runner_Score_Server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("{0} | Started.", DateTime.Now);

            await Listen();
        }

        private static async Task Listen()
        {
            
            HttpListener httpListener = new HttpListener();

            httpListener.Prefixes.Add("http://localhost:8888/");
            httpListener.Start();

            Console.WriteLine("{0} | Listening.", DateTime.Now);

            while (!Console.KeyAvailable)
            {
                try
                {
                    HttpListenerContext listenerContext = await httpListener.GetContextAsync();

                    HttpListenerRequest listenerRequest = listenerContext.Request;
                    HttpListenerResponse listenerResponse = listenerContext.Response;

                    Stream requestBodyStream = listenerRequest.InputStream;
                    Encoding requestBodyEncoding = listenerRequest.ContentEncoding;
                    StreamReader reader = new StreamReader(requestBodyStream, requestBodyEncoding);

                    string requestBodyData = await reader.ReadToEndAsync();
                    Console.WriteLine("{0} | Received: \n{1}\n=================================", DateTime.Now, requestBodyData);

                    Stream output = listenerResponse.OutputStream;
                    byte[] responseData = Encoding.UTF8.GetBytes(string.Empty);
                    output.Write(responseData, 0, 0);
                    output.Close();


                    requestBodyStream.Close();
                    reader.Close();


                    Console.WriteLine(requestBodyData);
                    Score data = DeserializeFromJSON(requestBodyData);

                    await SaveDataToDB(data);
                                        
                                
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0} | [ERROR] {1}", DateTime.Now, ex.Message);
                    Console.WriteLine("\n{0}", ex.ToString());
                }
            }

            httpListener.Stop();
            Console.WriteLine("{0} | HTTP Listener stopped", DateTime.Now);
        }

        private static Score DeserializeFromJSON(string _requestBodyData)
        {
            Score score = JsonConvert.DeserializeObject<Score>(_requestBodyData);
            return score;
        }

        private static async Task SaveDataToDB(Score _data)
        {
            const string connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=ScoreDatabase.mdf;Trusted_Connection=True;";
            //const string connectionString = "Data Source=ScoreDatabase.mdf";

            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    await sqlConnection.OpenAsync();

                    SqlCommand sqlCommand = new SqlCommand();
                    sqlCommand.Connection = sqlConnection;
                    sqlCommand.CommandText = @"INSERT INTO Table VALUES (@PlayerName, @Score)";
                    sqlCommand.Parameters.Add("@PlayerName", System.Data.SqlDbType.NVarChar, 50);
                    sqlCommand.Parameters.Add("@Score", System.Data.SqlDbType.Int, 128);

                    string playerName = _data._name;
                    int playerScore = int.Parse(_data._score);

                    sqlCommand.Parameters["@PlayerName"].Value = playerName;
                    sqlCommand.Parameters["@Score"].Value = playerScore;

                    await sqlCommand.ExecuteNonQueryAsync();

                    Console.WriteLine("{0} | Saved new score. Player: {1} Score: {2}", DateTime.Now, playerName, playerScore);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} | [ERROR] {1}", DateTime.Now, ex.Message);
            }
        }

        public class Score
        {
            [JsonProperty(PropertyName = "playerScore")]
            //public int _score { get; set; }
            // TODO: JsonConvert cannot parse second digit in received json file. (???????!!!!!!)
            // Ex.
            // JSON: {"playerScore":87,"playerName":"PlayerName"}
            // Exeception message: After parsing a value an unexpected character was encountered: 7. Path 'playerScore', line 1, position 32.
            public string _score { get; set; }

            [JsonProperty(PropertyName = "playerName")]
            public string _name { get; set; }

            public Score() { }
        }
    }
}
