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

        public Dictionary<long, int> ScoreDic = new Dictionary<long, int>(); // 점수 목록
        private List<PlayerScore> top5List = new List<PlayerScore>();        // 현재 top5 점수 리스트

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
            ScoreDic[id] = ScoreDic.GetValueOrDefault(id) + addScore;
            int newScore = ScoreDic[id];
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

        // 게임 오버 점수 삭제(DB에 저장 후 삭제)
        public void RemoveScore(long id)
        {
            Task task = UpdateRequest(id, ScoreDic[id]); // DB에 최종 점수 저장

            ScoreDic.Remove(id);

            int existingIndex = top5List.FindIndex(p => p.Id == id);
            if (existingIndex != -1) // 만약 top5 안에 있었다면
            {
                top5List.RemoveAt(existingIndex); // top5 리스트에서 삭제
                long inTop5Id = -1;
                int inTop5Score = 0;

                foreach (var rank in ScoreDic) // top5 빈칸 채우기
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

        // DB에 최종 점수 저장
        public async Task UpdateRequest(long id, int score)
        {
            string url = "http://localhost:8081/member/updateScore";
            Dictionary<string, string> values = new Dictionary<string, string> {{ "id", id.ToString() }, { "memberScore", score.ToString() }};
            FormUrlEncodedContent content = new FormUrlEncodedContent(values);   // 데이터를 Form 형태로 인코딩
            HttpResponseMessage response = await client.PostAsync(url, content); // Post 요청 전송

            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();
                if (result == "success")
                    Console.WriteLine("업데이트 성공");
            }
        }
    }
}
