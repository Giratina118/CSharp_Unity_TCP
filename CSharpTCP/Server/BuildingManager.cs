using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Server.CSVReader;

namespace Server
{
    public class Building
    {
        public ushort _id;    // 고유 번호
        
    }

    public class BuildingManager
    {
        public static BuildingManager Instance { get; } = new BuildingManager();
        
        public Dictionary<int, StructureData> BuildingData = new Dictionary<int, StructureData>();
        List<StructureSpawnData> BuildingSpawnDate = new List<StructureSpawnData>();

        /*
        public void InitData()
        {
            MonsterDataDic = LoadMonsterData(MonsterDataPath);
            MonsterSpawnDateList = LoadMonsterSpawnData(MonsterSpawnDataPath);

            foreach (MonsterSpawnData spawnData in MonsterSpawnDateList)
            {
                Spawn(spawnData);
            }
        }
        */
    }
}
