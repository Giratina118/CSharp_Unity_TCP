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
            //Debug.Log($"OnSend");
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
            CreateRemovePacket crPacket = new CreateRemovePacket();
            crPacket.Read(buffer);

            Debug.Log($"PlayerCreateRemovePacket | ID: {crPacket.playerId}, messageType : {crPacket.messageType}\n");

            switch ((ushort)crPacket.messageType)
            {
                case (ushort)MsgType.CreatePlayer:
                    // 플레이어 아이디로 특정 플레이어 생성
                    ObjectInfo playerInfo = new ObjectInfo();
                    playerInfo.id = crPacket.playerId;
                    ClientProgram.Instance.OnTriggerCreateCharacter(playerInfo);
                    break;

                case (ushort)MsgType.RemovePlayer:
                    // 플레이어 아이디로 특정 플레이어의 오브젝트 삭제
                    ClientProgram.Instance.OnTriggerRemoveExitCharacter(crPacket.playerId);
                    break;

                case (ushort)MsgType.CreateMissile:
                    // 플레이어 아이지로 생성할 미사일 위치, 방향 결정
                    ClientProgram.Instance.OnTriggerCreateMissile(crPacket.playerId);
                    break;
            }

            /*
            if (crPacket.messageType == (ushort)MsgType.CreatePlayer)
            {
                // 플레이어 아이디로 특정 플레이어 생성
                ObjectInfo playerInfo = new ObjectInfo();
                playerInfo.id = crPacket.playerId;
                ClientProgram.Instance.OnTriggerCreateCharacter(playerInfo);
            }
            else if (crPacket.messageType == (ushort)MsgType.RemovePlayer)
            {
                // 플레이어 아이디로 특정 플레이어의 오브젝트 삭제
                ClientProgram.Instance.OnTriggerRemoveExitCharacter(crPacket.playerId);
            }
            */
        }

        // CreateAll 패킷 받음
        public void OnRecvCreateAll(ArraySegment<byte> buffer)
        {
            CreateAll crPacket = new CreateAll();
            crPacket.Read(buffer);
            Debug.Log($"create all {(MsgType)crPacket.messageType}");

            switch (crPacket.messageType)
            {
                case (ushort)MsgType.CreateAllPlayer:
                    // 자신이 들어오기 전 먼저 들어와 있던 플레이어들의 오브젝트 생성
                    ClientProgram.Instance.OnTriggerCreateCharacterAll(crPacket.objInfos);
                    break;

                case (ushort)MsgType.CreateAllMonster:
                    // 자신이 들어오기 전 먼저 생성되어 있는 모든 몬스터 생성
                    ClientProgram.Instance.OnTriggerCreateMonsterAll(crPacket.objInfos);
                    break;
            }
        }

        // Move 패킷 받음
        public void OnRecvMove(ArraySegment<byte> buffer)
        {
            MovePacket movePacket = new MovePacket();
            movePacket.Read(buffer);

            Debug.Log($"move | msgType: {movePacket.messageType}, ID: {movePacket.objInfo.id}, pos: {movePacket.objInfo.position}, rot: {movePacket.objInfo.rotation}\n");
            long playerId = movePacket.objInfo.id;
            switch (movePacket.messageType)
            {
                case (ushort)MsgType.MovePlayer:
                    Debug.Log($"내 id: {ClientProgram.Instance.ClientId},  보낸 사람 id: {playerId}");

                    // 플레이어 아이디로 특정 플레이어의 위치 정보 갱신
                    if (ClientProgram.Instance.playerObjDic[playerId] != null)
                        ClientProgram.Instance.playerObjDic[playerId].OnTriggerUpdateOtherPos(movePacket.objInfo);
                    break;

                case (ushort)MsgType.RollbackPlayer:
                    ClientProgram.Instance.playerObjDic[playerId].OnTriggerPlayerRollback(movePacket.objInfo);
                    break;

                case (ushort)MsgType.MoveMonster:
                    // 몬스터들 갱신
                    break;

                case (ushort)MsgType.MoveMissile:
                    // 미사일들 갱신
                    break;
            }
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
