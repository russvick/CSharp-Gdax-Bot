namespace GdaxBot.Models
{
    class EmaSmaCrossoverModel
    {
        public int SmaTimePeriod { get; set; }
        public int SmaGranularity { get; set; }
        public int EmaTimePeriod { get; set; }
        public int EmaGranularity { get; set; }

        public EmaSmaCrossoverModel(int smaTimePeriod, int smaGranularity, int emaTimePeriod, int emaGranularity)
        {
            SmaTimePeriod = smaTimePeriod;
            SmaGranularity = smaGranularity;
            EmaTimePeriod = emaTimePeriod;
            EmaGranularity = emaGranularity;
        }
    }
}
