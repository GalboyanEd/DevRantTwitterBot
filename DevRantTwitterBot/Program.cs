using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using devRantDotNet;

namespace DevRantTwitterBot
{
    class Program
    {
        static void Main(string[] args)
        {
        }

        private static List<long> getRantsIDs(int recentsCount = 25)
        {
            List<long> rantsIDs = new List<long>();
            using (devRant dr = new devRant())
            {
                List<devRantDotNet.Source.Models.Rant> randomRantScore = dr.GetRantsAsync(devRant.SortType.recent, recentsCount).Result;
                randomRantScore.ForEach((rant) => {
                    rantsIDs.Add(rant.id);
                });
            }
            return rantsIDs;
        }
    }
}