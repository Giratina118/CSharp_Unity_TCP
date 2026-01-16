using Client;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable] // 필수: JsonUtility가 인식할 수 있게 함
public class LoginDTO // 서버의 MemberLoginDTO와 매칭
{
    public bool success;
    public string name;
    public long id;
}

public class TitleManager : MonoBehaviour
{
    // 처음 화면
    public GameObject FirstBtnBG;

    // 설정 화면
    public GameObject SettingBG;
    
    // 로그인 UI
    public GameObject LoginBG;
    public TMP_InputField LoginEmail;    // 이메일 입력
    public TMP_InputField LoginPassword; // 패스워드 입력

    // 회원가입 UI
    public GameObject RegisterBG;
    public TMP_InputField RegisterName;     // 닉네임 입력
    public TMP_InputField RegisterEmail;    // 이메일 입력
    public TMP_InputField RegisterPassword; // 패스워드 입력

    // 요청 유형
    enum RequestType
    {
        Login = 1, // 로그인
        Register,  // 회원가입
    }

    // 로그인 요청
    IEnumerator LoginRequest()
    {
        string url = "http://localhost:8081/member/login";
        WWWForm form = new WWWForm();
        Debug.Log($"보내는 데이터 체크 - Email: [{LoginEmail.text}], Password: [{LoginPassword.text}]");
        form.AddField("memberEmail", LoginEmail.text);
        form.AddField("memberPassword", LoginPassword.text);

        UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Post(url, form);
        yield return www.SendWebRequest(); // 응답 대기

        if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            yield break;

        string responseJson = www.downloadHandler.text;
        LoginDTO loginDTO = JsonUtility.FromJson<LoginDTO>(responseJson); // JSON 문자열을 클래스 객체로 파싱

        Debug.Log($"{responseJson} / {loginDTO.success}  {loginDTO.id}  {loginDTO.name}");

        if (loginDTO.success) // 로그인 성공이면 서버 연결, 메인씬으로
            ClientProgram.Instance.OnConnectServer(loginDTO.id, loginDTO.name);

        LoginEmail.text = LoginPassword.text = "";
    }

    // 회원가입 요청
    IEnumerator RegisterRequest()
    {
        string url = "http://localhost:8081/member/save";
        WWWForm form = new WWWForm();
        form.AddField("memberName", RegisterName.text);
        form.AddField("memberEmail", RegisterEmail.text);
        form.AddField("memberPassword", RegisterPassword.text);

        UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Post(url, form);
        yield return www.SendWebRequest(); // 응답 대기

        if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            yield break; // 응답 실패시 종료

        string response = www.downloadHandler.text;
        if (response.Equals("success")) // 회원가입 성공
            OnClickBackToLoginButton(); 

        RegisterName.text = RegisterEmail.text = RegisterPassword.text = "";
    }


    // 시작 화면
    // 시작하기 버튼
    public void OnClickStartButton()
    {
        FirstBtnBG.SetActive(false);
        LoginBG.SetActive(true);
    }

    // 설정 버튼
    public void OnClickSettingButton()
    {
        FirstBtnBG.SetActive(false);
        SettingBG.SetActive(true);
    }

    // 나가기 버튼
    public void OnClickExitButton()
    {
        Application.Quit();
    }


    // 설정 화면
    // 설정 저장 버튼
    public void OnClickSaveSettingButton()
    {
        AudioManager.Instance.Save();
    }

    // 뒤로 가기 버튼(설정/로그인 -> 최초화면)
    public void OnClickBackToFirstButton(bool isSetting)
    {
        SettingBG.SetActive(false);
        LoginBG.SetActive(false);
        FirstBtnBG.SetActive(true);

        if (isSetting)
            AudioManager.Instance.SetVolume();
    }


    // 로그인 화면
    // 회원가입하러 가기 버튼
    public void OnClickGotoRegisterButton()
    {
        LoginBG.SetActive(false);
        RegisterBG.SetActive(true);
    }

    // 로그인 버튼
    public void OnClickLoginButton()
    {
        if (LoginEmail.text.Length == 0 || LoginPassword.text.Length == 0)
            return;

        StartCoroutine(LoginRequest());
    }


    // 회원가입 화면
    // 회원가입 버튼
    public void OnClickRegisterButton()
    {
        if (RegisterName.text.Length == 0 || RegisterEmail.text.Length == 0 || RegisterPassword.text.Length == 0)
            return;

        StartCoroutine(RegisterRequest());
    }

    // 뒤로 가기 버튼(회원가입 -> 로그인)
    public void OnClickBackToLoginButton()
    {
        LoginBG.SetActive(true);
        RegisterBG.SetActive(false);
    }
}