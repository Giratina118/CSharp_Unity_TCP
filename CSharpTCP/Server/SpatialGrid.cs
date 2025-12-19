using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vector2Int = System.ValueTuple<int, int>;

namespace Server
{
    // 충돌처리 과정 단축을 위한 공간 분할
    public class GridCell
    {   // c# HashSet -> C++ unordered_set
        // 해당 칸에 들어있는 몬스터, 플레이어 집합
        public HashSet<Monster> Monsters = new HashSet<Monster>();
        public HashSet<ClientSession> Players = new HashSet<ClientSession>();
    }

    // 셀로 나눠진 월드 전체 공간
    public class SpatialGrid
    {
        public static SpatialGrid Instance { get; } = new SpatialGrid(10.0f); // 셀 크기

        private float _cellSize; // 한 셀의 실제 월드 크기
        private Dictionary<Vector2Int, GridCell> _cells = new Dictionary<Vector2Int, GridCell>(); // 셀 좌표, 셀 데이터

        private SpatialGrid(float cellSize)
        {
            _cellSize = cellSize;
        }

        // 월드 좌표 → 셀 좌표
        private Vector2Int ToCell(Vector3 pos)
        {
            int x = (int)MathF.Floor(pos.X / _cellSize);
            int z = (int)MathF.Floor(pos.Z / _cellSize);
            return (x, z);
        }

        // 몬스터 셀에 등록
        public void AddMonster(Monster monster)
        {
            // 셀 찾기, 없으면 추가 후 몬스터 등록
            Console.WriteLine($"위치: {monster._pos},  셀: {ToCell(monster._pos)}");
            GetOrCreateCell(ToCell(monster._pos)).Monsters.Add(monster);
        }

        // 플레이어 셀에 등록
        public void AddPlayer(ClientSession player)
        {
            // 셀 찾기, 없으면 추가 후 플레이어 등록
            GetOrCreateCell(ToCell(player.Info.position)).Players.Add(player);
        }

        // 몬스터 제거
        public void RemoveMonster(Monster monster)
        {
            Vector2Int cell = ToCell(monster._pos); // 현재 위치 기준 셀

            if (_cells.TryGetValue(cell, out GridCell gridCell))
            {
                gridCell.Monsters.Remove(monster); // 해당 셀에서 제거

                if (gridCell.Monsters.Count == 0 && gridCell.Players.Count == 0)
                    _cells.Remove(cell); // 비었으면 셀 제거
            }
        }

        // 플레이어 제거
        public void RemovePlayer(ClientSession player)
        {
            Vector2Int cell = ToCell(player.Info.position); // 현재 위치 기준 셀

            if (_cells.TryGetValue(cell, out GridCell gridCell))
            {
                gridCell.Players.Remove(player); // 해당 셀에서 제거

                if (gridCell.Monsters.Count == 0 && gridCell.Players.Count == 0)
                    _cells.Remove(cell); // 비었으면 셀 제거
            }
        }

        // 오브젝트가 이동할 때 어떤 셀을 지나치는지(검사해야 하는지)
        public List<GridCell> QueryBySegment(Vector3 from, Vector3 to)
        {
            HashSet<GridCell> result = new HashSet<GridCell>(); // 검사해야 하는 셀 목록

            Vector2Int prev = ToCell(from);
            Vector2Int curr = ToCell(to);

            if (_cells.TryGetValue(prev, out var prevCell))
                result.Add(prevCell); // 이전 위치 셀

            if (_cells.TryGetValue(curr, out var currCell))
                result.Add(currCell); // 현재 위치 셀

            return result.ToList();
        }

        // 셀 이동 갱신
        public void UpdatePlayer(ClientSession player, Vector3 prevPos)
        {
            Vector2Int prev = ToCell(prevPos);
            Vector2Int curr = ToCell(player.Info.position);

            if (prev == curr) // 같은 셀이면 X, 셀이 바뀌었을 때만 갱신
                return;

            if (_cells.TryGetValue(prev, out GridCell oldCell))
                oldCell.Players.Remove(player); // 이전 셀에서 제거

            GetOrCreateCell(curr).Players.Add(player); // 새 셀에 추가
        }

        // 몬스터 이동 갱신
        public void UpdateMonster(Monster monster, Vector3 prevPos)
        {
            Vector2Int prev = ToCell(prevPos);
            Vector2Int curr = ToCell(monster._pos);

            if (prev == curr)
                return;

            if (_cells.TryGetValue(prev, out GridCell oldCell))
                oldCell.Monsters.Remove(monster); // 이전 셀에서 제거

            GetOrCreateCell(curr).Monsters.Add(monster); // 새 셀에 추가
        }

        // 셀 생성
        private GridCell GetOrCreateCell(Vector2Int key)
        {
            // 아직 존재하지 않는 셀인지 확인
            if (!_cells.TryGetValue(key, out GridCell cell))
            {
                cell = new GridCell(); // 존재하지 않으면 셀 생성
                _cells.Add(key, cell);
            }
            return cell;
        }
    }
}
