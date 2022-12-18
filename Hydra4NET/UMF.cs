using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydra4NET
{
    public class UMFBaseMessage
    {
        private const string _UMF_Version = "UMF/1.4.6";

        private string? _toField;
        private string? _fromField;
        private string? _midField;
        private string? _versionField;
        private string? _timestampField;
        private object? _bodyField;

        public string? To {
            get
            {
                return _toField;
            }
            set
            {
                _toField = value;
            }
        }

        public string From { get; set; }
        public string Mid { get; set; }
        public string Version { get; set; }
        public string Timestamp { get; set; }
        public object Body { get; set; }

        public UMFBaseMessage()
        {
            To ??= "";
            From ??= "";
            Mid = Guid.NewGuid().ToString();
            Version = _UMF_Version;
            DateTime dateTime = DateTime.Now;            
            Timestamp = dateTime.ToUniversalTime().ToString("s") + "Z";
            Body = new object();
        }
    }    

    public class UMF
    {
        public bool Validate(UMFBaseMessage message)
        {
            return false;
        }
    }
}
