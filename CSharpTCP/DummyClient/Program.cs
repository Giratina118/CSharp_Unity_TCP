using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DummyClient
{
    public abstract class Packet
    {
        public ushort size;
        public ushort packetId;

        // 인터페이스 생성
        public abstract ArraySegment<byte> Write();
        public abstract void Read(ArraySegment<byte> s);
    }

    // 유저 확인용으로 보내줄 패킷
    class PlayerInfoReq : Packet
    {
        public long playerId;
        public string name; // 가변 길이 변수 처리

        public struct SkillInfo
        {
            public int id;
            public short level;
            public float duration;

            public bool Write(Span<byte> span, ref ushort count)
            {
                bool success = true;

                success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), id);
                count += sizeof(int);
                success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), level);
                count += sizeof(short);
                success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), duration);
                count += sizeof(float);

                return success;
            }

            public void Read(ReadOnlySpan<byte> span, ref ushort count)
            {
                id = BitConverter.ToInt32(span.Slice(count, span.Length - count));
                count += sizeof(int);
                level = BitConverter.ToInt16(span.Slice(count, span.Length - count));
                count += sizeof(short);
                duration = BitConverter.ToSingle(span.Slice(count, span.Length - count));
                count += sizeof(float);
            }
        }

        public List<SkillInfo> skills = new List<SkillInfo>(); // 가변 길이 변수 처리


        public PlayerInfoReq()
        {
            this.packetId = (ushort)PacketID.PlayerInfoReq;
        }

        public override ArraySegment<byte> Write()
        {
            /*
            현재 PlayerInfoReq 패킷 구성
            size (2byte)
            packetId (2byte)
            playerId (8byte)
            nameLen (2byte)
            name (?byte)
            */

            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            bool success = true;
            ushort count = 0; // 지금까지 몇 Byte를 Buffer에 밀어 넣었는가?

            Span<byte> span = new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count);

            count += sizeof(ushort);
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.packetId);
            count += sizeof(ushort);
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), this.playerId);
            count += sizeof(long);
            

            // string 처리 (Buffer에 2Byte인 string len을 위한 공간을 남겨둔 채로 string data를 먼저 삽입 후 string len 삽입) 
            // Buffer에 string data를 삽입함과 동시에 string len을 반환
            ushort nameLen = (ushort)Encoding.Unicode.GetBytes(this.name, 0, name.Length, openSegment.Array, openSegment.Offset + count + sizeof(ushort));
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), nameLen); // 길이
            count += sizeof(ushort);
            count += nameLen;


            // List 처리
            success &= BitConverter.TryWriteBytes(span.Slice(count, span.Length - count), (ushort)skills.Count);
            count += sizeof(ushort);
            foreach(SkillInfo skill in skills)
                success &= skill.Write(span, ref count);


            success &= BitConverter.TryWriteBytes(span, count); // size는 작업이 끝난 뒤 초기화

            if (success == false)
                return null;

            return SendBufferHelper.Close(count);
        }

        public override void Read(ArraySegment<byte> openSegment)
        {
            ushort count = 0;

            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(openSegment.Array, openSegment.Offset, openSegment.Count);

            count += sizeof(ushort);
            count += sizeof(ushort);
            this.playerId = BitConverter.ToInt64(span.Slice(count, span.Length - count)); // Slice는 실질적으로 Span에 변화를 주지 않음
            count += sizeof(long);

            // string 처리
            // playerId 이후(count로 세는 중) 16bit(2byte)짜리 unsigned 정수형 정보 -> 받은 문자열 길이
            ushort nameLen = BitConverter.ToUInt16(span.Slice(count, span.Length - count)); 
            count += sizeof(ushort);
            this.name = Encoding.Unicode.GetString(span.Slice(count, nameLen)); // GetString()는 Byte 배열을 받아 string으로 변환
            count += nameLen;


            // List 처리
            skills.Clear();
            ushort skillLen = BitConverter.ToUInt16(span.Slice(count, span.Length - count));
            count += sizeof(ushort);
            for (int i = 0; i < skillLen; i++)
            {
                SkillInfo skill = new SkillInfo();
                skill.Read(span, ref count);
                skills.Add(skill);
            }
        }
    }
    /*
    // 유저 확인될 시 보내줄 패킷
    class PlayerInfoOk : Packet
    {
        public int hp;
        public int attack;
    }
    */
    // 패킷 id
    public enum PacketID
    {
        PlayerInfoReq = 1,
        PlayerInfoOk = 2,
    }

    class ServerSession : Session
    {
        public override void OnConnected(EndPoint endPoint)
        {
            // 로그
            Console.WriteLine($"OnConnected : {endPoint}");


            for (int i = 0; i < 10; i++)
            {
                // 
                Random random = new Random();
                int randStrlen = random.Next(12) + 3;
                string sendMsg = "";

                for (int j = 0; j < randStrlen; j++)
                    sendMsg += (char)(random.Next(26) + 97);
                //

                PlayerInfoReq packet = new PlayerInfoReq() { playerId = 1001, name = sendMsg };
                packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 1, level = 15, duration = 4 });
                packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 2, level = 22, duration = 12 });
                packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 3, level = 17, duration = 3 });
                packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 4, level = 53, duration = 38 });
                packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 5, level = 48, duration = 30 });


                ArraySegment<byte> s = packet.Write(); // 직렬화

                if (s != null) // success시 Send
                    Send(s);
            }
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected : {endPoint}");
        }

        public override int OnRecv(ArraySegment<byte> buffer)
        {
            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            Console.WriteLine($"[From Client] {recvData}");

            return buffer.Count;
        }

        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

            Connector connector = new Connector();
            connector.Connect(endPoint, () => { return new ServerSession(); });


            while (true)
            {
                try
                {

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Thread.Sleep(1000);
            }
        }
    }
}