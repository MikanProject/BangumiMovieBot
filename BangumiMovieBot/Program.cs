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
            Console.ReadKey();
        }

        static async void FirstProcess()
        {
            var processor = new MoegirlWikiProcessor();
            await processor.WriteFilesAsync(
                await processor.GetMovieInfoAsync(2014, (DateTime.UtcNow + TimeSpan.FromDays(31)).Year));
        }
        static async void SecondProcess()
        {
            var processor = new MoegirlWikiProcessor();
            await processor.WriteFilesAsync(await processor.AddReleaseDateAsync());
        }
    }
}
