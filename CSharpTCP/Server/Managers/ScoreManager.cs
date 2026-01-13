using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class ScoreManager
    {
        public static ScoreManager Instance { get; } = new ScoreManager();

        public Dictionary<long, int> rankingDic = new Dictionary<long, int>();
        private List<PlayerScore> top5List = new List<PlayerScore>();

        // 점수 전송
        public void UpdateScore()
        {
            ScorePacket scorePacket = new ScorePacket() { playerScore = top5List };
            ArraySegment<byte> scoreSegment = scorePacket.Write();
            SessionManager.Instance.BroadcastAll(scoreSegment);
        }

        // 점수 획득
        public void AddScore(long id, int addScore)
        {
            // 점수 갱신
            rankingDic[id] = rankingDic.GetValueOrDefault(id) + addScore;
            int newScore = rankingDic[id];
            var player = new PlayerScore(id, newScore, SessionManager.Instance.Sessions[id].Name);
            Console.WriteLine($"{id}  {SessionManager.Instance.Sessions[id].Name}  {newScore}");

            if (top5List.Count == 5 && newScore <= top5List[4].Score && !top5List.Exists(p => p.Id == id))
                return;

            // 점수가 top5에 들어있었다면 지우기
            int existingIndex = top5List.FindIndex(p => p.Id == id);
            if (existingIndex != -1)
                top5List.RemoveAt(existingIndex);

            // 삽입하기
            int index = 0;
            while (index < top5List.Count && (top5List[index].Score > newScore || (top5List[index].Score == newScore && top5List[index].Id < id)))
                index++;

            top5List.Insert(index, player);
            if (top5List.Count > 5) top5List.RemoveAt(5);
        }

        // 점수 삭제
        public void RemoveScore(long id)
        {
            rankingDic.Remove(id);

            int existingIndex = top5List.FindIndex(p => p.Id == id);
            if (existingIndex != -1)
            {
                top5List.RemoveAt(existingIndex);
                long inTop5Id = -1;
                int inTop5Score = 0;

                foreach (var rank in rankingDic)
                {
                    if (rank.Value > inTop5Score && top5List.FindIndex(p => p.Id == rank.Key) == -1)
                    {
                        inTop5Id = rank.Key;
                        inTop5Score = rank.Value;
                    }
                }

                if (inTop5Id != -1)
                    AddScore(inTop5Id, 0);
            }
        }
    }
}
