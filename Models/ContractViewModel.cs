namespace retail_app_tester.Models
{
    public class ContractViewModel
    {
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string OrderId { get; set; }
        public double OrderTotal { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime UploadDate { get; set; }
        public string FileName { get; set; }
        public string ItemsSummary { get; set; }
        public int ItemCount { get; set; }
        public bool ContractExists { get; set; }

    }
}
