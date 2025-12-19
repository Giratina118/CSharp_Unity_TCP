using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Missile
    {
        public Vector3 _pos;
        public Vector3 _dir;
        private float _speed = 10.0f;
        public ushort Damage = 10;
        public float Radius = 0.05f;

        public void Update(float deltaTime)
        {
            Vector3 prev = _pos;
            _pos += _dir * _speed * deltaTime;
        }
    }

    public class MissileManager
    {
        public static MissileManager Instance { get; } = new MissileManager();
        public Dictionary<int, Missile> Missiles = new Dictionary<int, Missile>();
        private int _missilesId = 0;
        private Vector3 _muzzle = new Vector3(0.0f, 1.0f, 1.75f);

        public void Update(float deltaTime)
        {
            foreach (var missile in Missiles.Values.ToList())
            {
                Vector3 prevPos = missile._pos;

                missile.Update(deltaTime); // 위치 갱신

                //Console.WriteLine($"미사일 업데이트 {prevPos},  {missile._pos},  {missile}");

                // 충돌 체크
                CollisionSystem.Instance.MissileCollisionCheck(prevPos, missile._pos, missile);
            }
        }

        const float PI = 3.141592f;

        public void Add(ObjectInfo Info)
        {
            Console.WriteLine($"위치: {Info.position + _muzzle},  방향: {Info.rotation}, {{{MathF.Sin(Info.rotation.Y / 180.0f * PI)}, {0}, {MathF.Cos(Info.rotation.Y / 180.0f * PI)}}}");
            Vector3 dir = new Vector3(MathF.Sin(Info.rotation.Y / 180.0f * PI), 0.0f, MathF.Cos(Info.rotation.Y / 180.0f * PI));
            Missile newMissile = new Missile() { _pos = Info.position + _muzzle, _dir = dir };
            Missiles.Add(_missilesId++, newMissile);
        }

        public void Remove()
        {
            Missiles.Remove(_missilesId);
        }
    }
}
