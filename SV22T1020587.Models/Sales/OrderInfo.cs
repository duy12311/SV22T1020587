using System;
using System.ComponentModel.DataAnnotations;

namespace SV22T1020587.Models.Order
{
    public class OrderInfo
    {
        public string CustomerName { get; set; } = "";
        public string DeliveryProvince { get; set; } = "";
        public string DeliveryAddress { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";

        // New: Payment method selected by customer (e.g. "cod", "bank_transfer", "card")
        [Required]
        public string PaymentMethod { get; set; } = "cod";
    }
}