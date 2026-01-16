using Client;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance { get; private set; }

    public TMP_InputField ChatInputField; // 채팅 입력
    public TMP_Text ChatContent;          // 채팅 로그
    public Scrollbar ChatScrollbar;       // 채팅 로그 스크롤바

    private string _chatTemp;     // 새로 들어온 채팅
    private bool _onChat = false; // 채팅 갱신 정보가 들어왔는지

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (!ChatInputField.interactable)
                ChatInputField.interactable = true;
            else
                ChatInputField.interactable = false;
        }
    }

    // 채팅 전송
    public void SendChatting()
    {
        if (ChatInputField.text == "")
            return;

        ChatPacket packet = new ChatPacket() { PlayerId = ClientProgram.Instance.ClientId, Chat = ChatInputField.text };
        ArraySegment<byte> segment = packet.Write();
        ClearChatInputField(); // 인풋 필드 지우기
        ClientProgram.Instance.Connector.CurrentSession.Send(segment);
    }

    // 채팅 받기
    public void RecvChatting(string chat)
    {
        _onChat = true;
        _chatTemp = chat;
    }

    // 채팅 업데이트
    public void UpdateChatting()
    {
        if (!_onChat)
            return;
        _onChat = false;

        if (ChatContent.text == "") ChatContent.text += _chatTemp;
        else                        ChatContent.text += "\n" + _chatTemp;

        StartCoroutine(ScrollToBottom()); // 채팅창 아래로 내리기
        Canvas.ForceUpdateCanvases();     // Canvas 강제 갱신
        ChatScrollbar.value = 0;
    }

    // 채팅창 아래로 내리기(가장 최근 글이 맨 밑에 보이도록)
    private IEnumerator ScrollToBottom()
    {
        yield return null;            // 레이아웃이 확실히 갱신되도록 한 프레임 대기
        Canvas.ForceUpdateCanvases(); // Canvas 강제 갱신
        ChatScrollbar.value = 0.0f;   // ScrollRect 맨 아래
    }

    // 채팅 내역 적기
    public void WriteChatContent(string content)
    {
        ChatContent.text += content;
    }

    // 채팅 입력창 지우기
    public void ClearChatInputField()
    {
        ChatInputField.text = "";
    }
}
