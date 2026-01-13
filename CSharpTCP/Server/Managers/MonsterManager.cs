using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
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
        private float _moveInterval = 3.0f;     // 이동 변경 간격
        private float _moveTimer = 0.0f;        // 이동 변경 타이머
        private float _respawnInterval = 5.0f; // 리스폰 간격
        private float _respawnTimer = 0.0f;     // 리스폰 타이머
        private float _moveRange = 20.0f;       // 이동 반경

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
                Respawn(deltaTime);
            }
            else
            {
                Move(deltaTime);
            }
        }

        // 리스폰
        public void Respawn(float deltaTime)
        {
            _respawnTimer += deltaTime;
            if (_respawnTimer > _respawnInterval)
            {
                _respawnTimer = 0.0f;
                IsDie = false;

                // TODO: 모든 클라에게 리스폰 전송
                CreateRemovePacket monsterPacket = new CreateRemovePacket() { messageType = (ushort)MsgType.RespawnMonster, id = Id };
                ArraySegment<byte> monsterSegment = monsterPacket.Write();
                SessionManager.Instance.BroadcastAll(monsterSegment);
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
                float x = _spawnPos.X + rand.NextSingle() * _moveRange - _moveRange * 0.5f;
                float z = _spawnPos.Z + rand.NextSingle() * _moveRange - _moveRange * 0.5f;

                _targetPos = new Vector3(x, Pos.Y, z);
            }

            float distance = Vector3.Distance(Pos, _targetPos);

            // 목적지에 아주 가깝지 않을 때만 이동
            if (distance > 0.1f)
            {
                _moveDir = Vector3.Normalize(_targetPos - Pos);
                Pos += _moveDir * deltaTime * Speed;

                if (Vector3.Distance(Pos, _targetPos) < 0.1f) 
                    Pos = _targetPos;
            }
        }

        // 피격
        public void Hit(ushort dmg, long shooterId)
        {
            // TODO: 매개변수로 발사한 사람이 누군지도 같이 받아오기(shooter)
            // TODO: 몬스터 소멸 시 모든 클라에게 누가 쓰러뜨렸는지 전달, 클라에서는 받아서 알림창에 띄움
            // TODO: 몬스터 소멸 시 쓰러뜨린 클라에게 점수 추가(서버 내부에서), 점수는 1초에 1번씩 각 클라에게 전송
            // TODO: 몬스터 소멸 시 해당 위치에 새로운 회복 아이템 생성
            Console.WriteLine("맞음");

            if (CurHP <= dmg) // 체력 0 시 소멸
            {
                CurHP = 0;
                Die(shooterId);
            }
            else
                CurHP -= dmg;
        }

        // 소멸
        private void Die(long shooterId)
        {
            IsDie = true;
            ScoreManager.Instance.AddScore(shooterId, Point);
            Pos = _targetPos = _spawnPos;
            CurHP = MaxHP;
            // 모든 클라에게 소멸 전송
            CreateRemovePacket monsterPacket = new CreateRemovePacket() { messageType = (ushort)MsgType.RemoveMonster, id = Id };
            ArraySegment<byte> monsterSegment = monsterPacket.Write();
            SessionManager.Instance.BroadcastAll(monsterSegment);

            // TODO: 회복 아이템 생성

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

            List<ObjectInfo> monsterInfoList = new List<ObjectInfo>();

            foreach (Monster monster in Monsters.Values)
            {
                // 몬스터 위치 갱신
                Vector3 prev = monster.Pos;
                monster.Update(deltaTime);
                SpatialGrid.Instance.UpdateMonster(monster, prev);

                // 몬스터 정보 전송할 리스트
                ObjectInfo monsterInfo = new ObjectInfo() { id = monster.Id, objType = (ushort)ObjType.Monster, position = monster.Pos, rotation = monster.Rot };
                monsterInfoList.Add(monsterInfo);
            }

            // 몬스터 위치 정보 클라에 전송
            ObjListPacket monsterPacket = new ObjListPacket() { messageType = (ushort)MsgType.MonsterInfoList, Infos = monsterInfoList };
            ArraySegment<byte> monsterSegment = monsterPacket.Write();
            SessionManager.Instance.BroadcastAll(monsterSegment); 
        }

        public void Spawn(MonsterSpawnData spawnData)
        {
            Monster monster = new Monster(spawnData);

            Add(monster);

            Console.WriteLine($"Monster Spawned: {_monsterId}  {MonsterDataDic[monster.Type].Name}");
        }
    }
}