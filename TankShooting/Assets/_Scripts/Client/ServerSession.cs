using ServerCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Client
{
    class ServerSession : PacketSession
    {
        // 연결되면 실행
        public override void OnConnected(EndPoint endPoint)
        {
            Debug.Log($"OnConnected : {endPoint}");
        }

        // 연결 해제되면 실행
        public override void OnDisconnected(EndPoint endPoint)
        {
            Debug.Log($"OnDisconnected : {endPoint}");

            // 연결 해제 시 모든 오브젝트 제거
            ClientProgram.Instance.RemovePlayerAll();
        }

        // 패킷 보내면 실행
        public override void OnSend(int numOfBytes)
        {
            Debug.Log($"OnSend");
        }

        // 패킷 받으면 실행
        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            ushort count = 0;
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);
            count += sizeof(ushort);
            ushort packetType = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);

            // 패킷 유형에 맞는 코드 실행
            switch ((PacketType)packetType)
            {
                case PacketType.PlayerInfoReq: OnRecvPlayerInfoReq(buffer); break;
                case PacketType.PlayerInfoOk:  OnRecvPlayerInfoOk(buffer);  break;
                case PacketType.CreateRemove:  OnRecvCreateRemove(buffer);  break;
                case PacketType.CreateAll:     OnRecvCreateAll(buffer);     break;
                case PacketType.Move:          OnRecvMove(buffer);          break;
                case PacketType.Chat:          OnRecvChat(buffer);          break;
            }
        }

        // PlayerInfoReq 패킷 받음
        public void OnRecvPlayerInfoReq(ArraySegment<byte> buffer)
        {
            PlayerInfoReq playerInfo = new PlayerInfoReq();
            playerInfo.Read(buffer);

            Debug.Log($"InfoReq | ID : {playerInfo.playerId}, name : {playerInfo.name}\n");

            // 서버에 이름(닉네임) 전송
            PlayerInfoReq packet = new PlayerInfoReq() { playerId = 0, name = ClientProgram.Instance.NickName };
            ArraySegment<byte> segment = packet.Write(); // 직렬화

            if (segment != null)
                Send(segment);
        }

        // PlayerInfoOk 패킷 받음
        public void OnRecvPlayerInfoOk(ArraySegment<byte> buffer)
        {
            PlayerInfoOk playerInfo = new PlayerInfoOk();
            playerInfo.Read(buffer);

            Debug.Log($"InfoOk : {playerInfo.playerId}\n");
            ClientProgram.Instance.ClientId = playerInfo.playerId;
        }

        // CreateRemove 패킷 받음
        public void OnRecvCreateRemove(ArraySegment<byte> buffer)
        {
            PlayerCreateRemovePacket crPacket = new PlayerCreateRemovePacket();
            crPacket.Read(buffer);

            Debug.Log($"PlayerCreateRemovePacket | ID: {crPacket.playerId}, messageType : {crPacket.messageType}\n");

            if (crPacket.messageType == (ushort)MsgType.Create)
            {
                // 플레이어 아이디로 특정 플레이어 생성
                ObjectInfo playerInfo = new ObjectInfo();
                playerInfo.id = crPacket.playerId;
                ClientProgram.Instance.OnTriggerCreateCharacter(playerInfo);
            }
            else if (crPacket.messageType == (ushort)MsgType.Remove)
            {
                // 플레이어 아이디로 특정 플레이어의 오브젝트 삭제
                ClientProgram.Instance.OnTriggerRemoveExitCharacter(crPacket.playerId);
            }
        }

        // CreateAll 패킷 받음
        public void OnRecvCreateAll(ArraySegment<byte> buffer)
        {
            PlayerCreateAll crPacket = new PlayerCreateAll();
            crPacket.Read(buffer);
            Debug.Log("create all");

            // 자신이 들어오기 전 먼저 들어와 있던 플레이어들의 오브젝트 생성
            ClientProgram.Instance.OnTriggerCreateCharacterAll(crPacket.playerInfos);
        }

        // Move 패킷 받음
        public void OnRecvMove(ArraySegment<byte> buffer)
        {
            MovePacket movePacket = new MovePacket();
            movePacket.Read(buffer);

            Debug.Log($"move | msgType: {movePacket.messageType}, ID: {movePacket.playerInfo.id}, pos: {movePacket.playerInfo.position}, rot: {movePacket.playerInfo.rotation}\n");

            switch (movePacket.messageType)
            {
                case (ushort)MsgType.PlayerMove:
                    long playerId = movePacket.playerInfo.id;
                    Debug.Log($"내 id: {ClientProgram.Instance.ClientId},  보낸 사람 id: {playerId}");

                    // 플레이어 아이디로 특정 플레이어의 위치 정보 갱신
                    if (ClientProgram.Instance.playerObjDic[playerId] != null)
                        ClientProgram.Instance.playerObjDic[playerId].OnTriggerUpdateOtherPos(movePacket.playerInfo);
                    break;

                case (ushort)MsgType.MonsterMove:
                    // 몬스터들 갱신
                    break;

                case (ushort)MsgType.MissileMove:
                    // 미사일들 갱신
                    break;
            }


            //long id = movePacket.playerInfo.id;
            //Debug.Log($"Move | ID : {id}, pos : {movePacket.playerInfo.position}, rot : {movePacket.playerInfo.rotation}\n");

            // 플레이어 아이디로 특정 플레이어의 위치 정보 갱신
            //if (ClientProgram.Instance.playerObjDic[id] != null)
            //    ClientProgram.Instance.playerObjDic[id].OnTriggerUpdateOtherPos(movePacket.playerInfo);
        }

        // Chat 패킷 받음
        public void OnRecvChat(ArraySegment<byte> buffer)
        {
            ChatPacket chatPacket = new ChatPacket();
            chatPacket.Read(buffer);

            Debug.Log($"Chat | ID : {chatPacket.playerId}, chat: {chatPacket.chat}\n");

            // 채팅창에 업데이트
            ClientProgram.Instance.RecvChatting(chatPacket.chat);
        }
    }
}
