using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Monster
    {
        public ushort _id;     // 몬스터 고유 번호
        private ushort _maxHP;  // 최대 체력
        private ushort _curHP;  // 현재 체력
        private float _speed;   // 이동 속도
        private ushort _damage; // 공격력
        private Vector3 _pos;   // 위치
        private Vector3 _rot;   // 회전

        public Monster()
        {

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

        Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>();
        private ushort _monsterId = 0;

        public bool IsSpawning = false;
        float _spawnInterval = 5f;   // 5초
        float _remainTime = 5f;

        public bool Write(Span<byte> span, ref ushort count)
        {
            bool success = true;

            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), (ushort)_monsters.Count);
            count += sizeof(ushort); // 딕셔너리 길이
            foreach (Monster monster in _monsters.Values)
                success &= monster.Write(span, ref count);

            return success;
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
            _monsters.Add(_monsterId, monster);
        }

        public void Update(float deltaTime)
        {
            if (SessionManager.Instance.Sessions.Count == 0)
                return;

            _remainTime -= deltaTime;

            if (_remainTime <= 0f)
            {
                Spawn();
                _remainTime = _spawnInterval;
            }
        }

        void Spawn()
        {
            Monster monster = new Monster
            {
                _id = _monsterId,
                // 정보들 넣기
            };

            _monsters.Add(monster._id, monster);

            Console.WriteLine($"Monster Spawned: {monster._id}");
        }

    }
}