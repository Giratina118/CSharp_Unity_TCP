using Client;
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

    public GameObject DmamgedImage;  // 데미지 받았을 때 빨간 배경
    public GameObject ResultScreen;  // 게임 결과 창
    public GameObject RankingScreen; // 점수 창

    public Button DisconnectButton;  // 서버 연결 해제 버튼
    public Button NextButton;        // 넥스트 버튼(결과창)
    public Button ToTitleButton;     // 타이틀 버튼(점수창)

    public TMP_Text NameText;        // 이름 텍스트
    public TMP_Text ScoreBoardName;  // 점수판 top5 이름
    public TMP_Text ScoreBoard;      // 점수판 top5 점수
    public TMP_Text ScoreResult;     // 최종 점수(결과창)

    private List<PlayerScore> scoresTemp = new List<PlayerScore>();
    private bool _onUpdateScoreBoard = false;
    private bool _isHit = false;

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        NameText.text = $"Name: {ClientProgram.Instance.NickName}";
        DisconnectButton.onClick.AddListener(ClientProgram.Instance.GameOver);
    }

    void Update()
    {
        if (_isHit)
            StartCoroutine(FlickerRedScreen());

        UpdateScoreBoard();
    }

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
    }

    // 타이틀로 버튼(랭킹창 -> 타이틀창)
    public void OnClickToTitleButton()
    {
        RankingScreen.SetActive(false);
        SceneManager.LoadScene(0);
    }

    // 점수 갱신 트리거
    public void OnTriggerUpdateScoreBoard(List<PlayerScore> scores)
    {
        scoresTemp.Clear();

        foreach (PlayerScore score in scores)
        {
            scoresTemp.Add(score);
            Debug.Log($"{score.Id}  {score.Name}  {score.Score}");
        }
        
        _onUpdateScoreBoard = true;
    }

    // 점수 갱신
    public void UpdateScoreBoard()
    {
        if (!_onUpdateScoreBoard)
            return;

        _onUpdateScoreBoard = false;

        ScoreBoardName.text = ScoreBoard.text = "";

        int scoreNum = scoresTemp.Count;
        for (int i = 0; i < scoreNum; i++)
        {
            ScoreBoardName.text += $"{i + 1}.  {scoresTemp[i].Name}\n";
            ScoreBoard.text += $"{scoresTemp[i].Score}\n";
        }
    }

    IEnumerator GetSortedRanking()
    {
        string url = "http://localhost:8081/member/save";
        UnityWebRequest www = UnityWebRequest.Get(url);

        yield return www.SendWebRequest();

        /*
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
        }
        else
        {
            // 서버에서 받은 정렬된 JSON 문자열
            string jsonResult = www.downloadHandler.text;
            // JSON 파싱 후 바로 UI에 표시
        }
        */
    }
}
