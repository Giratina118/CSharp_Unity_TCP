using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Server.ObjListPacket;

namespace Server
{
    public class SessionManager
    {
        public static SessionManager Instance { get; } = new SessionManager();

        object _lock = new object();
        public Dictionary<long, ClientSession> Sessions = new Dictionary<long, ClientSession>(); // key: 클라이언트 id, value: 클라이언트 세션, 연결된 클라이언트 관리
        //int _sessionId = 0; // 클라이언트 id가 1씩 늘어나며 새로 연결한 클라이언트에게 id를 붙여줌)

        // 새 세션 등록
        public void Add(ClientSession session)
        {
            Console.WriteLine("add");
            lock (_lock)
            {
                //session.Info.id = _sessionId++;
                Sessions.Add(session.Info.id, session); // id 지정하고 딕셔너리에 추가

                // 연결 완료 신호 보냄
                PlayerInfoOk packet = new PlayerInfoOk() { playerId = session.Info.id };
                ArraySegment<byte> segment = packet.Write(); // 직렬화

                if (segment != null)
                    session.Send(segment);


                // 먼저 접속해 있던 플레이어의 오브젝트들 생성하라고 전송
                List<ObjectInfo> playerInfoList = new List<ObjectInfo>(); // 먼저 접속해 있던 플레이어들의 정보(id, 위치)
                foreach (ClientSession item in Sessions.Values)
                {
                    if (item.Info.id == session.Info.id)
                        continue;

                    ObjectInfo infoTemp = new ObjectInfo() { objType = (ushort)ObjType.Player, id = item.Info.id, position = item.Info.position, rotation = item.Info.rotation };
                    playerInfoList.Add(infoTemp);
                }
                ObjListPacket crAllPacket = new ObjListPacket() { messageType = (ushort)MsgType.CreateAllPlayer, Infos = playerInfoList };
                ArraySegment<byte> crSegment = crAllPacket.Write();
                session.Send(crSegment);


                // 먼저 생성되어 있던 모든 몬스터 생성하라고 전송
                List<ObjectInfo> monsterInfoList = new List<ObjectInfo>(); // 먼저 접속해 있던 플레이어들의 정보(id, 위치)
                foreach (int item in MonsterManager.Instance.Monsters.Keys)
                {
                    Monster sendMonster = MonsterManager.Instance.Monsters[item];
                    ObjectInfo infoTemp = new ObjectInfo() { objType = sendMonster.Type, id = item, position = sendMonster.Pos, rotation = sendMonster.Rot };
                    monsterInfoList.Add(infoTemp);
                }
                ObjListPacket createAllMonsterPacket = new ObjListPacket() { messageType = (ushort)MsgType.CreateAllMonster, Infos = monsterInfoList };
                ArraySegment<byte> crMonsterSegment = createAllMonsterPacket.Write();
                session.Send(crMonsterSegment);

                
                // 건물들 생성하라고 전송
                List<ObjectInfo> structureInfoList = new List<ObjectInfo>(); // 건물들 정보
                foreach (Structure item in StructureManager.Instance.Structures)
                {
                    ObjectInfo infoTemp = new ObjectInfo() { objType = (ushort)ObjType.Structure, id = item.Type, position = item.Pos, rotation = Vector3.Zero };
                    structureInfoList.Add(infoTemp);
                    Console.WriteLine($"{(ushort)ObjType.Structure},  {item.Type},  {item.Pos}");
                }
                ObjListPacket createAllStructurePacket = new ObjListPacket() { messageType = (ushort)MsgType.CreateAllStructure, Infos = structureInfoList };
                ArraySegment<byte> crStructureSegment = createAllStructurePacket.Write();
                Console.WriteLine($"{crStructureSegment.Count}");
                session.Send(crStructureSegment);
                

                // 입장 알림 보내기
                ChatPacket chatPacket = new ChatPacket() { playerId = -1, chat = $"{session.Name}님이 입장하셨습니다." };
                ArraySegment<byte> chatSegment = chatPacket.Write();
                BroadcastAll(chatSegment);

                SpatialGrid.Instance.AddPlayer(session); // 충돌 처리를 위함
            }
        }

        // 종료된 세션 제거
        public void Remove(ClientSession session)
        {
            lock (_lock)
            {
                if (Sessions.ContainsKey(session.Info.id))
                    Sessions.Remove(session.Info.id);

                // 퇴장 알림 보내기
                ChatPacket chatPacket = new ChatPacket() { playerId = -1, chat = $"{session.Name}님이 퇴장하셨습니다." };
                ArraySegment<byte> chatSegment = chatPacket.Write();
                BroadcastAll(chatSegment);
            }
        }

        // 브로드캐스트 (모든 세션에 패킷을 보냄)
        public void BroadcastAll(ArraySegment<byte> data)
        {
            lock (_lock)
            {
                foreach (var session in Sessions.Values)
                {
                    //Console.WriteLine($"{session.Info.id} -> Server -> All");
                    session.Send(data);
                }
            }
        }

        // 브로드캐스트 (특정 한 클라이언트를 제외한 모든 세션에 패킷을 보냄)
        public void BroadcastExcept(ArraySegment<byte> data, long targetId)
        {
            lock (_lock)
            {
                foreach (var session in Sessions.Values)
                {
                    if (session.Info.id == targetId)
                        continue;

                    //Console.WriteLine($"{targetId} -> Server -> {session.Info.id}");

                    session.Send(data);
                }
            }
        }

        // 점수 전송
        public void UpdatePoint()
        {
            // TODO: 점수 전송
        }
    }
}