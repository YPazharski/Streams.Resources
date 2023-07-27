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
        byte PreviosByte { get; set; } = FakeByte;
        int PassedElementsCount { get; set; }
        bool StreamIsFinished { get; set; }
        bool BufferPositionIsOnKey { get => PassedElementsCount % 2 == 0; }
        bool KeyIsFound { get; set; }
        bool IsRead { get; set; }

        public ResourceReaderStream(Stream stream, string key)
        {
            // You should not use stream in the constructor of wrapping stream.
            BufferedStream = new BufferedStream(stream, Constants.BufferSize);
            AimKey = Encoding.ASCII.GetBytes(key);
            Buffer = new byte[Constants.BufferSize];
            BufferPosition = Buffer.Length;
        }

        //byte GetNextByte()
        //{
        //    if (StreamIsFinished) throw new InvalidOperationException();
        //    if (BufferPosition >= BufferBytesCount)
        //        UpdateBuffer();
        //    var _byte = Buffer[BufferPosition++];
        //    switch (_byte)
        //    {
        //        case 0:
        //            if (PreviosByte == 0)
        //            {
        //                PreviosByte = FakeByte;
        //                return _byte;
        //            }
        //            PreviosByte = 0;
        //            return GetNextByte();
        //        case 1:
        //            if (PreviosByte == 0)
        //            {
        //                PreviosByte = FakeByte;
        //                PassedElementsCount++;
        //                return GetNextByte();
        //            }
        //            PreviosByte = _byte;
        //            return _byte;
        //        default:
        //            PreviosByte = _byte;
        //            return _byte;
        //    }
        //}

        bool MoveToNextByte()
        {
            if (StreamIsFinished) return false;
            if (BufferPosition >= BufferBytesCount)
            {
                UpdateBuffer();
                return MoveToNextByte();
            }
            var _byte = Buffer[BufferPosition];
            switch (_byte)
            {
                case 0:
                    if (PreviosByte == 0)
                    {
                        PreviosByte = FakeByte;
                        return true;
                    }
                    PreviosByte = 0;
                    BufferPosition++;
                    return MoveToNextByte();
                case 1:
                    if (PreviosByte == 0)
                    {
                        PreviosByte = FakeByte;
                        BufferPosition++;
                        PassedElementsCount++;
                        return true;
                    }
                    BufferPosition++;
                    PreviosByte = _byte;
                    return true;
                default:
                    PreviosByte = _byte;
                    BufferPosition++;
                    return true;
            }
        }

        byte GetCurrentByte()
        {
            return Buffer[BufferPosition];
        }

        void UpdateBuffer()
        {
            BufferBytesCount = BufferedStream.Read(Buffer, 0, Buffer.Length);
            BufferPosition = 0;
            if (BufferBytesCount == 0)
                StreamIsFinished = true;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // if key not found yet: SeekValue();
            // if value is not read yet: ReadFieldValue(...)
            // else return 0;
            if (count + offset > buffer.Length)
                throw new ArgumentException();
            if (!IsRead)
            {
                UpdateBuffer();
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
            IsRead = true;
            return readBytesCount;
        }

        private void SeekValue()
        {
            // while not end of stream read next section key, compare with required key
            // and skip value if read key is not equal to required key
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
            if (PassedElementsCount == passedElementsCount)
                return false;
            return true;
        }

        void SkipElement()
        {
            var passedElementsCount = PassedElementsCount;
            while (PassedElementsCount == passedElementsCount)
                MoveToNextByte();
        }

        #region YAGNI METHODS
        public override void Flush()
        {
            // nothing to do
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => throw new NotImplementedException();

        public override bool CanSeek => throw new NotImplementedException();

        public override bool CanWrite => throw new NotImplementedException();

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        #endregion
    }
}
