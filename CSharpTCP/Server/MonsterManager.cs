using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Server.CSVReader;

namespace Server
{
    public class Monster
    {
        public ushort _id;     // 몬스터 고유 번호
        private ushort _maxHP; // 최대 체력
        private ushort _curHP; // 현재 체력
        private float _speed;  // 이동 속도
        private ushort _damage;// 공격력
        private ushort _point; // 처치 시 획득 점수
        public Vector3 _pos;   // 위치
        public Vector3 _rot;   // 회전

        public Monster()
        {
            _curHP = _maxHP = 100;
            _speed = 2.0f;
            _damage = 5;
            _rot = Vector3.Zero;
        }

        public Monster(MonsterSpawnData spawnData)
        {
            _id = (ushort)spawnData.Type;
            MonsterData monsterData = MonsterManager.Instance.monsterData[spawnData.Type];
            _maxHP = _curHP = (ushort)monsterData.Hp;
            _speed = monsterData.Speed;
            _damage = (ushort)monsterData.Damage;
            _point = (ushort)monsterData.Point;
            _pos = new Vector3(spawnData.xPos, spawnData.yPos, spawnData.zPos);
            _rot = new Vector3(spawnData.xRot, spawnData.yRot, spawnData.zRot);
        }
        
        public bool Write(Span<byte> span, ref ushort count)
        {
            bool success = true;

            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this._id);
            count += sizeof(ushort); // 고유 번호
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this._maxHP);
            count += sizeof(ushort); // 최대 체력
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this._curHP);
            count += sizeof(ushort); // 현재 체력
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this._speed);
            count += sizeof(float);  // 이동 속도
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this._damage);
            count += sizeof(ushort); // 공격력

            success &= Utils.WriteVector3(_pos, ref span, ref count); // 이동
            success &= Utils.WriteVector3(_rot, ref span, ref count); // 회전

            return success;
        }

        public void Read(ReadOnlySpan<byte> span, ref ushort count)
        {
            _id = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 고유 번호
            _maxHP = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 최대 체력
            _curHP = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 현재 체력
            _speed = BitConverter.ToSingle(span.Slice(count, span.Length - count));
            count += sizeof(float);  // 이동 속도
            _damage = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 공격력

            Utils.ReadVector3(ref _pos, span, ref count); // 이동
            Utils.ReadVector3(ref _rot, span, ref count); // 회전
        }
        
        
        public void Update()
        {

        }

        public void Move()
        {

        }

        public void Attack()
        {

        }

        public void Hit(ushort dmg)
        {
            if (_curHP <= dmg)
            {
                _curHP = 0;
                Die();
            }
            else
                _curHP -= dmg;
        }

        private void Die()
        {

        }
    }

    public class MonsterManager
    {
        public static MonsterManager Instance { get; } = new MonsterManager();

        public Dictionary<int, Monster> Monsters = new Dictionary<int, Monster>();
        public Dictionary<int, MonsterData> monsterData = new Dictionary<int, MonsterData> ();
        List<MonsterSpawnData> monsterSpawnDate = new List<MonsterSpawnData> ();

        private ushort _monsterId = 0;

        public bool IsSpawning = false;
        float _spawnInterval = 5f;   // 5초
        float _remainTime = 5f;

        private const string MonsterDataPath = "MonsterData.csv";
        private const string MonsterSpawnDataPath = "MonsterSpawnData.csv";


        public bool Write(Span<byte> span, ref ushort count)
        {
            bool success = true;

            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), (ushort)Monsters.Count);
            count += sizeof(ushort); // 딕셔너리 길이
            foreach (Monster monster in Monsters.Values)
                success &= monster.Write(span, ref count);

            return success;
        }

        // 몬스터 정보 초기화
        public void InitData()
        {
            monsterData = LoadMonsterData(MonsterDataPath);
            monsterSpawnDate = LoadMonsterSpawnData(MonsterSpawnDataPath);

            foreach (MonsterSpawnData spawnData in monsterSpawnDate)
            {
                Spawn(spawnData);
            }
        }

        /*
        public void Read(ReadOnlySpan<byte> span, ref ushort count)
        {
            ushort monsterLen = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort);
            for (int i = 0; i < monsterLen; i++)
            {
                Monster monster = new Monster();
                monster.Read(span, ref count);
                
            }
        }
        */

        public void Add(Monster monster)
        {
            _monsterId++;
            Monsters.Add(_monsterId, monster);
        }

        public void Update(float deltaTime)
        {
            if (SessionManager.Instance.Sessions.Count == 0)
                return;

            /*
            _remainTime -= deltaTime;

            if (_remainTime <= 0f)
            {
                Spawn();
                _remainTime = _spawnInterval;
            }
            */
        }

        public void Spawn(MonsterSpawnData spawnData)
        {
            Monster monster = new Monster(spawnData);

            Add(monster);

            Console.WriteLine($"Monster Spawned: {_monsterId}  {monsterData[monster._id].Name}");
        }
    }
}