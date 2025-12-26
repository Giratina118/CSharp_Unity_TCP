using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Server.CSVReader;

namespace Server
{
    public class Structure
    {
        public int Type;     // 종류
        public Vector3 Size; // 크기
        public Vector3 Pos;  // 위치

        public Structure(StructureData structureData, StructureSpawnData structureSpawnData)
        {
            Type = structureData.Type;
            Size = new Vector3(structureData.XSize, structureData.YSize, structureData.ZSize);
            Pos = new Vector3(structureSpawnData.XPos, structureSpawnData.YPos, structureSpawnData.ZPos);
        }
    }

    public class StructureManager
    {
        public static StructureManager Instance { get; } = new StructureManager();

        public Dictionary<int, Structure> Structures = new Dictionary<int, Structure>();
        public Dictionary<int, StructureData> StructureDataDic = new Dictionary<int, StructureData>();
        List<StructureSpawnData> StructureSpawnDateList = new List<StructureSpawnData>();
        private ushort _structurerId = 0;

        private const string StructureDataPath = "StructureData.csv";
        private const string StructureSpawnDataPath = "StructureSpawnData.csv";

        public void InitData()
        {
            StructureDataDic = LoadStructureData(StructureDataPath);
            StructureSpawnDateList = LoadStructureSpawnData(StructureSpawnDataPath);

            foreach (StructureSpawnData spawnData in StructureSpawnDateList)
            {
                Spawn(spawnData);
            }
        }

        public void Add(Structure structure)
        {
            _structurerId++;
            Structures.Add(_structurerId, structure);
            SpatialGrid.Instance.AddStructure(structure);
        }

        public void Spawn(StructureSpawnData spawnData)
        {
            Structure structure = new Structure(StructureDataDic[spawnData.Type], spawnData);

            Add(structure);

            Console.WriteLine($"Structure Spawned: {_structurerId}  {StructureDataDic[_structurerId].Type}");
        }
    }
}
