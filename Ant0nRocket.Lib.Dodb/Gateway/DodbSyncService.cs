using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Std20.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ant0nRocket.Lib.Dodb.Gateway
{
    public static class DodbSyncService
    {
        private static readonly Logger logger = Logger.Create(nameof(DodbSyncService));

        public static event Action OnSyncStarted;
        public static event Action<ProgressEventArgs> OnSyncProgress;
        public static event Action OnSyncCompleted;

        //public static void Sync()
        //{
        //    using var dbContext = DodbGateway.GetContext();
        //    OnSyncStarted?.Invoke();
        //    var knownDocumentsIds = GetKnownDocumentIds(dbContext);
        //    OnSyncCompleted?.Invoke();                        
        //}

        ///// <summary>
        ///// Returns all document numbers from database.
        ///// </summary>
        //private static HashSet<Guid> GetKnownDocumentIds()
        //{
        //    using var db = GetDb();
        //    var numbers = db.Documents
        //        .AsNoTracking()
        //        .Select(d => d.Id)
        //        .ToHashSet();
        //    return numbers;
        //}
    }
}
