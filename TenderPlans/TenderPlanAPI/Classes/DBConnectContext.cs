﻿using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TenderPlanAPI.Models;

namespace TenderPlanAPI.Controllers
{
    public class DBConnectContext
    {
        
        private MongoClient client
        {
            get
            {
#if DEBUG
                //return new MongoClient("mongodb://10.48.93.43:27017/");
                return new MongoClient("mongodb://127.0.0.1:27017");
#else
                return new MongoClient("mongodb://5.8.180.100:27017/");
#endif
            }
        }


        private IMongoCollection<T> _connectionTenderPlan<T>(string name)
        {
            var db = client.GetDatabase("TenderPlans");
            return db.GetCollection<T>(name);
        }

        public IMongoCollection<Customer> Customers => _connectionTenderPlan<Customer>("customers");
        public IMongoCollection<TenderPlan> TenderPlans => _connectionTenderPlan<TenderPlan>("tenderPlans");
        public IMongoCollection<TenderPlanPosition> TenderPlanPositions => _connectionTenderPlan<TenderPlanPosition>("tenderPlanPosition");

        public IMongoCollection<TenderPlanIndex> TenderPlansIndex {
            get {
                var tpInd = _connectionTenderPlan<TenderPlanIndex>("tenderPlansIndex");
                var crtInd = Builders<TenderPlanIndex>.IndexKeys.Ascending(tpi => tpi.TenderPlanId);
                var crtIndOpt = new CreateIndexOptions<TenderPlanIndex> { Unique = true };
                var crtIndModel = new CreateIndexModel<TenderPlanIndex>(crtInd, crtIndOpt);
                tpInd.Indexes.CreateOne(crtIndModel);
                return tpInd;
            }
        }

        private IMongoCollection<T> _connectionFTPMonitor<T>(string name)
        {
            var dbName = TestEnvHelper.IsTestEnv ? "Test" : "FTPMonitor";
            var db = client.GetDatabase(dbName);
            return db.GetCollection<T>(name);
        }

        public IMongoCollection<FTPPath> FTPPath => _connectionFTPMonitor<FTPPath>("ftpPath");

        public IMongoCollection<FTPEntry> FTPEntry => _connectionFTPMonitor<FTPEntry>("ftpFile");

    }
}