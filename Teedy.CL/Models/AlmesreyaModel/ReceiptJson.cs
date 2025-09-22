using System.Text.Json.Serialization;

namespace Teedy.CL.Models.AlmesreyaModel
{
    public class ReceiptJson
    {
        [JsonPropertyName("رقم الايصال")]
        public int ReceiptNumber { get; set; }
        [JsonPropertyName("رقم المعاملة")]
        public List<ReceiptTransaction> ReceiptTransaction { get; set; }
        [JsonPropertyName("العميل")]
        public Client Client { get; set; }
        [JsonPropertyName("الشركة")]
        public Company Company { get; set; }
        [JsonPropertyName("تاريخ الايصال")]
        public DateTime ReceiptDate { get; set; }
        [JsonPropertyName("ملغي")]
        public bool IsCancelled { get; set; }
        [JsonPropertyName("نوع العميل")]
        public BuyerTypeETA Buyer { get; set; }
    }

    public class ReceiptTransaction
    {
        [JsonPropertyName("اسم العملة")]
        public string CurrencyName { get; set; }
        [JsonPropertyName("سعر الصرف")]
        public decimal CurrencyExchangeRate { get; set; }
        [JsonPropertyName("المبلغ")]
        public decimal Amount { get; set; }
        [JsonPropertyName("المبلغ بعد الصرف")]
        public decimal TotalAfterExchange { get; set; }
    }
    public class Client
    {
        [JsonPropertyName("الاسم")]
        public string Name { get; set; }
        [JsonPropertyName("الرقم اثبات الشخصية")]
        public string NationalId { get; set; }
        [JsonPropertyName("الوظيفة")]
        public string Job { get; set; }
        [JsonPropertyName("الهاتف")]
        public string Phone { get; set; }
    }
    public class Company
    {
        [JsonPropertyName("الاسم")]
        public string Name { get; set; }
        [JsonPropertyName("الرقم الضريبي")]
        public string TaxId { get; set; }
        [JsonPropertyName("تاريخ انتهاء السجل التجاري")]
        public DateTime CommercialRegisterExpiryDate { get; set; }
        [JsonPropertyName("السجل التجاري")]
        public string CommercialRegister { get; set; }

    }
}
