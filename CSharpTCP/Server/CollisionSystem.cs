using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class CollisionSystem
    {
        public static CollisionSystem Instance { get; } = new CollisionSystem();

        private SpatialGrid _grid = SpatialGrid.Instance;

        // 미사일이 이동한 선분(from -> to) 기반으로 충돌 검사
        public void MissileCollisionCheck(Vector3 prevPos, Vector3 currPos, Missile missile)
        {
            // 겹치는 셀 목록
            List<GridCell> cells = _grid.QueryBySegment(prevPos, currPos);

            Monster? targetMonster = null;
            ClientSession? targetPlayer = null;

            // 셀 안 오브젝트 충돌 검사
            foreach (GridCell cell in cells)
            {
                float minDistance = 100000.0f;

                foreach (Monster monster in cell.Monsters) // 몬스터 충돌
                {
                    float radius = monster.Radius + missile.Radius;
                    float distance = CollisionLineSphere(prevPos, currPos, monster._pos, radius);
                    if (distance > 0.0f && distance < minDistance)
                    {
                        targetMonster = monster;
                        minDistance = distance;
                        //OnHit(missile, monster);
                        //return;
                    }
                }

                foreach (ClientSession player in cell.Players) // 플레이어 충돌
                {
                    float radius = player.CollisionRadius + missile.Radius;
                    float distance = CollisionLineSphere(prevPos, currPos, player.Info.position, radius);
                    if (distance > 0.0f && distance < minDistance && missile.ShooterId != player.Info.id)
                    {
                        targetPlayer = player;
                        minDistance = distance;
                        //OnHit(missile, player);
                        //return;
                    }
                }
            }

            // 가장 가까운 충돌만 체크
            if (targetPlayer != null)
            {
                OnHit(missile, targetPlayer);
            }
            else if (targetMonster != null)
            {
                OnHit(missile, targetMonster);
            }

        }

        // 선분, 구 충돌
        private float CollisionLineSphere(Vector3 missilePrevPos, Vector3 missileCurrPos, Vector3 center, float radius)
        {
            // 선분 벡터와 점까지 벡터
            Vector3 missileLine = missileCurrPos - missilePrevPos;
            Vector3 centerToMissile = center - missilePrevPos;

            // 투영 계수 t 계산, 어느 지점에 위치하는지
            // t = 그림자 길이 / 선분 전체 길이
            //   = |centerToMissile| cosθ / |missileLine|
            //   = |centerToMissile||missileLine| cosθ / |missileLine||missileLine|
            //   = Dot(centerToMissile, missileLine) / missileLine.LengthSquared()
            float t = Vector3.Dot(centerToMissile, missileLine) / missileLine.LengthSquared();
            t = Math.Clamp(t, 0.0f, 1.0f); // 선분 범위로 제한

            // 수선의 발 좌표
            Vector3 foot = missilePrevPos + t * missileLine;

            // 점과 수선의 발 사이 거리
            float distance = Vector3.Distance(center, foot);

            //Console.WriteLine("수선의 발 좌표: " + foot);
            //Console.WriteLine("점에서 선분까지 거리: " + distance);

            if (distance < radius)
                return distance;  // 충돌 O
            else
                return -1; // 충돌 X
        }

        // 몬스터 미사일 충돌 후 처리
        private void OnHit(Missile missile, Monster monster)
        {
            missile.IsRemoved = true;
            monster.Hit(missile.Damage);
            Console.WriteLine($"몬스터 맞춤 {monster._id}, {monster._pos}");

            // 몬스터 데미지 처리
            // 미사일 제거
            //missile.IsAlive = false;
        }

        // 플레이어 미사일 충돌 후 처리
        private void OnHit(Missile missile, ClientSession player)
        {
            missile.IsRemoved = true;
            Console.WriteLine("플레이어 맞춤");
            // 플레이어 데미지 처리
            // 미사일 제거
            //missile.IsAlive = false;
        }
    }
}