using System;

namespace UnistadDocumentLibrary.Exceptions
{

    [Serializable]
    public class DocumentNameConversionException : BaseException
    {
        public DocumentNameConversionException() { }
        public DocumentNameConversionException(string message) : base(message) { }
        public DocumentNameConversionException(string message, Exception inner) : base(message, inner) { }
        protected DocumentNameConversionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
