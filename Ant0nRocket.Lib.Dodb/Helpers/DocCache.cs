using Ant0nRocket.Lib.Dodb.Models;
using Ant0nRocket.Lib.Extensions;

namespace Ant0nRocket.Lib.Dodb.Helpers
{
    /// <summary>
    /// Holds a cache of documents so there is no need to query database every time
    /// </summary>
    public class DocCache
    {
        private readonly Dictionary<Guid, DocInfo> docsIdCache = [];
        private readonly Dictionary<DateTime, List<DocInfo>> docsDateCache = [];

        /// <summary>
        /// Addes information about the <see cref="Document"/> into the cache.
        /// </summary>
        public void AddToCache(Guid docId, DateTime dateCreatedUtc)
        {
            if (docsIdCache.ContainsKey(docId)) return; // already in cache

            var docInfo = new DocInfo { Id = docId, DateCreatedUtc = dateCreatedUtc };
            docsIdCache.Add(docId, docInfo);

            dateCreatedUtc = dateCreatedUtc.StartOfTheDay(); // we need plain day here, without time part
            if (!docsDateCache.ContainsKey(dateCreatedUtc)) 
                docsDateCache.Add(dateCreatedUtc, []);
            docsDateCache[dateCreatedUtc].Add(docInfo);
        }

        /// <summary>
        /// Check <paramref name="docId"/> in cache
        /// </summary>
        public bool ContainsDoc(Guid docId) => docsIdCache.ContainsKey(docId);

        /// <summary>
        /// Check documents exists for specified <paramref name="dateTime"/>
        /// </summary>
        public bool HasDocs(DateTime dateTime) => 
            docsDateCache.ContainsKey(dateTime.StartOfTheDay(DateTimeKind.Utc));

        /// <summary>
        /// Returnes known <see cref="DocInfo"/> for specified <paramref name="dateTimeUtc"/>
        /// </summary>
        public List<DocInfo> GetDocs(DateTime dateTimeUtc)
        {
            dateTimeUtc = dateTimeUtc.StartOfTheDay();
            if (docsDateCache.ContainsKey(dateTimeUtc))
                return docsDateCache[dateTimeUtc];
            return [];
        }

        /// <summary>
        /// Is cache empty or not?
        /// </summary>
        public bool IsEmpty => docsIdCache.Count == 0;
    }
}
