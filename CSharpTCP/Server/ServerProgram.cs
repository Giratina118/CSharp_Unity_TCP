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

            float accumulator = 0.0f;
            float frameTime = 0.0f;

            var stopwatch = Stopwatch.StartNew();
            long lastTimeMs = stopwatch.ElapsedMilliseconds;

            while (serverRunning)
            {
                long now = stopwatch.ElapsedMilliseconds;
                frameTime = (now - lastTimeMs) / 1000.0f;
                lastTimeMs = now;

                // GC 스파이크(어플리케이션 성능이 순간적으로 급격히 저하하는 것, 가비지컬렉션, 박싱<->언박싱 등 원인) 방어
                frameTime = Math.Min(frameTime, 0.25f);

                accumulator += frameTime;

                while (accumulator >= TickDelta)
                {
                    MonsterManager.Instance.Update(TickDelta);
                    MissileManager.Instance.Update(TickDelta);

                    // 점수 업데이트
                    SessionManager.Instance.UpdatePoint();

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

            MonsterManager.Instance.InitData();   // 몬스터 데이터 받기
            StructureManager.Instance.InitData(); // 건물 데이터 받기

            // 클라이언트 받기
            _listener.Init(endPoint, () => { return new ClientSession(); });
            Console.WriteLine("Listening...");

            Task.Run(GameLoop);
            Thread.Sleep(Timeout.Infinite);
            
        }
    }
}
