using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Server.CSVReader;

namespace Server
{
    public class Monster
    {
        public ushort Id;     // 몬스터 id
        public ushort Type;   // 몬스터 고유 번호
        public ushort MaxHP;  // 최대 체력
        public ushort CurHP;  // 현재 체력
        public float Speed;   // 이동 속도
        public ushort Damage; // 공격력
        public ushort Point;  // 처치 시 획득 점수
        public Vector3 Pos;   // 위치
        public Vector3 Rot;   // 회전
        public float Radius;  // 반지름
        public bool IsDie;    // 사망 여부

        private Vector3 _spawnPos;
        private Vector3 _targetPos;
        private Vector3 _moveDir;
        private float _moveInterval = 3.0f; // 리스폰 간격
        private float _moveTimer = 0.0f; // 리스폰 타이머
        private float _respawnInterval = 20.0f; // 리스폰 간격
        private float _respawnTimer = 0.0f; // 리스폰 타이머

        public Monster()
        {
            CurHP = MaxHP = 100;
            Speed = 2.0f;
            Damage = 5;
            Rot = Vector3.Zero;
            Radius = 1.0f;
            IsDie = false;
        }

        public Monster(MonsterSpawnData spawnData)
        {
            Type = (ushort)spawnData.Type;
            MonsterData monsterData = MonsterManager.Instance.MonsterDataDic[spawnData.Type];
            MaxHP = CurHP = (ushort)monsterData.Hp;
            Speed = monsterData.Speed;
            Damage = (ushort)monsterData.Damage;
            Point = (ushort)monsterData.Point;
            Pos = new Vector3(spawnData.xPos, spawnData.yPos, spawnData.zPos);
            Rot = new Vector3(spawnData.xRot, spawnData.yRot, spawnData.zRot);
            Radius = 1.5f;
            IsDie = false;
            _spawnPos = Pos;
        }
        
        
        public void Update(float deltaTime)
        {
            if (IsDie)
            {
                _respawnTimer += deltaTime;
                if (_respawnTimer > _respawnInterval)
                {
                    _respawnTimer = 0.0f;
                    IsDie = false;
                    Pos = _spawnPos;

                    // TODO: 모든 클라에게 리스폰 전송

                }
            }
            else
            {
                Move(deltaTime);
            }
        }

        public void Move(float deltaTime)
        {
            // 일정 시간마다 갱신
            _moveTimer += deltaTime;
            if (_moveTimer > _moveInterval)
            {
                _moveTimer = 0.0f;

                Random rand = new Random();
                float x = Pos.X + rand.Next(0, 11) - 5;
                float z = Pos.Z + rand.Next(0, 11) - 5;
                _targetPos = new Vector3(x, 0, z);
                _moveDir = _targetPos - Pos;

                float vecLen = MathF.Sqrt(_moveDir.X * _moveDir.X + _moveDir.Z * _moveDir.Z);
                _moveDir.X /= vecLen;
                _moveDir.Y = 0.0f;
                _moveDir.Z /= vecLen;
            }

            Pos += _moveDir;
            
        }

        // 피격
        public void Hit(ushort dmg)
        {
            // TODO: 매개변수로 발사한 사람이 누군지도 같이 받아오기(shooter)
            // TODO: 몬스터 소멸 시 모든 클라에게 누가 쓰러뜨렸는지 전달, 클라에서는 받아서 알림창에 띄움
            // TODO: 몬스터 소멸 시 쓰러뜨린 클라에게 점수 추가(서버 내부에서), 점수는 1초에 1번씩 각 클라에게 전송
            // TODO: 몬스터 소멸 시 해당 위치에 새로운 회복 아이템 생성
            Console.WriteLine("맞음");

            if (CurHP <= dmg) // 체력 0 시 소멸
            {
                CurHP = 0;
                Die();
            }
            else
                CurHP -= dmg;
        }

        // 소멸
        private void Die()
        {
            IsDie = true;
            // TODO: 모든 클라에게 소멸 전송

            //SpatialGrid.Instance.RemoveMonster(this);
        }
    }

    public class MonsterManager
    {
        public static MonsterManager Instance { get; } = new MonsterManager();

        public Dictionary<int, Monster> Monsters = new Dictionary<int, Monster>();
        public Dictionary<int, MonsterData> MonsterDataDic = new Dictionary<int, MonsterData> ();
        List<MonsterSpawnData> MonsterSpawnDateList = new List<MonsterSpawnData> ();

        private ushort _monsterId = 0;

        public bool IsSpawning = false;
        float _spawnInterval = 5.0f;
        float _spawnTimer = 5.0f;

        private const string MonsterDataPath = "MonsterData.csv";
        private const string MonsterSpawnDataPath = "MonsterSpawnData.csv";

        /*
        public bool Write(Span<byte> span, ref ushort count)
        {
            bool success = true;

            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), (ushort)Monsters.Count);
            count += sizeof(ushort); // 딕셔너리 길이
            foreach (Monster monster in Monsters.Values)
                success &= monster.Write(span, ref count);

            return success;
        }

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

        // 몬스터 정보 초기화
        public void InitData()
        {
            MonsterDataDic = LoadMonsterData(MonsterDataPath);
            MonsterSpawnDateList = LoadMonsterSpawnData(MonsterSpawnDataPath);

            foreach (MonsterSpawnData spawnData in MonsterSpawnDateList)
            {
                Spawn(spawnData);
            }
        }

        public void Add(Monster monster)
        {
            _monsterId++;
            monster.Id = _monsterId;
            Monsters.Add(_monsterId, monster);
            SpatialGrid.Instance.AddMonster(monster);
        }

        public void Update(float deltaTime)
        {
            if (SessionManager.Instance.Sessions.Count == 0)
                return;

            foreach (Monster monster in Monsters.Values)
            {
                Vector3 prev = monster.Pos;

                monster.Update(deltaTime);

                
                SpatialGrid.Instance.UpdateMonster(monster, prev); // ⭐
            }

            // TODO: 몬스터 위치 정보 클라에 전송
            MovePacket movePacket = new MovePacket() { messageType = (ushort)MsgType.MoveMonster, playerInfo = new ObjectInfo { id } };
            ArraySegment<byte> chatSegment = movePacket.Write();
            BroadcastAll(chatSegment);


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

            Console.WriteLine($"Monster Spawned: {_monsterId}  {MonsterDataDic[monster.Type].Name}");
        }
    }
}