#region

using System.Collections.Generic;
using System.Data;
using System.Linq;
using Game.Data.Stats;
using Game.Util;
using Persistance;

#endregion

namespace Game.Data
{
    public class Structure : GameObject, IStructure
    {
        public const string DB_TABLE = "structures";

        public string Theme { get; set; }

        private readonly ITechnologyManager techmanager;

        private StructureProperties properties;

        private IStructureStats stats;

        private readonly IDbManager dbManager;

        public Structure(uint structureId, 
                         IStructureStats stats,
                         uint x,
                         uint y,
                         string theme,
                         ITechnologyManager technologyManager,
                         StructureProperties structureProperties,
                         IDbManager dbManager) : base(structureId, x, y)
        {
            Theme = theme;
            this.stats = stats;
            this.dbManager = dbManager;
            techmanager = technologyManager;
            properties = structureProperties;
        }

        #region Indexers

        public object this[string name]
        {
            get
            {
                return properties.Get(name);
            }
            set
            {
                CheckUpdateMode();
                properties.Add(name, value);
            }
        }

        #endregion

        #region Properties

        public ITechnologyManager Technologies
        {
            get
            {
                return techmanager;
            }
        }

        public StructureProperties Properties
        {
            get
            {
                return properties;
            }
            set
            {
                CheckUpdateMode();
                properties = value;
            }
        }
        
        #endregion

        public bool IsMainBuilding
        {
            get
            {
                return ObjectId == 1;
            }
        }

        public IStructureStats Stats
        {
            get
            {
                return stats;
            }
            set
            {
                if (stats != null)
                {
                    stats.StatsUpdate -= CheckUpdateMode;
                }

                CheckUpdateMode();
                stats = value;
                stats.StatsUpdate += CheckUpdateMode;
            }
        }

        public override byte Size
        {
            get
            {
                return stats.Base.Size;
            }
        }

        public override ushort Type
        {
            get
            {
                return stats.Base.Type;
            }
        }

        public byte Lvl
        {
            get
            {
                return stats.Base.Lvl;
            }
        }

        #region IPersistableObject Members

        public string DbTable
        {
            get
            {
                return DB_TABLE;
            }
        }

        public DbColumn[] DbColumns
        {
            get
            {
                return new[]
                {                    
                    new DbColumn("x", PrimaryPosition.X, DbType.UInt32),
                    new DbColumn("y", PrimaryPosition.Y, DbType.Int32),
                    new DbColumn("hp", stats.Hp, DbType.Decimal),
                    new DbColumn("type", Type, DbType.Int16),
                    new DbColumn("theme_id", Theme, DbType.String),
                    new DbColumn("level", Lvl, DbType.Byte),
                    new DbColumn("labor", stats.Labor, DbType.UInt16),
                    new DbColumn("is_blocked", IsBlocked, DbType.UInt32),
                    new DbColumn("in_world", InWorld, DbType.Boolean),
                    new DbColumn("state", (byte)State.Type, DbType.Boolean),
                    new DbColumn("state_parameters",
                                 XmlSerializer.SerializeList(State.Parameters.ToArray()),
                                 DbType.String)
                };
            }
        }

        public DbColumn[] DbPrimaryKey
        {
            get
            {
                return new[] {new DbColumn("id", ObjectId, DbType.UInt32), new DbColumn("city_id", City.Id, DbType.UInt32)};
            }
        }

        #endregion

        protected override bool Update()
        {
            var update = base.Update();

            if (update)
            {
                dbManager.Save(this);
            }

            return update;
        }

        public IEnumerable<DbDependency> DbDependencies
        {
            get
            {
                return new[] {new DbDependency("Properties", true, true), new DbDependency("Technologies", false, true)};
            }
        }

        public bool DbPersisted { get; set; }

        public override int Hash
        {
            get
            {
                return City != null ? City.Hash : int.MaxValue;
            }
        }

        public override object Lock
        {
            get
            {
                return City != null ? City.Lock : null;
            }
        }


    }
}