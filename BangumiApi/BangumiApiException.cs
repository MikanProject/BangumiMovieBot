using System;

namespace BangumiApi
{
    public class BangumiApiException : Exception
    {
        public BangumiApiException()
        {
        }

        public BangumiApiException(string message) : base(message)
        {
        }
    }
}