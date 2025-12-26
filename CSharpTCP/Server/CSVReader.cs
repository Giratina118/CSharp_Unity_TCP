using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Server
{
    public class CSVReader
    {
        enum MonsterKey
        {
            Ray = 1001,
            Bee = 2001,
        }

        public struct MonsterData
        {
            public int Type;    // 어떤 종류의 몬스터인지
            public string Name; // 이름
            public int Hp;      // 체력
            public float Speed; // 속도
            public int Damage;  // 데미지
            public int Point;   // 적 처치 시 점수
        }

        public struct MonsterSpawnData
        {
            public int Type;
            public float xPos;
            public float yPos;
            public float zPos;
            public float xRot;
            public float yRot;
            public float zRot;
        }

        public struct StructureData
        {
            public int Type;
            public float XSize; // 크기
            public float YSize;
            public float ZSize;
        }

        public struct StructureSpawnData
        {
            public int Type;
            public float XPos; // 위치
            public float YPos;
            public float ZPos;
        }


        public static List<T> Load<T>(string path, int skipLineCount, Func<string[], T> parser)
        {
            List<T> result = new List<T>();

            // 자원이 끝나면 자동으로 Dispose() 호출
            using (StreamReader reader = new StreamReader(path))
            {
                for (int i = 0; i < skipLineCount; i++) // 컬럼명 건너뛰기
                    reader.ReadLine();

                while (!reader.EndOfStream) // 파일 끝까지 반복
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) // 빈칸 건너뛰기
                        continue;

                    string[] tokens = line.Split(',');
                    result.Add(parser(tokens)); // parser: string[]를 T로 반환시키는 함수
                }
            }

            return result;
        }

        // 몬스터 데이터
        public static Dictionary<int, MonsterData> LoadMonsterData(string path)
        {
            Dictionary<int, MonsterData> result = new Dictionary<int, MonsterData>();

            var list = Load(path, 3, row => new MonsterData
            {
                Type = int.Parse(row[1]),
                Name = row[2],
                Hp = int.Parse(row[3]),
                Speed = float.Parse(row[4]),
                Damage = int.Parse(row[5]),
                Point = int.Parse(row[6])
            });

            foreach (var data in list)
            {
                if (result.ContainsKey(data.Type))
                    throw new Exception($"중복 Monster_Number: {data.Type}");

                result.Add(data.Type, data); // Type == Monster_Number
            }

            return result;
        }

        // 몬스터 스폰 데이터
        public static List<MonsterSpawnData> LoadMonsterSpawnData(string path)
        {
            return Load(path, 3, row => new MonsterSpawnData
            {
                Type = int.Parse(row[1]),
                xPos = float.Parse(row[3]),
                yPos = float.Parse(row[4]),
                zPos = float.Parse(row[5]),
                xRot = float.Parse(row[6]),
                yRot = float.Parse(row[7]),
                zRot = float.Parse(row[8])
            });
        }


        // 건물 데이터
        public static Dictionary<int, StructureData> LoadStructureData(string path)
        {
            Dictionary<int, StructureData> result = new Dictionary<int, StructureData>();

            var list = Load(path, 3, row => new StructureData
            {
                Type = int.Parse(row[1]),
                XSize = float.Parse(row[3]),
                YSize = float.Parse(row[4]),
                ZSize = float.Parse(row[5]),
            });

            foreach (var data in list)
            {
                if (result.ContainsKey(data.Type))
                    throw new Exception($"중복 Structure_Number: {data.Type}");

                result.Add(data.Type, data); // Type == Structure_Number
            }

            return result;
        }

        // 건물 스폰 데이터
        public static List<StructureSpawnData> LoadStructureSpawnData(string path)
        {
            return Load(path, 3, row => new StructureSpawnData
            {
                Type = int.Parse(row[1]),
                XPos = float.Parse(row[3]),
                YPos = float.Parse(row[4]),
                ZPos = float.Parse(row[5])
            });
        }
    }
}
