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
        public ushort Type;  // 종류
        public Vector3 Size; // 크기
        public Vector3 Pos;  // 위치

        public Structure(StructureData structureData, StructureSpawnData structureSpawnData)
        {
            Type = (ushort)structureData.Type;
            Size = new Vector3(structureData.XSize, structureData.YSize, structureData.ZSize);
            Pos = new Vector3(structureSpawnData.XPos, structureSpawnData.YPos, structureSpawnData.ZPos);
        }
    }

    public class StructureManager
    {
        public static StructureManager Instance { get; } = new StructureManager();

        public List<Structure> Structures = new List<Structure>(); // 건물 목록
        public Dictionary<int, StructureData> StructureDataDic = new Dictionary<int, StructureData>(); // 건물 정보(파싱받은 데이터)
        public List<StructureSpawnData> StructureSpawnDateList = new List<StructureSpawnData>();       // 건물 스폰 정보(파싱받은 데이터)

        private const string StructureDataPath = "StructureData.csv";           // 건물 정보 파싱 파일명
        private const string StructureSpawnDataPath = "StructureSpawnData.csv"; // 건물 스폰 정보 파싱 파일명

        // 건물 정보 초기화
        public void InitData()
        {
            StructureDataDic = LoadStructureData(StructureDataPath);
            StructureSpawnDateList = LoadStructureSpawnData(StructureSpawnDataPath);

            foreach (StructureSpawnData spawnData in StructureSpawnDateList)
                Spawn(spawnData);
        }

        // 건물 추가
        public void Add(Structure structure)
        {
            Structures.Add(structure);
            SpatialGrid.Instance.AddStructure(structure);
        }

        // 건물 스폰
        public void Spawn(StructureSpawnData spawnData)
        {
            Structure structure = new Structure(StructureDataDic[spawnData.Type], spawnData);
            Add(structure);
        }
    }
}
