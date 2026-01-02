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
            PlayerManager.Instance.RemovePlayerAll();
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
                case PacketType.Damage:        OnRecvDamage(buffer);        break;
            }
        }


        // PlayerInfoReq 패킷 받음
        public void OnRecvPlayerInfoReq(ArraySegment<byte> buffer)
        {
            PlayerInfoReq playerInfo = new PlayerInfoReq();
            playerInfo.Read(buffer);

            Debug.Log($"InfoReq | ID : {playerInfo.PlayerId}, name : {playerInfo.Name}\n");

            // 서버에 이름(닉네임) 전송
            PlayerInfoReq packet = new PlayerInfoReq() { PlayerId = 0, Name = ClientProgram.Instance.NickName };
            ArraySegment<byte> segment = packet.Write(); // 직렬화

            if (segment != null)
                Send(segment);
        }

        // PlayerInfoOk 패킷 받음
        public void OnRecvPlayerInfoOk(ArraySegment<byte> buffer)
        {
            PlayerInfoOk playerInfo = new PlayerInfoOk();
            playerInfo.Read(buffer);

            Debug.Log($"InfoOk : {playerInfo.PlayerId}\n");
            ClientProgram.Instance.ClientId = playerInfo.PlayerId;
        }

        // CreateRemove 패킷 받음
        public void OnRecvCreateRemove(ArraySegment<byte> buffer)
        {
            CreateRemovePacket crPacket = new CreateRemovePacket();
            crPacket.Read(buffer);

            Debug.Log($"PlayerCreateRemovePacket | ID: {crPacket.PlayerId}, messageType : {crPacket.MessageType}\n");

            switch ((ushort)crPacket.MessageType)
            {
                case (ushort)MsgType.CreatePlayer:
                    // 플레이어 아이디로 특정 플레이어 생성
                    ObjectInfo playerInfo = new ObjectInfo();
                    playerInfo.Id = crPacket.PlayerId;
                    PlayerManager.Instance.OnTriggerCreateCharacter(playerInfo);
                    break;

                case (ushort)MsgType.RemovePlayer:
                    // 플레이어 아이디로 특정 플레이어의 오브젝트 삭제
                    PlayerManager.Instance.OnTriggerRemoveExitCharacter(crPacket.PlayerId);
                    break;

                case (ushort)MsgType.CreateMissile:
                    // 플레이어 아이지로 생성할 미사일 위치, 방향 결정
                    MissileManager.Instance.OnTriggerCreateMissile(crPacket.PlayerId);
                    break;
            }
        }

        // CreateAll 패킷 받음
        public void OnRecvCreateAll(ArraySegment<byte> buffer)
        {
            CreateAll crPacket = new CreateAll();
            crPacket.Read(buffer);
            Debug.Log($"create all {(MsgType)crPacket.MessageType}");

            switch (crPacket.MessageType)
            {
                case (ushort)MsgType.CreateAllPlayer:
                    // 자신이 들어오기 전 먼저 들어와 있던 플레이어들의 오브젝트 생성
                    PlayerManager.Instance.OnTriggerCreateCharacterAll(crPacket.ObjInfos);
                    break;

                case (ushort)MsgType.CreateAllMonster:
                    // 자신이 들어오기 전 먼저 생성되어 있는 모든 몬스터 생성
                    MonsterManager.Instance.OnTriggerCreateMonsterAll(crPacket.ObjInfos);
                    break;

                case (ushort)MsgType.CreateAllStructure:
                    // 모든 건물 생성
                    StructureManager.Instance.OnTriggerCreateStructureAll(crPacket.ObjInfos);
                    break;
            }
        }

        // Move 패킷 받음
        public void OnRecvMove(ArraySegment<byte> buffer)
        {
            MovePacket movePacket = new MovePacket();
            movePacket.Read(buffer);

            //Debug.Log($"move | msgType: {movePacket.messageType}, ID: {movePacket.objInfo.id}, pos: {movePacket.objInfo.position}, rot: {movePacket.objInfo.rotation}\n");
            long playerId = movePacket.ObjInfo.Id;
            switch (movePacket.MessageType)
            {
                case (ushort)MsgType.MovePlayer:
                    //Debug.Log($"내 id: {ClientProgram.Instance.ClientId},  보낸 사람 id: {playerId}");

                    // 플레이어 아이디로 특정 플레이어의 위치 정보 갱신
                    if (PlayerManager.Instance.PlayerObjDic[playerId] != null)
                        PlayerManager.Instance.PlayerObjDic[playerId].OnTriggerUpdateOtherPos(movePacket.ObjInfo);
                    break;

                case (ushort)MsgType.RollbackPlayer:
                    PlayerManager.Instance.PlayerObjDic[playerId].OnTriggerPlayerRollback(movePacket.ObjInfo);
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

            Debug.Log($"Chat | ID : {chatPacket.PlayerId}, chat: {chatPacket.Chat}\n");

            // 채팅창에 업데이트
            ChatManager.Instance.RecvChatting(chatPacket.Chat);
        }

        // Damage 패킷 받음
        public void OnRecvDamage(ArraySegment<byte> buffer)
        {
            DamagePacket damagePacket = new DamagePacket();
            damagePacket.Read(buffer);

            switch ((ushort)damagePacket.messageType)
            {
                case (ushort)MsgType.DamagePlayer:
                    // 체력 감소, 공격자가 본인이면 데미지 표시 띄우기, 피해자가 본인이면 빨간 이펙트 띄우기
                    if (damagePacket.hitId == ClientProgram.Instance.ClientId)
                    {
                        // 본인 체력 감소
                    }
                    if (damagePacket.attackId == ClientProgram.Instance.ClientId)
                    {
                        // 데미지 표시 띄우기
                    }

                    break;

                case (ushort)MsgType.DamageMonster:
                    // 체력 감소, 공격자가 본인이면 데미지 표시 띄우기
                    MonsterManager.Instance.MonsterObjDic[damagePacket.hitId].OnTriggerHit(damagePacket.damage, damagePacket.curHp, damagePacket.maxHp);
                    if (damagePacket.attackId == ClientProgram.Instance.ClientId)
                    {
                        // 데미지 표시 띄우기
                    }
                    break;
            }
        }
    }
}