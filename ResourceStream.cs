using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streams.Resources
{
    public class ResourceReaderStream : Stream
    {
        BufferedStream BufferedStream { get; }
        byte[] AimKey { get; }
        byte[] Buffer { get; }
        int BufferPosition { get; set; } 
        int BufferBytesCount { get; set; }
        static byte FakeByte { get; } = 9;
        byte PreviousByte { get; set; } = FakeByte;
        int PassedElementsCount { get; set; }
        bool StreamIsFinished { get; set; }
        bool BufferPositionIsOnKey { get => PassedElementsCount % 2 == 0; }
        bool KeyIsFound { get; set; }
        bool IsRead { get; set; }

        public ResourceReaderStream(Stream stream, string key)
        {
            BufferedStream = new BufferedStream(stream, Constants.BufferSize);
            AimKey = Encoding.ASCII.GetBytes(key);
            Buffer = new byte[Constants.BufferSize];
            BufferPosition = Buffer.Length;
        }

        void CheckPosition()
        {
            if (BufferPosition >= BufferBytesCount)
                UpdateBuffer();
        }

        void UpdateBuffer()
        {
            BufferBytesCount = BufferedStream.Read(Buffer, 0, Buffer.Length);
            BufferPosition = 0;
            if (BufferBytesCount == 0)
                StreamIsFinished = true;
        }

        byte GetCurrentByte()
        {
            CheckPosition();
            if (StreamIsFinished) throw new EndOfStreamException();
            return Buffer[BufferPosition];
        }

        void UpdateBufferPosition(byte previousByte)
        {
            PreviousByte = previousByte;
            BufferPosition++;
        }

        bool MoveToNextByte()
        {
            var _byte = GetCurrentByte();
            if (StreamIsFinished) return false;
            switch (_byte)
            {
                case 0:
                    if (PreviousByte == 0)
                    {
                        UpdateBufferPosition(FakeByte);
                        return true;
                    }
                    UpdateBufferPosition(_byte);
                    return MoveToNextByte();
                case 1:
                    if (PreviousByte == 0)
                    {
                        PreviousByte = FakeByte;
                        BufferPosition++;
                        PassedElementsCount++;
                        return true;
                    }
                    UpdateBufferPosition(_byte);
                    return true;
                default:
                    UpdateBufferPosition(_byte);
                    return true;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count + offset > buffer.Length)
                throw new ArgumentException();
            if (!IsRead)
            {
                if (!StreamIsFinished && !KeyIsFound)
                    SeekValue();
                if (KeyIsFound)
                    return ReadValue(buffer, offset, count);
            }
            return 0;
        }

        private int ReadValue(byte[] buffer, int offset, int count)
        {
            var readBytesCount = 0;
            var currentByte = GetCurrentByte();
            while (readBytesCount < count)
            {
                MoveToNextByte();
                if (BufferPositionIsOnKey)
                    break;
                buffer[offset++] = currentByte;
                currentByte = GetCurrentByte();
                readBytesCount++;
            }
            if (BufferPositionIsOnKey)
                IsRead = true;
            return readBytesCount;
        }

        private void SeekValue()
        {
            while (!StreamIsFinished && !KeyIsFound)
            {
                if (BufferPositionIsOnKey)
                {
                    KeyIsFound = CurrentElementEquals(AimKey);
                    if (KeyIsFound) break;
                }
                else
                    SkipElement();
            }
        }

        bool CurrentElementEquals(byte[] bytes)
        {
            var passedElementsCount = PassedElementsCount;
            var matchesCount = 0;
            while(matchesCount < bytes.Length)
            {
                var _byte = GetCurrentByte();
                if (_byte != bytes[matchesCount])
                {
                    SkipElement();
                    return false;
                }
                matchesCount++;
                MoveToNextByte();
            }
            MoveToNextByte();
            return (PassedElementsCount != passedElementsCount);
        }

        void SkipElement()
        {
            var passedElementsCount = PassedElementsCount;
            while (PassedElementsCount == passedElementsCount)
                MoveToNextByte();
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position 
        { 
            get => BufferPosition; 
            set => throw new NotSupportedException(); 
        }
    }
}
