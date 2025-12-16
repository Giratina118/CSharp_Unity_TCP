using ServerCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class ClientSession : PacketSession
    {
        public string Name = ""; // 이름
        public ObjectInfo Info = new ObjectInfo();

        private float _moveSpeed = 3.0f;       // 이동 속도
        private float _updateInterval = 0.25f; // 갱신 간격
        private DateTime _beforeRequestTime;

        // 연결되면 실행
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            // 연결됐다고 클라이언트에 전송
            PlayerInfoReq packet = new PlayerInfoReq() { playerId = 0, name = " " };
            ArraySegment<byte> segment = packet.Write(); // 직렬화

            if (segment != null)
                Send(segment);
        }

        // 연결 해제되면 실행
        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected : {endPoint}");

            // 연결 해제된 플레이어의 오브젝트 제거
            PlayerCreateRemovePacket packet = new PlayerCreateRemovePacket() { playerId = Info.id, messageType = (ushort)MsgType.Remove };
            ArraySegment<byte> segment = packet.Write();
            SessionManager.Instance.BroadcastExcept(segment, Info.id);

            SessionManager.Instance.Remove(this); // 딕셔너리에서 삭제
        }

        // 패킷 보내면 실행
        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"OnSend");
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

            Console.WriteLine($"InfoReq | ID : {playerInfo.playerId}, name : {playerInfo.name}\n");

            Name = playerInfo.name;
            SessionManager.Instance.Add(this); // 딕셔너리에 클라이언트 추가
        }

        // PlayerInfoOk 패킷 받음
        public void OnRecvPlayerInfoOk(ArraySegment<byte> buffer)
        {
            PlayerInfoOk playerInfo = new PlayerInfoOk();
            playerInfo.Read(buffer);

            Console.WriteLine($"InfoOK | ID: {playerInfo.playerId}\n");
        }

        // CreateRemove 패킷 받음
        public void OnRecvCreateRemove(ArraySegment<byte> buffer)
        {
            PlayerCreateRemovePacket crPacket = new PlayerCreateRemovePacket();
            crPacket.Read(buffer);

            Console.WriteLine($"CreateRemove | ID: {crPacket.playerId}, messageType: {crPacket.messageType}\n");

            ArraySegment<byte> segment = crPacket.Write();
            if (crPacket != null)
                SessionManager.Instance.BroadcastAll(segment); // 생성/삭제 모든 클라이언트에 전송
        }

        // Move 패킷 받음
        public void OnRecvMove(ArraySegment<byte> buffer)
        {
            MovePacket movePacket = new MovePacket();
            movePacket.Read(buffer);

            // MovePacket은 플레이어, 몬스터, 총알의 이동을 처리하지만 서버는 플레이어 이외는 받을 일이 없다.
            if (movePacket.messageType != (ushort)MsgType.PlayerMove)
                return;

            /*
            if (_beforeRequestTime == default)
            {
                _beforeRequestTime = DateTime.Now;

                Info.position = movePacket.playerInfo.position;
                Info.rotation = movePacket.playerInfo.rotation;

                return;
            }
            */

            Console.WriteLine($"movePacket | msgType: {movePacket.messageType}, ID: {movePacket.playerInfo.id}");

            // 이동, 회전값에 이상이 없는지 검사 후 처리, 문제가 있다면 해당 클라 위치를 롤백처리, 문제가 없다면 다른 클라에게 전송
            Vector3 newPos = movePacket.playerInfo.position;
            //Console.WriteLine($"위치 변화량: {Vector3.Distance(Info.position, newPos)}, {{{newPos.X - Info.position.X}, {newPos.Y - Info.position.Y}, {newPos.Z - Info.position.Z}}}\n");

            //bool _isrollback = false;

            // 요청 주기가 지나치게 짧은 경우 롤백
            /*
            TimeSpan interval = DateTime.Now - _beforeRequestTime;
            Console.WriteLine($"호출 간격: {interval.TotalMilliseconds} {_updateInterval}");
            if (interval.TotalSeconds < _updateInterval * 0.9f)
            {
                // 로그 출력
                Console.WriteLine("Frequent Request");
                _isrollback = true;
            }
            _beforeRequestTime = DateTime.Now;
            */

            // 속도가 기준치보다 높은 경우 롤백
            /*
            float limitSpeed = _moveSpeed * _updateInterval * 1.1f;
            if (Vector3.Distance(Info.position, newPos) > limitSpeed)
            {
                // 로그 출력
                Console.WriteLine("Abnormal Movement");
                _isrollback = true;
            }
            */
            /*
            if (_isrollback)
            {
                _isrollback = false;
                movePacket.playerInfo = Info;
                ArraySegment<byte> rollbackSegment = movePacket.Write();
                Send(rollbackSegment);
                return;
            }
            */

            Info.position = movePacket.playerInfo.position;
            Info.rotation = movePacket.playerInfo.rotation;
            Console.WriteLine($"movePacket | msgType: {movePacket.messageType}, ID: {movePacket.playerInfo.id}, " +
                $"pos: {{{Info.position.X}, {Info.position.Y}, {Info.position.Z}}}, rot: {{{Info.rotation.X}, {Info.rotation.Y}, {Info.rotation.Z}}}\n");

            ArraySegment<byte> segment = movePacket.Write();
            if (movePacket != null)
                SessionManager.Instance.BroadcastExcept(segment, Info.id); // 본인을 제외한 다른 클라리언트들에 위치정보 전송
        }

        // Chat 패킷 받음
        public void OnRecvChat(ArraySegment<byte> buffer)
        {
            ChatPacket recvChatPacket = new ChatPacket();
            recvChatPacket.Read(buffer);

            Console.WriteLine($"Chat | ID: {recvChatPacket.playerId}, name: {Name}, chat: {recvChatPacket.chat}\n");

            // 모든 플레이어들에게 채팅 전송
            string sendChat = SessionManager.Instance.Sessions[recvChatPacket.playerId].Name + ": " + recvChatPacket.chat;
            ChatPacket sendChatPacket = new ChatPacket() { playerId = recvChatPacket.playerId, chat = sendChat };

            ArraySegment<byte> segment = sendChatPacket.Write();
            if (sendChatPacket != null)
                SessionManager.Instance.BroadcastAll(segment);
        }
    }
}