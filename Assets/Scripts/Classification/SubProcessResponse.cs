using Newtonsoft.Json;


namespace Classification
{
    public enum Status
    {
        ERROR = 0,
        OK = 1
    }


    public class SubProcessResponse
    {
        [JsonProperty("status")]
        public Status Status { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonProperty("probability")]
        public float Probability { get; set; }

        [JsonIgnore]
        public bool IsSuccess => Status == Status.OK;

        [JsonIgnore]
        public bool IsError => Status == Status.ERROR;

        public override string ToString()
        {
            return $"Status: {Status}, Content: {Content}, Error: {ErrorMessage}";
        }
    }
}
