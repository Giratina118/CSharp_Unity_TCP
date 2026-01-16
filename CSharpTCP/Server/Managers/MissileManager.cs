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
        public Vector3 CreatedPos;    // 생성 위치
        public Vector3 Pos;           // 현재 위치
        public Vector3 Dir;           // 날아가는 방향
        public ushort Damage = 10;    // 데미지
        public float Radius = 0.1f;   // 충돌 반지름
        public bool IsRemoved = false;// 충돌되어 제거된 상태인지

        public DateTime SpawnTime;    // 생성 시각
        public float LifeTime = 10.0f;// 생존 시간

        private float _speed = 15.0f; // 미사일 속도

        // 생존 시간 끝났는지
        public bool IsLifeTimeOver()
        {
            return (DateTime.UtcNow - SpawnTime).TotalSeconds >= LifeTime;
        }

        // 업데이트(위치)
        public void Update(float deltaTime)
        {
            Vector3 prev = Pos;
            Pos += Dir * _speed * deltaTime;
        }
    }

    public class MissileManager
    {
        public static MissileManager Instance { get; } = new MissileManager();

        public List<Missile> Missiles = new List<Missile>(); // 미사일 리스트

        const float PI = 3.141592f; // 원주율
        private Vector3 _muzzle = new Vector3(0.0f, 0.5f, 0.0f); // 포구

        // 업데이트(이동, 접촉, 삭제)
        public void Update(float deltaTime)
        {
            for (int i = Missiles.Count - 1; i >= 0; i--)
            {
                Missile missile = Missiles[i];

                if (missile.IsLifeTimeOver() || missile.IsRemoved) // 생존 시간 다 된건 삭제
                {
                    Missiles.RemoveAt(i);
                    continue;
                }

                Vector3 prevPos = missile.Pos;
                missile.Update(deltaTime); // 개별 업데이트(이동)

                CollisionSystem.Instance.MissileCollisionCheck(prevPos, missile.Pos, missile); // 접촉 체크
            }
        }

        // 새 미사일 추가
        public void Add(ObjectInfo Info)
        {
            Console.WriteLine($"위치: {Info.Position + _muzzle},  방향: {Info.Rotation}, {{{MathF.Sin(Info.Rotation.Y / 180.0f * PI)}, {0}, {MathF.Cos(Info.Rotation.Y / 180.0f * PI)}}}");
            Vector3 dir = new Vector3(MathF.Sin(Info.Rotation.Y / 180.0f * PI), 0.0f, MathF.Cos(Info.Rotation.Y / 180.0f * PI));
            Missile newMissile = new Missile() { ShooterId = Info.Id, CreatedPos = Info.Position + _muzzle, Pos = Info.Position + _muzzle, Dir = dir, SpawnTime = DateTime.UtcNow };
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
