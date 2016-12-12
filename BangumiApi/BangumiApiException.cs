using System;

namespace BangumiApi
{
    internal class BangumiApiException : Exception
    {
        public BangumiApiException()
        {
        }

        public BangumiApiException(string message) : base(message)
        {
        }
    }
}