using System.Text.Json;

namespace Hydra4NET
{
    public class UMFBase
    {
        protected const string _UMF_Version = "UMF/1.4.6";
        protected string _To;
        protected string _From;
        protected string _Mid;
        protected string _Type;
        protected string _Version;
        protected string _Timestamp;

        protected UMFBase()
        {
            _To ??= String.Empty;
            _From ??= String.Empty;
            _Mid = Guid.NewGuid().ToString();
            _Type = String.Empty;
            _Version = _UMF_Version;
            _Timestamp = GetTimestamp();
        }

        public string To
        {
            get { return _To; }
            set { _To = value; }
        }

        public string Frm
        {
            get { return _From; }
            set { _From = value; }
        }

        public string Mid
        {
            get { return _Mid; }
            set { _Mid = value; }
        }

        public string Typ
        {
            get { return _Type; }
            set { _Type = value; }
        }

        public string Ver
        {
            get { return _Version; }
            set { _Version = value; }
        }

        public string Ts
        {
            get { return _Timestamp; }
            set { _Timestamp = value; }
        }

        public static string GetTimestamp()
        {
            DateTime dateTime = DateTime.Now;
            return dateTime.ToUniversalTime().ToString("u").Replace(" ", "T");
        }
    }

    public class UMF<T> : UMFBase where T : class, new()
    {
        private T _Body;

        public T Bdy 
        { 
            get { return _Body; } 
            set { _Body = value; }  
        }

        public UMF()
        {
            _Body = new T();
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        static public U? Deserialize<U>(string message)
        {
            return JsonSerializer.Deserialize<U>(message, new JsonSerializerOptions() 
            { 
                PropertyNameCaseInsensitive = true 
            });
        }
    }    
}
