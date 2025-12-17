using ServerCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Server
{
    // 패킷 유형
    public enum PacketType
    {
        PlayerInfoReq = 1,  // 클라 -> 서버 플레이어 정보 전송
        PlayerInfoOk,       // 서버 -> 클라 정보 받았다고 전달, 플레이어 번호 전달
        CreateRemove,       // 플레이어 오브젝트 생성/삭제, 어떤 플레이어의 오브젝트를 생성/삭제해야 하는지
        CreateAll,          // 처음 들어왔을 때 먼저 들어와 있던 플레이어들 모두 생성
        Move,               // 어떤 플레이어를 어디로 움직여야 하는지
        Chat,               // 어떤 플레이어가 어떤 채팅을 쳤는지

        Max,
    }

    // 메세지 유형
    enum MsgType
    {
        Create = 1,  // 플레이어 오브젝트 생성
        Remove,      // 플레이어 오브젝트 삭제

        CreateAllPlayer,
        CreateAllMonster,

        MovePlayer,  // 플레이어 이동
        MoveMonster, // 몬스터 이동
        MoveMissile, // 미사일 이동
        RollbackPlayer, // 플레이어 롤백

        Max,
    };

    // 플레이어 정보(id, 위치)
    public struct ObjectInfo
    {
        public long id; // id
        public Vector3 position; // 위치 정보
        public Vector3 rotation; // 회전 정보

        public bool Write(Span<byte> span, ref ushort count)
        {
            bool success = true;

            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.id);
            count += sizeof(long);  // id
            success &= Utils.WriteVector3(position, ref span, ref count); // 이동
            success &= Utils.WriteVector3(rotation, ref span, ref count); // 회전

            return success;
        }

        public void Read(ReadOnlySpan<byte> span, ref ushort count)
        {
            id = BitConverter.ToInt64(span.Slice(count, span.Length - count));
            count += sizeof(long);  // id
            Utils.ReadVector3(ref position, span, ref count); // 이동
            Utils.ReadVector3(ref rotation, span, ref count); // 회전
        }
    }

    // 패킷 기본 부모 클래스
    public abstract class Packet
    {
        public ushort size;       // 패킷 크기
        public ushort packetType; // 패킷 유형

        // 최상위 Class인 Packet에 인터페이스 생성
        public abstract ArraySegment<byte> Write();
        public abstract void Read(ArraySegment<byte> segment);
    }

    // 유저 확인용 정보를 전송 (클라 -> 서버)
    class PlayerInfoReq : Packet
    {
        public long playerId; // 플레이어 아이디
        public string name;   // 이름(닉네임)

        public PlayerInfoReq() { this.packetType = (ushort)PacketType.PlayerInfoReq; }

        public override ArraySegment<byte> Write()
        {
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

            bool success = true; // 쓰기 성공 여부
            ushort count = 0; // 지금까지 몇 Byte를 Buffer에 밀어 넣었는가?

            Span<byte> span = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.packetType);
            count += sizeof(ushort); // 패킷 아이디
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.playerId);
            count += sizeof(long);   // 플레이어 아이디

            // string 처리 (Buffer에 2Byte인 string len을 위한 공간을 남겨둔 채로 string data를 먼저 삽입 후 string len 삽입) 
            // Buffer에 string data를 삽입함과 동시에 string len을 반환
            ushort nameLen = (ushort)Encoding.Unicode.GetBytes(this.name, 0, name.Length, segment.Array, segment.Offset + count + sizeof(ushort));
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), nameLen); // 길이
            count += sizeof(ushort); // 플레이어 네임
            count += nameLen;

            success &= BitConverter.TryWriteBytes(span, count); // size는 작업이 끝난 뒤 초기화

            if (success == false)
                return null;

            return SendBufferHelper.Close(count);
        }

        public override void Read(ArraySegment<byte> segment)
        {
            ushort count = 0;
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            count += sizeof(ushort); // 패킷 아이디
            this.playerId = BitConverter.ToInt64(span.Slice(count, span.Length - count)); // Slice는 실질적으로 Span에 변화를 주지 않음
            count += sizeof(long);   // 플레이어 아이디

            // string 처리
            // playerId 이후(count로 세는 중) 16bit(2byte)짜리 unsigned 정수형 정보 -> 받은 문자열 길이
            ushort nameLen = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 플레이어 네임
            this.name = Encoding.Unicode.GetString(span.Slice(count, nameLen)); // GetString()는 Byte 배열을 받아 string으로 변환
            count += nameLen;
        }
    }

    // 유저 정보 확인 완료 전송 (서버 -> 클라)
    class PlayerInfoOk : Packet
    {
        public long playerId; // 플레이어 아이디

        public PlayerInfoOk() { this.packetType = (ushort)PacketType.PlayerInfoOk; }

        public override ArraySegment<byte> Write()
        {
            Console.WriteLine("OnWriteOk");

            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

            bool success = true;
            ushort count = 0; // 지금까지 몇 Byte를 Buffer에 밀어 넣었는가?

            Span<byte> span = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.packetType);
            count += sizeof(ushort); // 패킷 아이디
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.playerId);
            count += sizeof(long);   // 플레이어 아이디

            success &= BitConverter.TryWriteBytes(span, count); // size는 작업이 끝난 뒤 초기화

            if (success == false)
                return null;

            return SendBufferHelper.Close(count);
        }

        public override void Read(ArraySegment<byte> segment)
        {
            ushort count = 0;
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            count += sizeof(ushort); // 패킷 아이디
            this.playerId = BitConverter.ToInt64(span.Slice(count, span.Length - count));
            count += sizeof(long); // 플레이어 아이디
        }
    }

    // 유저 오브젝트 생성/삭제 관리
    class PlayerCreateRemovePacket : Packet
    {
        public long playerId;      // 어떤 유저를
        public ushort messageType; // 생성 혹은 삭제할지

        public PlayerCreateRemovePacket() { this.packetType = (ushort)PacketType.CreateRemove; }

        public override ArraySegment<byte> Write()
        {
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

            bool success = true;
            ushort count = 0; // 지금까지 몇 Byte를 Buffer에 밀어 넣었는가?

            Span<byte> span = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.packetType); // 패킷 종류
            count += sizeof(ushort); // 패킷 유형

            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.playerId); // 플레이어 id
            count += sizeof(long);   // 플레이어 아이디
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.messageType); // 메시지 타입
            count += sizeof(ushort); // 메시지 타입(생성/삭제 여부)


            success &= BitConverter.TryWriteBytes(span, count); // size는 작업이 끝난 뒤 초기화

            if (success == false)
                return null;

            return SendBufferHelper.Close(count);
        }

        public override void Read(ArraySegment<byte> segment)
        {
            ushort count = 0;
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            count += sizeof(ushort); // 패킷 아이디

            this.playerId = BitConverter.ToInt64(span.Slice(count, span.Length - count)); // Slice는 실질적으로 Span에 변화를 주지 않음
            count += sizeof(long);   // 플레이어 아이디
            this.messageType = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 메시지 타입(생성/삭제 여부)
        }
    }

    // 최초 연결 시 기존에 들어와 있던 플레이어 생성
    class CreateAll : Packet
    {
        public ushort messageType; // aptlwl dbgud
        public List<ObjectInfo> playerInfos = new List<ObjectInfo>();

        public CreateAll() { this.packetType = (ushort)PacketType.CreateAll; }

        public override ArraySegment<byte> Write()
        {
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

            bool success = true;
            ushort count = 0; // 지금까지 몇 Byte를 Buffer에 밀어 넣었는가?

            Span<byte> span = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.packetType);
            count += sizeof(ushort); // 패킷 아이디
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.messageType);
            count += sizeof(ushort); // 메시지 유형

            // 구조체 리스트 id, 좌표 쓰기
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), (ushort)playerInfos.Count);
            count += sizeof(ushort);
            foreach (ObjectInfo infos in playerInfos)
                success &= infos.Write(span, ref count);

            success &= BitConverter.TryWriteBytes(span, count); // size는 작업이 끝난 뒤 초기화

            if (success == false)
                return null;

            return SendBufferHelper.Close(count);
        }

        public override void Read(ArraySegment<byte> segment)
        {
            ushort count = 0;
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            count += sizeof(ushort); // 패킷 아이디
            messageType = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 메시지 유형

            // 구조체 리스트 id, 좌표 읽기
            playerInfos.Clear();
            ushort infoLen = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort);
            for (int i = 0; i < infoLen; i++)
            {
                ObjectInfo info = new ObjectInfo();
                info.Read(span, ref count);
                playerInfos.Add(info);
            }
        }
    }

    // 플레이어 이동(위치, 회전 정보) 관리
    class MovePacket : Packet
    {
        public ushort messageType;
        public ObjectInfo playerInfo; // 플레이어 아이디, 위치, 회전 정보

        public MovePacket() { this.packetType = (ushort)PacketType.Move; }

        public override ArraySegment<byte> Write()
        {
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

            bool success = true;
            ushort count = 0; // 지금까지 몇 Byte를 Buffer에 밀어 넣었는가?

            Span<byte> span = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.packetType);
            count += sizeof(ushort); // 패킷 유형
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.messageType);
            count += sizeof(ushort); // 메시지 타입(어느 오브젝트가 이동하는지)

            playerInfo.Write(span, ref count);

            success &= BitConverter.TryWriteBytes(span, count); // size는 작업이 끝난 뒤 초기화

            if (success == false)
                return null;

            return SendBufferHelper.Close(count);
        }

        public override void Read(ArraySegment<byte> segment)
        {
            ushort count = 0;

            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            count += sizeof(ushort); // 패킷 유형

            this.messageType = BitConverter.ToUInt16(span.Slice(count, span.Length - count)); // Slice는 실질적으로 Span에 변화를 주지 않음
            count += sizeof(ushort); // 메시지 타입

            playerInfo.Read(span, ref count);
        }
    }

    // 채팅 관리
    class ChatPacket : Packet
    {
        public long playerId; // 플레이어 아이디
        public string chat;   // 채팅 내용

        public ChatPacket() { this.packetType = (ushort)PacketType.Chat; }

        public override ArraySegment<byte> Write()
        {
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

            bool success = true;
            ushort count = 0; // 지금까지 몇 Byte를 Buffer에 밀어 넣었는가?

            Span<byte> span = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.packetType);
            count += sizeof(ushort); // 패킷 아이디

            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.playerId);
            count += sizeof(long);   // 플레이어 아이디

            ushort chatLen = (ushort)Encoding.Unicode.GetBytes(this.chat, 0, chat.Length, segment.Array, segment.Offset + count + sizeof(ushort));
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), chatLen); // 길이
            count += sizeof(ushort); // 채팅
            count += chatLen;


            success &= BitConverter.TryWriteBytes(span, count); // size는 작업이 끝난 뒤 초기화

            if (success == false)
                return null;

            return SendBufferHelper.Close(count);
        }

        public override void Read(ArraySegment<byte> segment)
        {
            ushort count = 0;
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            count += sizeof(ushort); // 패킷 아이디

            this.playerId = BitConverter.ToInt64(span.Slice(count, span.Length - count)); // Slice는 실질적으로 Span에 변화를 주지 않음
            count += sizeof(long);   // 플레이어 아이디

            ushort chatLen = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 플레이어 네임
            this.chat = Encoding.Unicode.GetString(span.Slice(count, chatLen));
            count += chatLen;
        }
    }
}