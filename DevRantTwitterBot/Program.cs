using System;
using System.Net;
using System.IO;
using System.Media;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using devRantDotNet;
using Tweetinvi;

namespace DevRantTwitterBot
{
    class Config
    {
        public static string path = "TweetedRants.txt";
        public static string fileName = "currentImage";

    }

   

    class Program
    {
        static void Main(string[] args)
        {

            string CONSUMER_KEY = ConfigurationManager.AppSettings["CONSUMER_KEY"];
            string CONSUMER_SECRET = ConfigurationManager.AppSettings["CONSUMER_SECRET"];
            string ACCESS_TOKEN = ConfigurationManager.AppSettings["ACCESS_TOKEN"];
            string ACCESS_TOKEN_SECRET = ConfigurationManager.AppSettings["ACCESS_TOKEN_SECRET"];
            
            Auth.SetUserCredentials(CONSUMER_KEY, CONSUMER_SECRET, ACCESS_TOKEN, ACCESS_TOKEN_SECRET);
            
            while(true){
                mainLoop();
            }

        }

        static void mainLoop(){
            var recentRants = getRecentTopRants(10);

            foreach (var rant in recentRants)
            {
                tweet(rant.id);
                Thread.Sleep(600 * 1);
            }
        }

        private static List<devRantDotNet.Source.Models.Rant> getRecentTopRants(int recentsCount = 25)
        {
            List<devRantDotNet.Source.Models.Rant> recentRants;
            using (devRant dr = new devRant())
            {
                recentRants = dr.GetRantsAsync(devRant.SortType.recent, recentsCount).Result;
                recentRants.Sort((firstObj, secondObj) =>
                {
                    return secondObj.score - firstObj.score;
                }
);
            }
            return recentRants;
        }

        private static int getHighScore() 
        {
            List<int> rantsScores = new List<int>();
            using (devRant dr = new devRant())
            {
                List<devRantDotNet.Source.Models.Rant> getRants = dr.GetRantsAsync(devRant.SortType.recent, 25).Result;
                getRants.ForEach((rant) => {
                    rantsScores.Add(rant.score);

                    Console.WriteLine(rant.score);
                });
            }
            rantsScores.Sort();

            return rantsScores.Last();
        }

        private static bool goodRant(long rantId)
        {
            devRant dr = new devRant();

            int rantLength = dr.GetRantAsync(Convert.ToInt32(rantId)).Result.text.Length; //ToDo

            if(rantLength > 280)
            {
                return false;
            }

            using(StreamReader sr = File.OpenText(Config.path))
            {
                string currentRantId;
                while((currentRantId = sr.ReadLine()) != null)
                    if(Convert.ToInt64(currentRantId) == rantId)
                        return false;
            }     
            return true;
        }

        private static bool checkImage(long rantId) 
        {
            devRant dr = new devRant();
            string img = dr.GetRantAsync(Convert.ToInt32(rantId)).Result.attachedImageUrl;

            if(img != null)
            {
                return true;
            }

            return false;
        }

        private static Tweetinvi.Models.IMedia downloadImg(long runtId)
        {
            devRant dr = new devRant();
            string imgUrl = dr.GetRantAsync(Convert.ToInt32(runtId)).Result.attachedImageUrl;
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(new Uri(imgUrl), Config.fileName);
            }

            var image = File.ReadAllBytes(Config.fileName);
            File.Delete(Config.fileName);
            var media = Upload.UploadImage(image);
            return media;
        }

        private static void tweet(long rantId) 
        {
            devRant dr = new devRant();

            if(goodRant(rantId) == false)
                return;

            if (checkImage(rantId) == false)
            {
                Tweet.PublishTweet(dr.GetRantAsync(Convert.ToInt32(rantId)).Result.text);
                return;
            }

            Tweet.PublishTweet(dr.GetRantAsync(Convert.ToInt32(rantId)).Result.text, new Tweetinvi.Parameters.PublishTweetOptionalParameters
            {
                Medias = new List<Tweetinvi.Models.IMedia> { downloadImg(rantId) }
            });

            using (StreamWriter outputFile = new StreamWriter(Config.path, append: true))
            {
                Console.WriteLine("Current rant id writing to file is {0}", rantId);
                outputFile.WriteLineAsync(Convert.ToString(rantId));
            }
        }
    }
}