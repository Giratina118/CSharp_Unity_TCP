using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    public class SendBufferHelper
    {
        // 멀티 쓰레드 환경에서 중접 문제를 막기 위해 별도의 공간을 만들어줌
        public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(() => { return null; });
        public static int ChunkSize { get; set; } = 4096 * 100;

        public static ArraySegment<byte> Open(int reserveSize)
        {
            // 한 번도 사용하지 않았을 때(생성되지 않은 상태) 새로 생성
            if (CurrentBuffer.Value == null)
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            // 공간이 찼다면 새 버퍼를 만들어 준다.
            if (CurrentBuffer.Value.FreeSize < reserveSize)
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            return CurrentBuffer.Value.Open(reserveSize);
        }

        public static ArraySegment<byte> Close(int usedSize)
        {
            return CurrentBuffer.Value.Close(usedSize);
        }
    }

    public class SendBuffer
    {
        byte[] _buffer; // 보낼 데이터
        int _usedSize = 0; // 버퍼에서 얼마나 보냈는지(RecvBuffer의 WritePos같은 역할)

        // 남은 버퍼 공간(유효 공간)
        public int FreeSize { get { return _buffer.Length - _usedSize; } }

        public SendBuffer(int chunkSize)
        {
            _buffer = new byte[chunkSize];
        }

        // 얼마만큼 사용할 것인지, 매개변수로 받아옴
        public ArraySegment<byte> Open(int reserveSize)
        {
            // 원하는 사이즈가 남은 사이즈보다 큰 경우 기본형(null) 리턴 
            if (reserveSize > FreeSize)
                return new ArraySegment<byte>();

            // 여유 공간이 있다면 요청한 공간을 넘겨준다.
            return new ArraySegment<byte>(_buffer, _usedSize, reserveSize);
        }

        // Open 이후 사용한 사이즈만큼 받아와서 지정해준 후 다시 _usedSize에 돌려준다. 
        public ArraySegment<byte> Close(int usedSize)
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, _usedSize, usedSize);
            _usedSize += usedSize; // 두 사이트가 다름 =, +=
            return segment;
        }
    }
}