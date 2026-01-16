using Client;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public GameObject DmamgedImage;   // 데미지 받았을 때 빨간 배경
    public GameObject ResultScreen;   // 게임 결과 창
    public GameObject RankingScreen;  // 점수 창

    public Button DisconnectButton;   // 서버 연결 해제 버튼
    public Button NextButton;         // 넥스트 버튼(결과창)
    public Button ToTitleButton;      // 타이틀 버튼(점수창)

    public TMP_Text NameText;         // 이름 텍스트
    public TMP_Text MyScoreText;      // 자기 점수 텍스트 
    public TMP_Text ScoreBoardName;   // 점수판 top5 이름
    public TMP_Text ScoreBoard;       // 점수판 top5 점수
    public TMP_Text ScoreResult;      // 최종 점수(결과창)

    public TMP_Text RankingRankText;  // 랭킹 출력 - 순위
    public TMP_Text RankingNameText;  // 랭킹 출력 - 이름
    public TMP_Text RankingScoreText; // 랭킹 출력 - 점수
    public TMP_Text RankingRateText;  // 랭킹 출력 - 백분위

    private int _myScoreTemp = 0; // 자기 점수 임시 저장
    private List<PlayerScore> _scoresTemp = new List<PlayerScore>(); // top5 점수 임시 저장
    private bool _onUpdateMyScore = false;    // 자기 점수 갱신
    private bool _onUpdateScoreBoard = false; // top5 점수판 갱신
    private bool _isHit = false;              // 데미지 받았을 때 hp 갱신

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        NameText.text = $"{ClientProgram.Instance.NickName}";
        DisconnectButton.onClick.AddListener(ClientProgram.Instance.GameOver);
    }

    void Update()
    {
        if (_isHit)
            StartCoroutine(FlickerRedScreen());

        UpdateMyScore();
        UpdateScoreBoard();
    }

    // 데미지 경고 트리거
    public void OnTriggerDamaged()
    {
        _isHit = true;
    }

    // 데미지 받으면 빨간색 점멸
    IEnumerator FlickerRedScreen()
    {
        _isHit = false;

        DmamgedImage.SetActive(true);
        yield return new WaitForSecondsRealtime(0.15f);
        DmamgedImage.SetActive(false);
        yield return new WaitForSecondsRealtime(0.1f);
        DmamgedImage.SetActive(true);
        yield return new WaitForSecondsRealtime(0.15f);
        DmamgedImage.SetActive(false);
        yield return new WaitForSecondsRealtime(0.1f);
        DmamgedImage.SetActive(true);
        yield return new WaitForSecondsRealtime(0.15f);
        DmamgedImage.SetActive(false);
    }

    // 결과 창 열기
    public void OpenResultScreen()
    {
        ResultScreen.SetActive(true);
        ScoreResult.text = ClientProgram.Instance.Score.ToString();
    }

    // 다음으로 버튼(결과창 -> 랭킹창)
    public void OnClickNextButton()
    {
        RankingScreen.SetActive(true);
        ResultScreen.SetActive(false);

        StartCoroutine(GetRanking());
    }

    // 타이틀로 버튼(랭킹창 -> 타이틀창)
    public void OnClickToTitleButton()
    {
        RankingScreen.SetActive(false);
        SceneManager.LoadScene(0);
    }

    // 점수판(현재 top5) 갱신 트리거
    public void OnTriggerUpdateScoreBoard(List<PlayerScore> scores)
    {
        _scoresTemp.Clear();

        foreach (PlayerScore score in scores)
            _scoresTemp.Add(score);
        
        _onUpdateScoreBoard = true;
    }

    // 점수판(현재 top5) 갱신
    public void UpdateScoreBoard()
    {
        if (!_onUpdateScoreBoard)
            return;

        _onUpdateScoreBoard = false;

        ScoreBoardName.text = ScoreBoard.text = "";

        int scoreNum = _scoresTemp.Count;
        for (int i = 0; i < scoreNum; i++)
        {
            ScoreBoardName.text += $"{i + 1}.  {_scoresTemp[i].Name}\n";
            ScoreBoard.text += $"{_scoresTemp[i].Score}\n";
        }
    }

    // 자기 점수 갱신 트리거
    public void OnTriggerUpdateMyScore(int myScore)
    {
        _myScoreTemp = myScore;
        _onUpdateMyScore = true;
    }

    // 자기 점수 갱신
    public void UpdateMyScore()
    {
        if (!_onUpdateMyScore)
            return;

        MyScoreText.text = $"Score: {_myScoreTemp}";
        ClientProgram.Instance.Score = _myScoreTemp;
    }

    [Serializable]
    public class MemberListWrapper
    {
        public List<MemberData> list; // JSON 배열을 담을 리스트
    }

    [Serializable]
    public class MemberData
    {
        public long id; // 웹서버와 대소문자 통일
        public string memberName;
        public int memberScore;
    }

    // 랭킹 요청 함수
    IEnumerator GetRanking()
    {
        // 전체 점수 리스트 수 가져오기(상위 몇%인지 표시하기 위함)
        string url = "http://localhost:8081/member/rank/count";
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();
        int totalCount = int.Parse(www.downloadHandler.text);

        url = "http://localhost:8081/member/rank"; // 랭킹
        www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string json = www.downloadHandler.text;
            string newJson = "{ \"list\": " + json + "}";
            MemberListWrapper rankingData = JsonUtility.FromJson<MemberListWrapper>(newJson);

            int rank = 1; 
            RankingRankText.text = RankingNameText.text = RankingScoreText.text = RankingRateText.text = "";
            foreach (var member in rankingData.list) // top10 출력
            {
                RankingRankText.text += $"{rank}\n";
                RankingNameText.text += $"{member.memberName}\n";
                RankingScoreText.text += $"{member.memberScore}\n";

                int rate = (int)((float)(rank - 1.0f) / totalCount * 100.0f);
                RankingRateText.text += $"{rate}%\n";

                rank++;
            }
        }
    }
}
