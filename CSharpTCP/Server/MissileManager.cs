using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Missile
    {
        public long ShooterId;        // 발사한 플레이어 id
        public Vector3 Pos;           // 생성 위치
        public Vector3 Dir;           // 날아가는 방향
        public ushort Damage = 10;    // 데미지
        public float Radius = 0.05f;  // 충돌 반지름
        public bool IsRemoved = false;// 충돌되어 제거된 상태인지

        public DateTime SpawnTime;   // 생성 시각
        public float LifeTime = 10.0f; // 생명 시간

        private float _speed = 10.0f; // 미사일 속도

        public bool IsLifeTimeOver => (DateTime.UtcNow - SpawnTime).TotalSeconds >= LifeTime;

        public void Update(float deltaTime)
        {
            Vector3 prev = Pos;
            Pos += Dir * _speed * deltaTime;
            Console.WriteLine($"위치: {Pos}");
        }
    }

    public class MissileManager
    {
        public static MissileManager Instance { get; } = new MissileManager();
        public List<Missile> Missiles = new List<Missile>();
        private int _missilesId = 0;
        private Vector3 _muzzle = new Vector3(0.0f, 1.0f, 1.75f);

        public void Update(float deltaTime)
        {
            foreach (Missile missile in Missiles)
            {
                if (missile.IsRemoved)
                    continue;

                if (missile.IsLifeTimeOver)
                {
                    Remove(missile);
                    continue;
                }

                Vector3 prevPos = missile.Pos;

                missile.Update(deltaTime); // 위치 갱신

                //Console.WriteLine($"미사일 업데이트 {prevPos},  {missile._pos},  {missile}");

                // 충돌 체크
                CollisionSystem.Instance.MissileCollisionCheck(prevPos, missile.Pos, missile);
            }
        }

        const float PI = 3.141592f;

        // 새 미사일 추가
        public void Add(ObjectInfo Info)
        {
            Console.WriteLine($"위치: {Info.position + _muzzle},  방향: {Info.rotation}, {{{MathF.Sin(Info.rotation.Y / 180.0f * PI)}, {0}, {MathF.Cos(Info.rotation.Y / 180.0f * PI)}}}");
            Vector3 dir = new Vector3(MathF.Sin(Info.rotation.Y / 180.0f * PI), 0.0f, MathF.Cos(Info.rotation.Y / 180.0f * PI));
            Missile newMissile = new Missile() { ShooterId = Info.id, Pos = Info.position + _muzzle, Dir = dir };
            Missiles.Add(newMissile);
        }

        // 미사일 제거
        public void Remove(Missile missile)
        {
            missile.IsRemoved = true;
            Missiles.Remove(missile);
        }
    }
}
