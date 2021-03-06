﻿using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TenderPlanAPI.Models;
using TenderPlanAPI.Parameters;

namespace TenderPlanAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TenderPlanIndexController:ControllerBase
    {
        private readonly IDBConnectContext db;

        public TenderPlanIndexController(IDBConnectContext Db)
        {
            db = Db ?? throw new System.ArgumentNullException(nameof(Db));
        }

        /// <summary>
        /// </summary>
        /// <returns>Список всех файлов в индексе</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TenderPlanIndex>>> Get()
        {
            return (await db.TenderPlansIndex.FindAsync(Builders<TenderPlanIndex>.Filter.Empty)).ToList();
        }

        [HttpPost]
        public  ActionResult Post([FromBody]IEnumerable<TenderPlanIndexParam> TenderPlanIndexes)
        {
            TenderPlanIndexes.AsParallel().ForAll(i =>
            {
                var filter= Builders<TenderPlanIndex>.Filter.Eq("TenderPlanId", i.TenderPlanId);

                var indexedFile = db.TenderPlansIndex.Find(filter).ToList().FirstOrDefault();
                if (indexedFile == null)
                {
                    // Файл не был проиндексирован ранее
                    var newIndexedFile = new TenderPlanIndex
                    {
                        FTPFileId = i.FTPFileId,
                        TenderPlanId = i.TenderPlanId,
                        RevisionId = i.RevisionId
                    };
                    db.TenderPlansIndex.InsertOne(newIndexedFile);
                }
                else
                {
                    // Файл уже есть в индексе
                    if (indexedFile.RevisionId != i.RevisionId)
                    {
                        var setOutdated = Builders<TenderPlanIndex>.Update.Set("IsOutdated", true);
                        var setRevisionId = Builders<TenderPlanIndex>.Update.Set("RevisionId", i.RevisionId);
                        var update = Builders<TenderPlanIndex>.Update.Combine(setOutdated, setRevisionId);
                        var updateFilter = Builders<TenderPlanIndex>.Filter.Eq("_id", indexedFile.Id);
                        db.TenderPlansIndex.UpdateOne(updateFilter, update);
                    }
                }
            });

            return Ok();
        }

    }
}
