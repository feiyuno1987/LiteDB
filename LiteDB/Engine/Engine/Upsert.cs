﻿using System;
using System.Collections.Generic;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Implement upsert command to documents in a collection. Calls update on all documents,
        /// then any documents not updated are then attempted to insert.
        /// This will have the side effect of throwing if duplicate items are attempted to be inserted.
        /// </summary>
        public int Upsert(string collection, IEnumerable<BsonDocument> docs, BsonAutoId autoId = BsonAutoId.ObjectId)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (docs == null) throw new ArgumentNullException(nameof(docs));

            using (var trans = this.BeginTrans())
            {
                var col = trans.Collection.GetOrAdd(collection);

                // lock collection
                trans.WriteLock(collection);

                var count = 0;

                foreach (var doc in docs)
                {
                    // first try update document (if exists _id)
                    // if not found, insert
                    if (doc["_id"] == BsonValue.Null || this.UpdateDocument(trans, col, doc) == false)
                    {
                        this.InsertDocument(trans, col, doc, autoId);
                        count++;
                    }
                }

                // persist changes
                trans.Commit();

                // returns how many document was inserted
                return count;
            }
        }
    }
}