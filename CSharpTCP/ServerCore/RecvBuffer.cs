using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class RecvBuffer
    {
        ArraySegment<byte> _buffer; // 받은 데이터
        int _readPos;  // 작업 진행도 표시(받아온 byte 중 얼마나 작업했는지)
        int _writePos; // 받아온 byte 위치 표시

        public RecvBuffer(int bufferSize)
        {
            _buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
        }

        // 버퍼의 유효 사이즈, 작업해야할 데이터가 얼마나 쌓였는지
        public int DataSize { get { return _writePos - _readPos; } }

        // 버퍼에 남은 공간이 얼마인지(얼마나 더 받을 수 있는지)
        public int FreeSize { get { return _buffer.Count - _writePos; } }

        // 데이터를 어디부터 읽으면 되는지 (DataSize 이용)
        public ArraySegment<byte> ReadSegment
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize); }
        }

        // 버퍼의 남은 공간 크기 (FreeSize 이용)
        public ArraySegment<byte> WriteSegment
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize); }
        }

        // 데이터가 buffer크기를 넘어서지 않도록 중간중간 read, write 커서를 맨 앞으로 보낸다.
        public void Clean()
        {
            int dataSize = DataSize;
            if (dataSize == 0) // 남은 데이터가 없으면 커서 위치를 맨 앞으로 보낸다.
                _readPos = _writePos = 0;
            else
            {
                // 남은 데이터가 있으면 시작 위치로 복사한 후 커서를 맨 아프올 보낸다.
                Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
                _readPos = 0;
                _writePos = dataSize;
            }
        }

        // 데이터를 성공적으로 받았을 경우 OnRead를 호출하여 read 커서 이동
        public bool OnRead(int numOfBytes)
        {
            if (numOfBytes > DataSize)
                return false;

            _readPos += numOfBytes;
            return true;
        }

        // 클라이언트에서 데이트를 보낸 상황에서 성공적으로 받았을 경우 OnWrite를 호출하여 write 커서 이동
        public bool OnWrite(int numOfBytes)
        {
            if (numOfBytes > FreeSize)
                return false;

            _writePos += numOfBytes;
            return true;
        }
    }
}