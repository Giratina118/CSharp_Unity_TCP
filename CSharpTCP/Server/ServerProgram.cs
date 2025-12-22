using ServerCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// 패킷 전송 현재 과정
// 클라(개인) -> 서버 | 연결 시도
// 서버 -> 클라(개인) | 연결 완료 확인
// 클라(개인) -> 서버 | 플레이어 정보 전송(닉네임 정보는 아직 안 넣었음)
// 서버 -> 클라(개인) | 연결 완료 신호 보냄, 클라이언트 아이디 전송(몇 번째로 접속한 클라인지 숫자를 세어 매김)
// 서버 -> 클라(개인) | 서버에 접속해 있던 모든 캐릭터 생성하도록
// 클라(개인) -> 서버 | 서버에 접속해 있던 모든 캐릭터 생성 후 본인 캐릭터 생성 요청
// 서버 -> 클라(전체) | 모든 클라에 캐릭터 생성 요청, 동시에 플레이어 접속 알림 보냄(아직 안 넣음, 채팅 만들고 채팅으로 알리기)
// 
// 클라(개인) -> 서버 | 위치(이동) 정보 보냄
// 서버 -> 클라(전체) | 해당 플레이어의 위치 정보 갱신
// 
// 클라(개인) -> 서버 | 채팅 보내기
// 서버 -> 클라(전체) | 채팅 정보 갱신

// 연결 해제(플레이어 오브젝트 삭제) 구현
// 입장 시에 이미 들어와 있는 클라이언트들 생성 후 처리하기

// 입장 퇴장 시 알림 메시지 보내기 (모두에게 + 본인에게)


namespace Server
{
    internal class ServerProgram
    {
        static Listener _listener = new Listener();

        static async Task GameLoop()
        {
            bool serverRunning = true;

            const float TickDelta = 0.1f; // 10 TPS
            const int SleepMs = 1;

            float accumulator = 0f;
            float frameTime = 0f;

            var stopwatch = Stopwatch.StartNew();
            long lastTimeMs = stopwatch.ElapsedMilliseconds;

            while (serverRunning)
            {
                long now = stopwatch.ElapsedMilliseconds;
                frameTime = (now - lastTimeMs) / 1000f;
                lastTimeMs = now;

                // GC / 스파이크 방어
                frameTime = Math.Min(frameTime, 0.25f);

                accumulator += frameTime;

                while (accumulator >= TickDelta)
                {
                    MonsterManager.Instance.Update(TickDelta);
                    MissileManager.Instance.Update(TickDelta);

                    accumulator -= TickDelta;
                }

                await Task.Delay(SleepMs);
            }
        }

        static void Main(string[] args)
        {
            string host = Dns.GetHostName(); // 로컬 호스트 이름 찾기
            IPHostEntry ipHost = Dns.GetHostEntry(host); // 호스트 ip 얻기
            IPAddress ipAddr = ipHost.AddressList[0]; // ip 리스트 중에 첫 번째 획득
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777); // ip 특정 포트 번호 접근

            MonsterManager.Instance.InitData(); // 몬스터 데이터 받기


            // 클라이언트 받기
            _listener.Init(endPoint, () => { return new ClientSession(); });
            Console.WriteLine("Listening...");

            Task.Run(GameLoop);
            Thread.Sleep(Timeout.Infinite);
            
        }
    }
}
