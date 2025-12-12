using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    public class Listener
    {
        Socket _listenSocket;
        //Action<Socket> _onAcceptHandler;
        Func<Session> _sessionFactory;

        public void Init(IPEndPoint endPoint, Func<Session> sessionFactory)
        {
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sessionFactory += sessionFactory;

            // 문지기 교육
            _listenSocket.Bind(endPoint);

            // 영업 시작
            // backlog - 최대 대기수, 초과 시 접속 불가(fail)
            _listenSocket.Listen(10);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            RegisterAccept(args);
        }

        void RegisterAccept(SocketAsyncEventArgs args)
        {
            // args의 값이 null이 아닌 채로 동작할 수 있으므로 null 처리를 진행한다.
            args.AcceptSocket = null;

            // AcceptAsync 메서드를 이용하여 비동기로 접속을 받는다.
            bool pending = _listenSocket.AcceptAsync(args);
            if (pending == false)
            {
                // 실패할 경우 SocketSocketError의 값을 null로 보내어
                // 아무런 동작을 하지 않게 만든다.
                OnAcceptCompleted(null, args);
            }
        }

        void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            // 소켓 에러가 발생하지 않았다면
            if (args.SocketError == SocketError.Success)
            {
                Session _session = _sessionFactory.Invoke();
                _session.Start(args.AcceptSocket);
                //
                Thread.Sleep(1000);

                _session.OnConnected(args.AcceptSocket.RemoteEndPoint);

                // 인보크(=이벤트)를 이용해 다음 동작을 진행한다.
                //_onAcceptHandler.Invoke(args.AcceptSocket);
            }
            else
            {
                Console.WriteLine(args.SocketError.ToString());
            }

            // 다음 이벤트를 위해 새로이 등록해준다.
            RegisterAccept(args);
        }
    }
}
