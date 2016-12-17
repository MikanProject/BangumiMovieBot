using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BangumiMovieBot
{
    class Program
    {
        static void Main(string[] args)
        {
            FirstProcess();
            SecondProcess();
            ThirdProcess();
            Console.ReadKey();
        }

        static void FirstProcess()
        {
            var processor = new MoegirlWikiProcessor();
            processor.WriteFilesAsync(
                processor.GetMovieInfoAsync(2014, (DateTime.UtcNow + TimeSpan.FromDays(31)).Year).Result
            ).Wait();
        }
        static void SecondProcess()
        {
            var processor = new MoegirlWikiProcessor();
            processor.WriteFilesAsync(processor.AddReleaseDateAsync().Result).Wait();
        }
        static void ThirdProcess()
        {
            var processor = new MoegirlWikiProcessor();
            processor.GenerateBangumiDataAsync().Wait();
        }
    }
}
