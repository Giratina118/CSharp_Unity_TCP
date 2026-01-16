using ServerCore;
using System;
using System.Collections.Generic;
using System.Data;
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
        PlayerInfoReq = 1, // 클라 -> 서버 플레이어 정보 전송
        PlayerInfoOk,      // 서버 -> 클라 정보 받았다고 전달, 플레이어 번호 전달
        CreateRemove,      // 플레이어 오브젝트 생성/삭제, 어떤 플레이어의 오브젝트를 생성/삭제해야 하는지
        ObjList,           // 오브젝트 리스트 전송
        Move,              // 어떤 플레이어를 어디로 움직여야 하는지
        Chat,              // 어떤 플레이어가 어떤 채팅을 쳤는지
        Damage,            // 유저/몬스터 데미지 전송
        Score,             // 점수 전송

        Max,
    }

    // 메세지 유형
    enum MsgType
    {
        // CreateRemovePacket
        CreatePlayer = 1, // 플레이어 오브젝트 생성
        RemovePlayer,     // 플레이어 오브젝트 삭제
        RespawnMonster,   // 몬스터 오브젝트 생성
        RemoveMonster,    // 몬스터 오브젝트 삭제
        CreateMissile,    // 미사일 오브젝트 생성
        RemoveMissile,    // 미사일 오브젝트 삭제
        DieMe,            // 자신이 죽었다고 알리주기

        // ObjListPacket
        CreateAllPlayer,    // 모든 플레이어 생성(최초 초기화)
        CreateAllMonster,   // 모든 몬스터 생성(최초 초기화)
        CreateAllStructure, // 모든 건물 생성(최초 초기화)
        MonsterInfoList,    // 모든 몬스터 정보(위치) 전송

        // MovePacket
        MovePlayer,     // 플레이어 이동
        MoveMissile,    // 미사일 이동
        RollbackPlayer, // 플레이어 롤백

        // DamagePacket
        DamagePlayer,   // 플레이어 데미지
        DamageMonster,  // 몬스터 데미지
        HealPlayer,     // 플레이어 회복

        Max,
    };

    // 오브젝트 유형
    enum ObjType
    {
        Player = 1, // 플레이어
        Missile,    // 미사일
        Monster,    // 몬스터
        Structure,  // 건물

        Max,
    }

    // 오브젝트 정보(id, 위치)
    public struct ObjectInfo
    {
        public ushort ObjType;   // 어떤 종류의 오브젝트인지
        public long Id;          // id
        public Vector3 Position; // 위치 정보
        public Vector3 Rotation; // 회전 정보

        public bool Write(Span<byte> span, ref ushort count)
        {
            bool success = true;

            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.ObjType);
            count += sizeof(ushort); // 오브젝트 유형
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.Id);
            count += sizeof(long); // id
            success &= WriteVector3(Position, ref span, ref count); // 이동
            success &= WriteVector3(Rotation, ref span, ref count); // 회전

            return success;
        }

        public void Read(ReadOnlySpan<byte> span, ref ushort count)
        {
            ObjType = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 오브젝트 유형
            Id = BitConverter.ToInt64(span.Slice(count, span.Length - count));
            count += sizeof(long); // id
            ReadVector3(ref Position, span, ref count); // 이동
            ReadVector3(ref Rotation, span, ref count); // 회전
        }

        public static bool WriteVector3(Vector3 vec, ref Span<byte> span, ref ushort count)
        {
            bool success = true;

            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), vec.X);
            count += sizeof(float); // x좌표
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), vec.Y);
            count += sizeof(float); // y좌표
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), vec.Z);
            count += sizeof(float); // z좌표
            return success;
        }

        public static void ReadVector3(ref Vector3 vec, ReadOnlySpan<byte> span, ref ushort count)
        {
            vec.X = BitConverter.ToSingle(span.Slice(count, span.Length - count));
            count += sizeof(float); // x좌표
            vec.Y = BitConverter.ToSingle(span.Slice(count, span.Length - count));
            count += sizeof(float); // y좌표
            vec.Z = BitConverter.ToSingle(span.Slice(count, span.Length - count));
            count += sizeof(float); // z좌표
        }
    }

    // 점수 정보
    public struct PlayerScore
    {
        public long Id;     // 플레이어 아이디
        public int Score;   // 점수
        public string Name; // 이름

        public PlayerScore(long id, int score, string name)
        {
            Id = id;
            Score = score;
            Name = name;
        }
    }


    // 패킷 기본 부모 클래스
    public abstract class Packet
    {
        public ushort Size;       // 패킷 크기
        public ushort PacketType; // 패킷 유형

        // 최상위 Class인 Packet에 인터페이스 생성
        public abstract ArraySegment<byte> Write();
        public abstract void Read(ArraySegment<byte> segment);

        /*
        public bool WriteDate(ref Span<byte> span, ref ushort count)
        {
            bool result = BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.PacketType);
            count += sizeof(ushort); // 패킷 아이디
            return true;
        }
        */
    }

    // 유저 확인용 정보를 전송 (클라 -> 서버)
    class PlayerInfoReq : Packet
    {
        public long PlayerId; // 플레이어 아이디
        public string Name;   // 이름(닉네임)

        public PlayerInfoReq() { this.PacketType = (ushort)Server.PacketType.PlayerInfoReq; }

        public override ArraySegment<byte> Write()
        {
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

            bool success = true; // 쓰기 성공 여부
            ushort count = 0;    // 지금까지 몇 Byte를 Buffer에 밀어 넣었는가?

            Span<byte> span = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.PacketType);
            count += sizeof(ushort); // 패킷 아이디
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.PlayerId);
            count += sizeof(long);   // 플레이어 아이디

            // string 처리 (Buffer에 2Byte인 string len을 위한 공간을 남겨둔 채로 string data를 먼저 삽입 후 string len 삽입) 
            // Buffer에 string data를 삽입함과 동시에 string len을 반환
            ushort nameLen = (ushort)Encoding.Unicode.GetBytes(this.Name, 0, Name.Length, segment.Array, segment.Offset + count + sizeof(ushort));
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
            this.PlayerId = BitConverter.ToInt64(span.Slice(count, span.Length - count)); // Slice는 실질적으로 Span에 변화를 주지 않음
            count += sizeof(long);   // 플레이어 아이디

            // string 처리
            // playerId 이후(count로 세는 중) 16bit(2byte)짜리 unsigned 정수형 정보 -> 받은 문자열 길이
            ushort nameLen = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 플레이어 네임
            this.Name = Encoding.Unicode.GetString(span.Slice(count, nameLen)); // GetString()는 Byte 배열을 받아 string으로 변환
            count += nameLen;
        }
    }

    // 유저 정보 확인 완료 전송 (서버 -> 클라)
    class PlayerInfoOk : Packet
    {
        public long PlayerId; // 플레이어 아이디

        public PlayerInfoOk() { this.PacketType = (ushort)Server.PacketType.PlayerInfoOk; }

        public override ArraySegment<byte> Write()
        {
            Console.WriteLine("OnWriteOk");

            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

            bool success = true;
            ushort count = 0; // 지금까지 몇 Byte를 Buffer에 밀어 넣었는가?

            Span<byte> span = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.PacketType);
            count += sizeof(ushort); // 패킷 아이디
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.PlayerId);
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
            this.PlayerId = BitConverter.ToInt64(span.Slice(count, span.Length - count));
            count += sizeof(long); // 플레이어 아이디
        }
    }

    // 유저 오브젝트 생성/삭제 관리
    class CreateRemovePacket : Packet
    {
        public long Id;            // 어떤 대상을
        public ushort MessageType; // 생성 혹은 삭제할지

        public CreateRemovePacket() { this.PacketType = (ushort)Server.PacketType.CreateRemove; }

        public override ArraySegment<byte> Write()
        {
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

            bool success = true;
            ushort count = 0; // 지금까지 몇 Byte를 Buffer에 밀어 넣었는가?

            Span<byte> span = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.PacketType); // 패킷 종류
            count += sizeof(ushort); // 패킷 유형

            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.Id); // 플레이어 id
            count += sizeof(long);   // 플레이어 아이디
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.MessageType); // 메시지 타입
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

            this.Id = BitConverter.ToInt64(span.Slice(count, span.Length - count)); // Slice는 실질적으로 Span에 변화를 주지 않음
            count += sizeof(long);   // 플레이어 아이디
            this.MessageType = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 메시지 타입(생성/삭제 여부)
        }
    }

    // 최초 연결 시 기존에 들어와 있던 플레이어 생성
    class ObjListPacket : Packet
    {
        public ushort MessageType; // 메시지 유형
        public List<ObjectInfo> Infos = new List<ObjectInfo>(); // 오브젝트 정보 리스트

        public ObjListPacket() { this.PacketType = (ushort)Server.PacketType.ObjList; }

        public override ArraySegment<byte> Write()
        {
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

            bool success = true;
            ushort count = 0; // 지금까지 몇 Byte를 Buffer에 밀어 넣었는가?

            Span<byte> span = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.PacketType);
            count += sizeof(ushort); // 패킷 아이디
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.MessageType);
            count += sizeof(ushort); // 메시지 유형

            // 구조체 리스트 id, 좌표 쓰기
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), (ushort)Infos.Count);
            count += sizeof(ushort);
            foreach (ObjectInfo infos in Infos)
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
            MessageType = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 메시지 유형

            // 구조체 리스트 id, 좌표 읽기
            Infos.Clear();
            ushort infoLen = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort);
            for (int i = 0; i < infoLen; i++)
            {
                ObjectInfo info = new ObjectInfo();
                info.Read(span, ref count);
                Infos.Add(info);
            }
        }
    }

    // 플레이어 이동(위치, 회전 정보) 관리
    class MovePacket : Packet
    {
        public ushort MessageType; // 메시지 유형
        public ObjectInfo ObjInfo; // 플레이어 아이디, 위치, 회전 정보

        public MovePacket() { this.PacketType = (ushort)Server.PacketType.Move; }

        public override ArraySegment<byte> Write()
        {
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

            bool success = true;
            ushort count = 0; // 지금까지 몇 Byte를 Buffer에 밀어 넣었는가?

            Span<byte> span = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.PacketType);
            count += sizeof(ushort); // 패킷 유형
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.MessageType);
            count += sizeof(ushort); // 메시지 타입(어느 오브젝트가 이동하는지)

            ObjInfo.Write(span, ref count);

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

            this.MessageType = BitConverter.ToUInt16(span.Slice(count, span.Length - count)); // Slice는 실질적으로 Span에 변화를 주지 않음
            count += sizeof(ushort); // 메시지 타입

            ObjInfo.Read(span, ref count);
        }
    }

    // 채팅 관리
    class ChatPacket : Packet
    {
        public long PlayerId; // 플레이어 아이디
        public string Chat;   // 채팅 내용

        public ChatPacket() { this.PacketType = (ushort)Server.PacketType.Chat; }

        public override ArraySegment<byte> Write()
        {
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

            bool success = true;
            ushort count = 0; // 지금까지 몇 Byte를 Buffer에 밀어 넣었는가?

            Span<byte> span = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.PacketType);
            count += sizeof(ushort); // 패킷 아이디

            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.PlayerId);
            count += sizeof(long);   // 플레이어 아이디

            ushort chatLen = (ushort)Encoding.Unicode.GetBytes(this.Chat, 0, Chat.Length, segment.Array, segment.Offset + count + sizeof(ushort));
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

            this.PlayerId = BitConverter.ToInt64(span.Slice(count, span.Length - count)); // Slice는 실질적으로 Span에 변화를 주지 않음
            count += sizeof(long);   // 플레이어 아이디

            ushort chatLen = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 플레이어 네임
            this.Chat = Encoding.Unicode.GetString(span.Slice(count, chatLen));
            count += chatLen;
        }
    }

    // 데미지 관리
    class DamagePacket : Packet
    {
        public long HitId;         // 어떤 유저/몬스터가 데미지를 입었는지
        public long AttackId;      // 어떤 유저가 데미지를 입혔는지
        public ushort MessageType; // 유저인지 몬스터인지
        public ushort Damage;      // 얼마의 피해를 입었는지
        public ushort CurHp;       // 현재 체력
        public ushort MaxHp;       // 최대 체력

        public DamagePacket() { this.PacketType = (ushort)Server.PacketType.Damage; }

        public override ArraySegment<byte> Write()
        {
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

            bool success = true;
            ushort count = 0; // 지금까지 몇 Byte를 Buffer에 밀어 넣었는가?

            Span<byte> span = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.PacketType);
            count += sizeof(ushort); // 패킷 유형

            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.HitId);
            count += sizeof(long);   // 플레이어/몬스터 아이디
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.AttackId);
            count += sizeof(long);   // 플레이어 아이디
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.MessageType);
            count += sizeof(ushort); // 메시지 타입(생성/삭제 여부)
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.Damage);
            count += sizeof(ushort); // 데미지
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.CurHp);
            count += sizeof(ushort); // 현재 체력
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.MaxHp);
            count += sizeof(ushort); // 현재 체력

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

            this.HitId = BitConverter.ToInt64(span.Slice(count, span.Length - count)); // Slice는 실질적으로 Span에 변화를 주지 않음
            count += sizeof(long);   // 플레이어/몬스터 아이디
            this.AttackId = BitConverter.ToInt64(span.Slice(count, span.Length - count));
            count += sizeof(long);   // 플레이어 아이디
            this.MessageType = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 메시지 타입(생성/삭제 여부)
            this.Damage = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 데미지
            this.CurHp = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 현재 체력
            this.MaxHp = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort); // 현재 체력
        }
    }

    // 점수 전송 (top5 점수)
    class ScorePacket : Packet
    {
        public List<PlayerScore> PlayerScore = new List<PlayerScore>(); // 점수 정보 리스트

        public ScorePacket() { this.PacketType = (ushort)Server.PacketType.Score; }

        public override ArraySegment<byte> Write()
        {
            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

            bool success = true;
            ushort count = 0; // 지금까지 몇 Byte를 Buffer에 밀어 넣었는가?

            Span<byte> span = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort); // 패킷 사이즈
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.PacketType);
            count += sizeof(ushort); // 패킷 아이디

            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), (ushort)PlayerScore.Count);
            count += sizeof(ushort);

            for (int i = 0; i < PlayerScore.Count; i++)
            {
                success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.PlayerScore[i].Id);
                count += sizeof(long); // 플레이어 아이디
                success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.PlayerScore[i].Score);
                count += sizeof(int);  // 점수

                ushort nameLen = (ushort)Encoding.Unicode.GetBytes(this.PlayerScore[i].Name, 0, PlayerScore[i].Name.Length, segment.Array, segment.Offset + count + sizeof(ushort));
                success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), nameLen); // 길이
                count += sizeof(ushort); // 이름
                count += nameLen;
            }

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

            PlayerScore.Clear();
            ushort scoreLen = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort);
            for (int i = 0; i < scoreLen; i++)
            {
                long id = BitConverter.ToInt64(span.Slice(count, span.Length - count));
                count += sizeof(long); // id
                int score = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
                count += sizeof(int); // 점수

                ushort nameLen = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
                count += sizeof(ushort); // 플레이어 네임
                string name = Encoding.Unicode.GetString(span.Slice(count, nameLen));
                count += nameLen;

                PlayerScore scores = new PlayerScore(id, score, name);
                PlayerScore.Add(scores);
            }
        }
    }
}