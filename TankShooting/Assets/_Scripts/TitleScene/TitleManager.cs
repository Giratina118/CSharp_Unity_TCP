using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    // 로그인 UI
    public GameObject LoginBG;
    public TMP_InputField LoginEmail;
    public TMP_InputField LoginPassword;

    // 회원가입 UI
    public GameObject RegisterBG;
    public TMP_InputField RegisterName;
    public TMP_InputField RegisterEmail;
    public TMP_InputField RegisterPassword;

    enum RequestType
    {
        Login = 1,
        Register,
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !LoginBG.activeSelf && !RegisterBG.activeSelf)
            LoginBG.SetActive(true);
    }

    public class MemberDTO
    {
        public long id;
        public String memberEmail;
        public String memberPassword;
        public String memberName;
    }

    // 웹서버 요청
    IEnumerator UnityWebRequest(int requestType)
    {
        string url = "";
        WWWForm form = new WWWForm();

        switch (requestType)
        {
            case (int)RequestType.Login:
                url = "http://localhost:8081/member/login"; 
                form.AddField("memberEmail", LoginEmail.text);
                form.AddField("memberPassword", LoginPassword.text);
                break;

            case (int)RequestType.Register:
                url = "http://localhost:8081/member/save";
                form.AddField("memberName", RegisterName.text); 
                form.AddField("memberEmail", RegisterEmail.text);
                form.AddField("memberPassword", RegisterPassword.text);
                break;

            default:
                // 코루틴 종료
                break;
        }

        UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Post(url, form);

        yield return www.SendWebRequest(); // 응답 대기


        if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            string response = www.downloadHandler.text;
            if (response == "success")
            {
                Debug.Log("로그인 성공");

                // 로그인 성공이면 메인씬으로
                // 회원가입 성공이면 로그인으로
            }
            else
            {
                Debug.Log("로그인/회원가입 실패");
            }
        }
    }

    // 회원가입하러 가기 버튼
    public void OnClickGotoRegisterButton()
    {
        RegisterBG.SetActive(true);
        LoginBG.SetActive(false);
    }

    // 로그인 버튼
    public void OnClickLoginButton()
    {
        if (LoginEmail.text.Length == 0 || LoginPassword.text.Length == 0)
            return;

        LoginEmail.text = LoginPassword.text = "";

        StartCoroutine(UnityWebRequest((int)RequestType.Login));
    }

    // 나가기 버튼
    public void OnClickExitButton()
    {
        Application.Quit();
    }


    // 회원가입 버튼
    public void OnClickRegisterButton()
    {
        if (RegisterName.text.Length == 0 || RegisterEmail.text.Length == 0 || RegisterPassword.text.Length == 0)
            return;

        RegisterName.text = RegisterEmail.text = RegisterPassword.text = "";

        StartCoroutine(UnityWebRequest((int)RequestType.Register));
    }

    // 뒤로 가기 버튼
    public void OnClickBackButton()
    {
        LoginBG.SetActive(true);
        RegisterBG.SetActive(false);
    }
}