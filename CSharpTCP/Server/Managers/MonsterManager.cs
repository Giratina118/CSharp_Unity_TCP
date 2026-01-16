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

        private Vector3 _spawnPos;  // 스폰 위치
        private Vector3 _targetPos; // 이동 위치
        private Vector3 _moveDir;   // 이동 방향
        private float _moveInterval = 3.0f;     // 이동 변경 간격
        private float _moveTimer = 0.0f;        // 이동 변경 타이머
        private float _respawnInterval = 10.0f; // 리스폰 간격
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
        
        // 업데이트(이동/리스폰)
        public void Update(float deltaTime)
        {
            if (IsDie) Respawn(deltaTime); // 죽은 상태면 리스폰 대기
            else       Move(deltaTime);    // 살아있으면 이동
        }

        // 리스폰
        public void Respawn(float deltaTime)
        {
            _respawnTimer += deltaTime;
            if (_respawnTimer > _respawnInterval)
            {
                _respawnTimer = 0.0f;
                IsDie = false;

                // 모든 클라에게 리스폰 전송
                CreateRemovePacket monsterPacket = new CreateRemovePacket() { MessageType = (ushort)MsgType.RespawnMonster, Id = Id };
                ArraySegment<byte> monsterSegment = monsterPacket.Write();
                SessionManager.Instance.BroadcastAll(monsterSegment);
            }
        }

        // 이동
        public void Move(float deltaTime)
        {
            _moveTimer += deltaTime;
            if (_moveTimer > _moveInterval) // 일정 시간마다 목적지 재설정
            {
                _moveTimer = 0.0f;

                Random rand = new Random();
                float x = _spawnPos.X + rand.NextSingle() * _moveRange - _moveRange * 0.5f;
                float z = _spawnPos.Z + rand.NextSingle() * _moveRange - _moveRange * 0.5f;

                _targetPos = new Vector3(x, Pos.Y, z);
            }

            float distance = Vector3.Distance(Pos, _targetPos);
            if (distance > 0.1f) // 목적지에 다다르면 이동X
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
            CreateRemovePacket monsterPacket = new CreateRemovePacket() { MessageType = (ushort)MsgType.RemoveMonster, Id = Id };
            ArraySegment<byte> monsterSegment = monsterPacket.Write();
            SessionManager.Instance.BroadcastAll(monsterSegment);

            SessionManager.Instance.Sessions[shooterId].Heal();
        }
    }


    public class MonsterManager
    {
        public static MonsterManager Instance { get; } = new MonsterManager();

        public Dictionary<int, Monster> Monsters = new Dictionary<int, Monster>(); // 몬스터 목록
        public Dictionary<int, MonsterData> MonsterDataDic = new Dictionary<int, MonsterData> (); // 몬스터 정보(파싱받은 데이터)
        public List<MonsterSpawnData> MonsterSpawnDateList = new List<MonsterSpawnData> ();       // 몬스터 스폰 정보(파싱받은 데이터)

        private ushort _monsterId = 0; // 몬스터 id
        private const string MonsterDataPath = "MonsterData.csv";           // 몬스터 정보 파싱 파일명
        private const string MonsterSpawnDataPath = "MonsterSpawnData.csv"; // 몬스터 스폰 정보 파싱 파일명

        // 몬스터 정보 초기화
        public void InitData()
        {
            MonsterDataDic = LoadMonsterData(MonsterDataPath);
            MonsterSpawnDateList = LoadMonsterSpawnData(MonsterSpawnDataPath);

            foreach (MonsterSpawnData spawnData in MonsterSpawnDateList)
                Spawn(spawnData);
        }

        // 몬스터 추가
        public void Add(Monster monster)
        {
            _monsterId++;
            monster.Id = _monsterId;
            Monsters.Add(_monsterId, monster);
            SpatialGrid.Instance.AddMonster(monster);
        }

        // 몬스터 업데이트
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
                ObjectInfo monsterInfo = new ObjectInfo() { Id = monster.Id, ObjType = (ushort)ObjType.Monster, Position = monster.Pos, Rotation = monster.Rot };
                monsterInfoList.Add(monsterInfo);
            }

            // 몬스터 위치 정보 클라에 전송
            ObjListPacket monsterPacket = new ObjListPacket() { MessageType = (ushort)MsgType.MonsterInfoList, Infos = monsterInfoList };
            ArraySegment<byte> monsterSegment = monsterPacket.Write();
            SessionManager.Instance.BroadcastAll(monsterSegment); 
        }

        // 몬스터 스폰
        public void Spawn(MonsterSpawnData spawnData)
        {
            Monster monster = new Monster(spawnData);
            Add(monster);
        }
    }
}