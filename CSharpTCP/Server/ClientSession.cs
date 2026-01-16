using ServerCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Server
{
    public class ClientSession : PacketSession
    {
        public string Name = "";   // 이름
        public ushort CurHP = 100; // 현재 체력
        public ushort MaxHP = 100; // 최대 체력
        public float CollisionRadius = 1.5f;       // 반지름(충돌 반경)
        public ObjectInfo Info = new ObjectInfo(); // 정보(위치, 회전)

        private float _moveSpeed = 4.0f;       // 이동 속도
        private float _updateInterval = 0.25f; // 갱신 간격
        private DateTime _beforeRequestTime;   // 이전 요청 주기
        private ushort _heal = 20; // 체력 회복량(몬스터 처치 시)

        // 연결되면 실행
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            // 연결됐다고 클라이언트에 전송
            PlayerInfoReq packet = new PlayerInfoReq() { PlayerId = 0, Name = " " };
            ArraySegment<byte> segment = packet.Write(); // 직렬화

            if (segment != null)
                Send(segment);
        }

        // 연결 해제되면 실행
        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected : {endPoint}");

            // 연결 해제된 플레이어의 오브젝트 제거
            CreateRemovePacket packet = new CreateRemovePacket() { Id = Info.Id, MessageType = (ushort)MsgType.RemovePlayer };
            ArraySegment<byte> segment = packet.Write();
            SessionManager.Instance.BroadcastExcept(segment, Info.Id);

            SessionManager.Instance.Remove(this); // 딕셔너리에서 삭제
            SpatialGrid.Instance.RemovePlayer(this);
            ScoreManager.Instance.RemoveScore(Info.Id);
        }

        // 패킷 보내면 실행
        public override void OnSend(int numOfBytes)
        {

        }

        // 패킷 받으면 실행
        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            // 역직렬화
            int count = 0;
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            count += 2;
            ushort packetType = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
            count += 2;

            if (packetType == 0)
                Disconnect();

            // 패킷 유형에 맞는 코드 실행
            switch ((PacketType)packetType)
            {
                case PacketType.PlayerInfoReq: OnRecvPlayerInfoReq(buffer); break;
                case PacketType.PlayerInfoOk:  OnRecvPlayerInfoOk(buffer);  break;
                case PacketType.CreateRemove:  OnRecvCreateRemove(buffer);  break;
                case PacketType.Move:          OnRecvMove(buffer);          break;
                case PacketType.Chat:          OnRecvChat(buffer);          break;
            }
        }

        // PlayerInfoReq 패킷 받음
        public void OnRecvPlayerInfoReq(ArraySegment<byte> buffer)
        {
            PlayerInfoReq playerInfo = new PlayerInfoReq();
            playerInfo.Read(buffer);

            Console.WriteLine($"InfoReq | ID : {playerInfo.PlayerId}, name : {playerInfo.Name}\n");

            Info.Id = playerInfo.PlayerId;
            Name = playerInfo.Name;
            SessionManager.Instance.Add(this); // 딕셔너리에 클라이언트 추가
        }

        // PlayerInfoOk 패킷 받음
        public void OnRecvPlayerInfoOk(ArraySegment<byte> buffer)
        {
            PlayerInfoOk playerInfo = new PlayerInfoOk();
            playerInfo.Read(buffer);

            Console.WriteLine($"InfoOK | ID: {playerInfo.PlayerId}\n");
        }

        // CreateRemove 패킷 받음
        public void OnRecvCreateRemove(ArraySegment<byte> buffer)
        {
            CreateRemovePacket crPacket = new CreateRemovePacket();
            crPacket.Read(buffer);

            Console.WriteLine($"CreateRemove | ID: {crPacket.Id}, messageType: {(MsgType)crPacket.MessageType}\n");

            
            switch ((int)crPacket.MessageType)
            {
                case (int)MsgType.CreatePlayer:
                case (int)MsgType.RemovePlayer:
                    ArraySegment<byte> playerSegment = crPacket.Write();
                    if (crPacket != null)
                        SessionManager.Instance.BroadcastAll(playerSegment); // 플레이어 생성/삭제 모든 클라이언트에 전송
                    break;

                case (int)MsgType.CreateMissile:
                    // 미사일 생성 요청 받으면 각 클라에 미사일 띄우기
                    ArraySegment<byte> missileSegment = crPacket.Write();
                    if (crPacket != null)
                    {
                        MissileManager.Instance.Add(Info);
                        SessionManager.Instance.BroadcastAll(missileSegment); // 미사일 생성 모든 클라이언트에 전송
                    }
                    break;
            }
        }

        // Move 패킷 받음
        public void OnRecvMove(ArraySegment<byte> buffer)
        {
            MovePacket movePacket = new MovePacket();
            movePacket.Read(buffer);

            // MovePacket은 플레이어, 몬스터, 총알의 이동을 처리하지만 서버는 플레이어 이외는 받을 일이 없다.
            if (movePacket.MessageType != (ushort)MsgType.MovePlayer)
                return;

            Vector3 newPos = movePacket.ObjInfo.Position;
            Vector3 newRot = movePacket.ObjInfo.Rotation;

            Vector3 prev = Info.Position;
            Info.Position = newPos;
            SpatialGrid.Instance.UpdatePlayer(this, prev);

            // 이동, 회전값에 이상이 없는지 검사 후 처리, 문제가 있다면 해당 클라 위치를 롤백처리, 문제가 없다면 다른 클라에게 전송
            bool _isrollback = RollbackCheck(newPos);

            // 롤백해야 하는 경우
            if (_isrollback)
            {
                _isrollback = false;
                movePacket.MessageType = (ushort)MsgType.RollbackPlayer;
                movePacket.ObjInfo = Info;

                ArraySegment<byte> rollbackSegment = movePacket.Write();
                Send(rollbackSegment); // 이전 상태 전송
                return;
            }

            Info.Position = newPos;
            Info.Rotation = newRot;
            
            ArraySegment<byte> segment = movePacket.Write();
            if (movePacket != null)
                SessionManager.Instance.BroadcastExcept(segment, Info.Id); // 본인을 제외한 다른 클라리언트들에 위치정보 전송
        }

        // Chat 패킷 받음
        public void OnRecvChat(ArraySegment<byte> buffer)
        {
            ChatPacket recvChatPacket = new ChatPacket();
            recvChatPacket.Read(buffer);

            // 모든 플레이어들에게 채팅 전송
            string sendChat = SessionManager.Instance.Sessions[recvChatPacket.PlayerId].Name + ": " + recvChatPacket.Chat;
            ChatPacket sendChatPacket = new ChatPacket() { PlayerId = recvChatPacket.PlayerId, Chat = sendChat };
            ArraySegment<byte> segment = sendChatPacket.Write();
            if (sendChatPacket != null)
                SessionManager.Instance.BroadcastAll(segment);
        }


        // 롤백 여부 검사
        public bool RollbackCheck(Vector3 newPos)
        {
            bool isrollback = false;

            // 요청 주기가 지나치게 짧은 경우 롤백
            TimeSpan interval = DateTime.Now - _beforeRequestTime;
            if (interval.TotalSeconds < _updateInterval * 0.9f)
            {
                // 로그 출력
                Console.WriteLine("Frequent Request");
                Console.WriteLine($"호출 간격: {interval.TotalSeconds} {_updateInterval}");
                isrollback = true;
            }
            _beforeRequestTime = DateTime.Now;

            // 속도가 기준치보다 높은 경우 롤백
            float limitSpeed = _moveSpeed * _updateInterval * 1.1f;
            if (Vector3.Distance(Info.Position, newPos) > limitSpeed)
            {
                // 로그 출력
                Console.WriteLine("Abnormal Movement");
                Console.WriteLine($"이전 위치: {Info.Position}, 새 위치: {newPos}, 이동 거리: {Vector3.Distance(Info.Position, newPos)}, 한계 거리: {limitSpeed}");
                isrollback = true;
            }

            return isrollback;
        }

        // 피격
        public void Hit(ushort dmg)
        {
            if (CurHP <= dmg) // 체력 0 시 소멸
            {
                CurHP = 0;
                CreateRemovePacket crPacket = new CreateRemovePacket() { MessageType = (ushort)MsgType.DieMe, Id = Info.Id };
                ArraySegment<byte> playerSegment = crPacket.Write();
                Send(playerSegment); // 체력 0 시 죽었다고 알려주기
            }
            else
                CurHP -= dmg;
        }

        // 체력 회복(몬스터 처치 시)
        public void Heal()
        {
            CurHP += _heal;
            if (CurHP > MaxHP)
                CurHP = MaxHP;

            DamagePacket sendChatPacket = new DamagePacket() { MessageType = (ushort)MsgType.HealPlayer, AttackId = Info.Id, HitId = Info.Id, CurHp = CurHP, MaxHp = MaxHP, Damage = 0 };
            ArraySegment<byte> segment = sendChatPacket.Write();
            Send(segment); // 체력 회복 알려주기
        }
    }
}