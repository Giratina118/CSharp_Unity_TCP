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

            Structure? targetStructure = null;
            Monster? targetMonster = null;
            ClientSession? targetPlayer = null;

            float minDistance = 100000.0f;

            // 셀 안 오브젝트 충돌 검사
            foreach (GridCell cell in cells)
            {
                foreach (Structure structure in cell.Structures)
                {
                    float collisionDistance = CollisionLineRect(prevPos, currPos, structure.Pos, structure.Size);

                    if (collisionDistance > 0.0f && collisionDistance < minDistance)
                    {
                        targetStructure = structure;
                        minDistance = collisionDistance;
                    }
                }

                foreach (Monster monster in cell.Monsters) // 몬스터 충돌
                {
                    float radius = monster.Radius + missile.Radius;
                    float collisionDistance = CollisionLineSphere(prevPos, currPos, monster.Pos, radius);
                    float fromFireDistance = Vector3.Distance(missile.CreatedPos, monster.Pos);
                    if (collisionDistance > 0.0f && fromFireDistance < minDistance)
                    {
                        targetMonster = monster;
                        minDistance = fromFireDistance;
                    }
                }

                foreach (ClientSession player in cell.Players) // 플레이어 충돌
                {
                    float radius = player.CollisionRadius + missile.Radius;
                    float collisionDistance = CollisionLineSphere(prevPos, currPos, player.Info.position, radius);
                    float fromFireDistance = Vector3.Distance(missile.CreatedPos, player.Info.position);
                    if (collisionDistance > 0.0f && fromFireDistance < minDistance && missile.ShooterId != player.Info.id)
                    {
                        targetPlayer = player;
                        minDistance = fromFireDistance;
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

        // 선분, 사각형 충돌
        private float CollisionLineRect(Vector3 missilePrevPos, Vector3 missileCurrPos, Vector3 center, Vector3 size)
        {
            // 직선이 사각형과 접하면 충돌, 4개 선 중 하나와는 겹쳐야 한다.
            // 즉 4개의 선(x, y값)을 대입했을때 범위에 들어오면 접함.

            Vector3 dir = missileCurrPos - missilePrevPos;
            float length = dir.Length();

            if (length < 0.0001f)
                return -1.0f;

            Vector3 boxMin = center - size;
            Vector3 boxMax = center + size;

            float tMin = 0.0f;
            float tMax = 1.0f;

            if (!LineCollisionCheck(dir.X, missilePrevPos.X, boxMin.X, boxMax.X, ref tMin, ref tMax)) return -1.0f;
            if (!LineCollisionCheck(dir.Y, missilePrevPos.Y, boxMin.Y, boxMax.Y, ref tMin, ref tMax)) return -1.0f;
            if (!LineCollisionCheck(dir.Z, missilePrevPos.Z, boxMin.Z, boxMax.Z, ref tMin, ref tMax)) return -1.0f;

            if (tMin < 0.0f)
                tMin = 0.0f;

            return length * tMin;
        }

        // 충돌 여부 체크
        private bool LineCollisionCheck(float dir, float start, float min, float max, ref float tMin, ref float tMax)
        {
            if (MathF.Abs(dir) < 0.0001f)
            {
                // 이 축으로 이동이 없으면 범위 안에 있어야 함
                return start >= min && start <= max;
            }

            float t1 = (min - start) / dir;
            float t2 = (max - start) / dir;

            if (t1 > t2)
            {
                float temp = t1;
                t1 = t2;
                t2 = temp;
            }

            tMin = MathF.Max(tMin, t1);
            tMax = MathF.Min(tMax, t2);

            return tMin <= tMax;
        }

        // 몬스터 미사일 충돌 후 처리
        private void OnHit(Missile missile, Monster monster)
        {
            long shooter = missile.ShooterId;

            missile.IsRemoved = true;
            monster.Hit(missile.Damage); // 몬스터 데미지 처리
            Console.WriteLine($"몬스터 맞춤 {monster.Type}, {monster.CurHP}/{monster.MaxHP}");

            MissileManager.Instance.Remove(missile); // 미사일 제거
            // TODO: 몬스터 공격 성공한 플레이어(shooter)에게 공격 성공 전달(전달 요소: 데미지, 피격 대상 위치)

        }

        // 플레이어 미사일 충돌 후 처리
        private void OnHit(Missile missile, ClientSession player)
        {
            missile.IsRemoved = true;
            Console.WriteLine($"플레이어 맞춤 {player.Name}");

            MissileManager.Instance.Remove(missile); // 미사일 제거

            // TODO: 플레이어 피격 처리(서버 내부), 클라이언트 세션에서 체력 관리 및 피격 함수 생성, 거기서 피격당한 플레이어에게 전달
            // TODO: 플레이어 체력에 따라 소멸 처리, 다른 클라들에게 누가 쓰러뜨렸는지 전달, 쓰러뜨린 플레이어는 쓰러진 플레이어가 가지고 있던 점수의 절반 획득
            // TODO: 쓰러진 플레이어는 서버 연결 해제, 최종 점수 표시

            // TODO: 공격 성공한 플레이어(shooter)에게 공격 성공 전달(전달 요소: 데미지, 피격 대상 위치)

            //missile.IsAlive = false;
        }
    }
}