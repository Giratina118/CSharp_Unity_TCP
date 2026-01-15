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

        private SpatialGrid _grid = SpatialGrid.Instance; // 전체 맵을 작은 구역들로 나눠서 검사 

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
                foreach (Structure structure in cell.Structures) // 건물 충돌 체크
                {
                    Vector3? collisionPos = CollisionLineRect(prevPos, currPos, structure.Pos, structure.Size);

                    if (collisionPos == null)
                        continue;

                    float distanceFromCreatedPos = Vector3.Distance(missile.CreatedPos, collisionPos.Value);

                    if (distanceFromCreatedPos < minDistance)
                    {
                        Console.WriteLine($"건물 {distanceFromCreatedPos}");
                        targetStructure = structure;
                        minDistance = distanceFromCreatedPos;
                    }
                }

                foreach (Monster monster in cell.Monsters) // 몬스터 충돌 체크
                {
                    if (monster.IsDie)
                        continue;

                    float combinedRadius = monster.Radius + missile.Radius;
                    Vector3? collisionPos = CollisionLineSphere(prevPos, missile.Pos, monster.Pos, combinedRadius);
                    if (collisionPos == null)
                        continue;

                    float distanceFromCreatedPos = Vector3.Distance(missile.CreatedPos, collisionPos.Value);

                    if (distanceFromCreatedPos < minDistance)
                    {
                        Console.WriteLine($"몬스터 {distanceFromCreatedPos}");
                        targetMonster = monster;
                        minDistance = distanceFromCreatedPos;
                    }
                }

                foreach (ClientSession player in cell.Players) // 플레이어 충돌 체크
                {
                    float radius = player.CollisionRadius + missile.Radius;
                    Vector3? collisionPos = CollisionLineSphere(prevPos, currPos, player.Info.position, radius);

                    if (collisionPos == null)
                        continue;

                    float distanceFromCreatedPos = Vector3.Distance(missile.CreatedPos, collisionPos.Value);

                    if (distanceFromCreatedPos < minDistance && player.Info.id != missile.ShooterId)
                    {
                        Console.WriteLine($"플레이어 {distanceFromCreatedPos}");
                        targetPlayer = player;
                        minDistance = distanceFromCreatedPos;
                    }
                }
            }

            // 가장 가까운 충돌만 체크
            if (targetPlayer != null) // 플레이어 맞음
            {
                Console.WriteLine("플레이어 맞음");
                OnHit(missile, targetPlayer);
            }
            else if (targetMonster != null) // 몬스터 맞음
            {
                Console.WriteLine("몬스터 맞음");
                OnHit(missile, targetMonster);
            }
            else if (targetStructure != null) // 건물 맞음
            {
                Console.WriteLine("건물 맞음");
                MissileManager.Instance.Remove(missile); // 미사일 삭제
            }
        }

        // 선분, 구 충돌
        private Vector3? CollisionLineSphere(Vector3 lineStartPos, Vector3 lineEndPos, Vector3 sphereCenter, float sphereRadius)
        {
            Vector3 missileVector = lineEndPos - lineStartPos;
            float missileLength = missileVector.Length();

            if (missileLength < 0.0001f)
                return null;

            Vector3 missileDir = missileVector / missileLength;
            Vector3 startToCenter = sphereCenter - lineStartPos;

            float projection = Vector3.Dot(startToCenter, missileDir);
            float closestDistSqr = startToCenter.LengthSquared() - projection * projection;
            float radiusSqr = sphereRadius * sphereRadius;

            if (closestDistSqr > radiusSqr)
                return null; // 충돌 없음

            float offset = MathF.Sqrt(radiusSqr - closestDistSqr);
            float hitDistance = projection - offset;

            if (hitDistance < 0.0f)
                hitDistance = 0.0f;
            if (hitDistance > missileLength)
                return null;

            return lineStartPos + missileDir * hitDistance;
        }

        // 선분, 사각형 충돌
        private Vector3? CollisionLineRect(Vector3 lineStartPos, Vector3 lineEndPos, Vector3 boxCenter, Vector3 boxHalfSize)
        {
            Vector3 rayDir = lineEndPos - lineStartPos;
            float rayLength = rayDir.Length();

            if (rayLength < 0.0001f)
                return null;

            rayDir /= rayLength;

            Vector3 boxMin = boxCenter - boxHalfSize;
            Vector3 boxMax = boxCenter + boxHalfSize;

            float tEnter = 0.0f;
            float tExit = rayLength;

            for (int axis = 0; axis < 3; axis++)
            {
                float rayComponent = axis == 0 ? rayDir.X : axis == 1 ? rayDir.Y : rayDir.Z;
                float rayStartComponent = axis == 0 ? lineStartPos.X : axis == 1 ? lineStartPos.Y : lineStartPos.Z;
                float min = axis == 0 ? boxMin.X : axis == 1 ? boxMin.Y : boxMin.Z;
                float max = axis == 0 ? boxMax.X : axis == 1 ? boxMax.Y : boxMax.Z;

                if (MathF.Abs(rayComponent) < 0.0001f)
                {
                    if (rayStartComponent < min || rayStartComponent > max)
                        return null;
                }
                else
                {
                    float t1 = (min - rayStartComponent) / rayComponent;
                    float t2 = (max - rayStartComponent) / rayComponent;

                    if (t1 > t2)
                    {
                        float temp = t1; 
                        t1 = t2; 
                        t2 = temp;
                    }

                    tEnter = MathF.Max(tEnter, t1);
                    tExit = MathF.Min(tExit, t2);

                    if (tEnter > tExit)
                        return null;
                }
            }

            if (tEnter < 0.0f)
                tEnter = 0.0f;
            if (tEnter > rayLength)
                return null;

            return lineStartPos + rayDir * tEnter;
        }

        // 몬스터 미사일 충돌 후 처리
        private void OnHit(Missile missile, Monster monster)
        {
            long shooter = missile.ShooterId;

            missile.IsRemoved = true;
            monster.Hit(missile.Damage, missile.ShooterId); // 몬스터 데미지 처리
            Console.WriteLine($"몬스터 맞춤 {monster.Type}, {monster.CurHP}/{monster.MaxHP}");

            MissileManager.Instance.Remove(missile); // 미사일 제거

            // 몬스터 데미지, 체력 전달
            DamagePacket damagePacket = new DamagePacket() 
            { messageType = (ushort)MsgType.DamageMonster, hitId = monster.Id, attackId = missile.ShooterId, damage = missile.Damage, curHp = monster.CurHP, maxHp = monster.MaxHP };
            ArraySegment<byte> segment = damagePacket.Write();
            if (damagePacket != null)
                SessionManager.Instance.BroadcastAll(segment);
        }

        // 플레이어 미사일 충돌 후 처리
        private void OnHit(Missile missile, ClientSession player)
        {
            missile.IsRemoved = true;
            
            if (player.CurHP <= missile.Damage) // 죽음
            {
                // 처치 채팅 전송(킬 로그)
                ChatPacket chatPacket = new ChatPacket() { playerId = -1, chat = $"{SessionManager.Instance.Sessions[missile.ShooterId].Name} --kill--> {player.Name}" };
                ArraySegment<byte> chatSegment = chatPacket.Write();
                if (chatPacket != null)
                    SessionManager.Instance.BroadcastAll(chatSegment);

                // 점수 전송
                ScoreManager.Instance.AddScore(missile.ShooterId, ScoreManager.Instance.ScoreDic[player.Info.id] / 2);
            }
            else // 안 죽음
            {
                // 플레이어 데미지 전달
                DamagePacket damagePacket = new DamagePacket() { messageType = (ushort)MsgType.DamagePlayer, hitId = player.Info.id, attackId = missile.ShooterId, damage = missile.Damage, curHp = player.CurHP };
                ArraySegment<byte> segment = damagePacket.Write();
                if (damagePacket != null)
                    SessionManager.Instance.BroadcastAll(segment);
            }

            player.Hit(missile.Damage);
            Console.WriteLine($"플레이어 맞춤 {player.Name}");

            MissileManager.Instance.Remove(missile); // 미사일 제거
        }
    }
}