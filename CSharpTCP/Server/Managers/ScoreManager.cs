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
            PlayerScore player = new PlayerScore(id, newScore, SessionManager.Instance.Sessions[id].Name);
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


            // 해당 플레이어에게 점수 전송
            PlayerScore selfScore = new PlayerScore(id, newScore, "");
            List<PlayerScore> selfScoreList = new List<PlayerScore>();
            selfScoreList.Add(selfScore);

            ScorePacket scorePacket = new ScorePacket() { playerScore = selfScoreList };
            ArraySegment<byte> scoreSegment = scorePacket.Write();
            SessionManager.Instance.Sessions[id].Send(scoreSegment);

        }

        // 점수 삭제
        public void RemoveScore(long id)
        {
            UpdateRequest(id, rankingDic[id]);

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

        private static readonly HttpClient client = new HttpClient();

        // 점수 DB에 저장
        public async Task UpdateRequest(long id, int score)
        {
            string url = "http://localhost:8081/member/updateScore";

            // 유니티의 WWWForm.AddField()
            Dictionary<string, string> values = new Dictionary<string, string> {{ "id", id.ToString() }, { "memberScore", score.ToString() }};

            // 데이터를 Form 형태로 인코딩
            FormUrlEncodedContent content = new FormUrlEncodedContent(values);

            // Post 요청 전송
            HttpResponseMessage response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();
                if (result == "success")
                    Console.WriteLine("업데이트 성공");
            }
        }
    }
}
