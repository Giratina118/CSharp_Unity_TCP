using ServerCore;
using System;
using System.Net;
using System.Text;
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

            Debug.Log((PacketType)packetType);
            // 패킷 유형에 맞는 코드 실행
            switch ((PacketType)packetType)
            {
                case PacketType.PlayerInfoReq: OnRecvPlayerInfoReq(buffer); break;
                case PacketType.PlayerInfoOk: OnRecvPlayerInfoOk(buffer); break;
                case PacketType.CreateRemove: OnRecvCreateRemove(buffer); break;
                case PacketType.ObjList: OnRecvObjectList(buffer); break;
                case PacketType.Move: OnRecvMove(buffer); break;
                case PacketType.Chat: OnRecvChat(buffer); break;
                case PacketType.Damage: OnRecvDamage(buffer); break;
                case PacketType.Score: OnRecvScore(buffer); break;
            }
        }


        // PlayerInfoReq 패킷 받음
        public void OnRecvPlayerInfoReq(ArraySegment<byte> buffer)
        {
            PlayerInfoReq playerInfo = new PlayerInfoReq();
            playerInfo.Read(buffer);

            Debug.Log($"InfoReq | ID : {playerInfo.PlayerId}, name : {playerInfo.Name}\n");

            // 서버에 이름(닉네임) 전송
            PlayerInfoReq packet = new PlayerInfoReq() { PlayerId = ClientProgram.Instance.ClientId, Name = ClientProgram.Instance.NickName };
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
            Debug.Log($"PlayerCreateRemovePacket | ID: {crPacket.Id}, messageType : {crPacket.MessageType}\n");

            switch ((ushort)crPacket.MessageType)
            {
                case (ushort)MsgType.CreatePlayer:
                    // 플레이어 아이디로 특정 플레이어 생성
                    ObjectInfo playerInfo = new ObjectInfo();
                    playerInfo.Id = crPacket.Id;
                    PlayerManager.Instance.OnTriggerCreateCharacter(playerInfo);
                    break;

                case (ushort)MsgType.RemovePlayer:
                    // 플레이어 아이디로 특정 플레이어의 오브젝트 삭제
                    PlayerManager.Instance.OnTriggerRemoveExitCharacter(crPacket.Id);
                    break;

                case (ushort)MsgType.CreateMissile:
                    // 플레이어 아이디로 생성할 미사일 위치, 방향 결정
                    MissileManager.Instance.OnTriggerCreateMissile(crPacket.Id);
                    break;

                case (ushort)MsgType.RespawnMonster:
                    // 몬스터 리스폰(켜기)
                    MonsterManager.Instance.OnTriggerRespawn(crPacket.Id);
                    break;

                case (ushort)MsgType.RemoveMonster:
                    // 몬스터 끄기
                    MonsterManager.Instance.OnTriggerRemoveMonster(crPacket.Id);
                    break;

                case (ushort)MsgType.DieMe:
                    // 본인이 죽었다면
                    ClientProgram.Instance.GameOver();
                    break;
            }
        }

        // ObjectList 패킷 받음
        public void OnRecvObjectList(ArraySegment<byte> buffer)
        {
            ObjListPacket objListPacket = new ObjListPacket();
            objListPacket.Read(buffer);
            Debug.Log($"create all {(MsgType)objListPacket.MessageType}");

            switch (objListPacket.MessageType)
            {
                case (ushort)MsgType.CreateAllPlayer:
                    // 자신이 들어오기 전 먼저 들어와 있던 플레이어들의 오브젝트 생성
                    PlayerManager.Instance.OnTriggerCreateCharacterAll(objListPacket.ObjInfos);
                    break;

                case (ushort)MsgType.CreateAllMonster:
                    // 자신이 들어오기 전 먼저 생성되어 있는 모든 몬스터 생성
                    MonsterManager.Instance.OnTriggerCreateMonsterAll(objListPacket.ObjInfos);
                    break;

                case (ushort)MsgType.CreateAllStructure:
                    // 모든 건물 생성
                    StructureManager.Instance.OnTriggerCreateStructureAll(objListPacket.ObjInfos);
                    break;

                case (ushort)MsgType.MonsterInfoList:
                    // 몬스터 위치 업데이트
                    Debug.Log("recv monster info");
                    MonsterManager.Instance.OnTriggerUpdatePos(objListPacket.ObjInfos);
                    break;
            }
        }

        // Move 패킷 받음
        public void OnRecvMove(ArraySegment<byte> buffer)
        {
            MovePacket movePacket = new MovePacket();
            movePacket.Read(buffer);

            long playerId = movePacket.ObjInfo.Id;
            switch (movePacket.MessageType)
            {
                case (ushort)MsgType.MovePlayer:
                    // 플레이어 아이디로 특정 플레이어의 위치 정보 갱신
                    if (PlayerManager.Instance.PlayerObjDic[playerId] != null)
                        PlayerManager.Instance.PlayerObjDic[playerId].OnTriggerUpdateOtherPos(movePacket.ObjInfo);
                    break;

                case (ushort)MsgType.RollbackPlayer:
                    PlayerManager.Instance.PlayerObjDic[playerId].OnTriggerPlayerRollback(movePacket.ObjInfo);
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

            switch ((ushort)damagePacket.MessageType)
            {
                case (ushort)MsgType.DamagePlayer:
                    // 체력 감소, 공격자가 본인이면 데미지 표시 띄우기, 피해자가 본인이면 빨간 이펙트 띄우기
                    PlayerController hitPlayer;
                    if (damagePacket.HitId == ClientProgram.Instance.ClientId)
                    {
                        // 본인 체력 감소
                        hitPlayer = PlayerManager.Instance.PlayerObjDic[damagePacket.HitId];
                        hitPlayer.OnTriggerUpdateHpbar((int)damagePacket.CurHp - (int)damagePacket.Damage);
                        UIManager.Instance.OnTriggerDamaged();
                    }
                    break;

                case (ushort)MsgType.DamageMonster:
                    // 체력 감소, 공격자가 본인이면 데미지 표시 띄우기
                    MonsterManager.Instance.MonsterObjDic[damagePacket.HitId].OnTriggerHit(damagePacket.Damage, damagePacket.CurHp, damagePacket.MaxHp);
                    break;

                case (ushort)MsgType.HealPlayer:
                    // 체력 회복
                    PlayerManager.Instance.PlayerObjDic[ClientProgram.Instance.ClientId].OnTriggerUpdateHpbar(damagePacket.CurHp);
                    break;
            }
        }

        // Score 패킷 받음
        public void OnRecvScore(ArraySegment<byte> buffer)
        {
            ScorePacket scorePacket = new ScorePacket();
            scorePacket.Read(buffer);

            if (scorePacket.PlayerScore.Count == 1 && scorePacket.PlayerScore[0].Name.Equals("")) // 자기 점수 업데이트
                UIManager.Instance.OnTriggerUpdateMyScore(scorePacket.PlayerScore[0].Score);
            else
                UIManager.Instance.OnTriggerUpdateScoreBoard(scorePacket.PlayerScore); // top5 점수보드 업데이트
        }
    }
}